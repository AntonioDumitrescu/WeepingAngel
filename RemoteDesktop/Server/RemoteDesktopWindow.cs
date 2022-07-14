using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Imaging;
using ImGuiNET;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using OpenH264;
using OpenH264.Intermediaries;
using RemoteDesktop.Messages;
using Veldrid;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;
using Yggdrasil.Api.Server;
using DataGridViewBindingCompleteEventArgs = System.Windows.Forms.DataGridViewBindingCompleteEventArgs;
using PixelFormat = Veldrid.PixelFormat;
using Rectangle = System.Drawing.Rectangle;

namespace RemoteDesktop.Server;

internal class RemoteDesktopWindow : IMessageReceiver
{
    private readonly ILogger<RemoteDesktopWindow> _logger;
    private readonly IRemoteClient _client;
    private readonly IServerWindow _serverWindow;
    private readonly OpenH264BeginMessage _openMessage = new();
    private readonly DecoderWrapper _decoder = new();
    private readonly CancellationTokenSource _cts = new();

    #region Pipeline

    private readonly Channel<List<byte[]>> _networkOutput =
        Channel.CreateBounded<List<byte[]>>(
            new BoundedChannelOptions(2)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });

    private readonly Channel<(IMemoryOwner<byte>, int, int)> _decodingOutput =
        Channel.CreateBounded<(IMemoryOwner<byte>, int, int)>(
            new BoundedChannelOptions(2)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });

    // do not change the bounded size of this one:
    private readonly Channel<(IMemoryOwner<byte>, int, int)> _conversionOutput =
        Channel.CreateBounded<(IMemoryOwner<byte>, int, int)>(
            new BoundedChannelOptions(1)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });

    #endregion

    private readonly GraphicsDevice _graphicsDevice;
    private Texture? _texture;
    private IntPtr _binding = IntPtr.Zero;

    private readonly Task _task;

    public RemoteDesktopWindow(ILogger<RemoteDesktopWindow> logger, IRemoteClient client, IServerWindow serverWindow)
    {
        _logger = logger;
        _client = client;
        client.Connection.AddReceiver(this);

        _serverWindow = serverWindow;
        
        _graphicsDevice = serverWindow.GraphicsDevice;
        
        client.Connection.Send(_openMessage);

        var decodeTask = Task.Factory.StartNew(DecodeStreamAsync, TaskCreationOptions.LongRunning);
        var convertTask = Task.Factory.StartNew(ConvertStreamAsync, TaskCreationOptions.LongRunning);

        _task = Task.WhenAll(decodeTask, convertTask);
    }

    private void CreateTexture(int width, int height)
    {
        _logger.LogInformation("Creating {w}x{h} texture.", width, height);

        _texture = _graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            (uint)width,
            (uint)height,
            1,
            1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled));
    }

    private unsafe void UploadImage(ReadOnlySpan<byte> rgba, int width, int height)
    {
        if (_texture == null || _texture.Width != width || _texture.Height != height)
        {
            _texture?.Dispose();
            CreateTexture(width, height);
        }

        fixed (void* pRgbaBytes = &rgba[0])
        {
            _graphicsDevice.UpdateTexture(
                _texture,
                new IntPtr(pRgbaBytes),
                (uint)(width * height * 4),
                0,
                0,
                0,
                (uint)width,
                (uint)height,
                1,
                0,
                0);
        }

        _binding = _serverWindow.GetOrCreateImGuiBinding(_graphicsDevice.ResourceFactory, _texture!);
    }

    public void Render()
    {
        ImGui.SetNextWindowSize(new Vector2(200, 200), ImGuiCond.FirstUseEver);

        ImGui.Begin($"Remote Desktop ({_client.Username} / {_client.Connection})");
        {
            /*var targetFps = _openMessage.TargetFps;
            var targetBitRate = _openMessage.TargetBitRate;
            var maxBitRate = _openMessage.MaxBitRate;
            var idrInterval = _openMessage.IdrInterval;

            
            ImGui.SliderInt("FPS", ref targetFps, 1, 60);
            ImGui.SliderInt("Target Bit Rate", ref targetBitRate, 500, 10000);
            ImGui.SliderInt("Max Bit Rate", ref maxBitRate, 500, 10000);
            ImGui.SliderInt("IDR Interval", ref idrInterval, 1, 4);
            */
            

            /*if (_texture != null)
            {
                ImGui.Image(_binding!.Value, new Vector2(_texture.Width, _texture.Height));
            }*/

            if (_conversionOutput.Reader.TryRead(out var item))
            {
                var rgbaOwner = item.Item1;
                var width = item.Item2;
                var height = item.Item3;
                
                UploadImage(rgbaOwner.Memory.Span, width, height);
                
                rgbaOwner.Dispose();
            }


            if (_texture != null)
            {
                ImGui.Image(_binding, ImGui.GetWindowSize());
            }
        }

        ImGui.End();
    }

    public event Action? OnRemove;
    public void RegisterHandlers(IHandlerRegister register)
    {
        register.Register<NalStreamMessage>(ReceiveStreamAsync);
    }

    #region Pipeline

    private async ValueTask ReceiveStreamAsync(NalStreamMessage message)
    {
        await _networkOutput.Writer.WriteAsync(message.Nals);
        Console.WriteLine("wrote nals");
    }

    private async Task DecodeStreamAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            List<byte[]> nals;

            try
            {
                nals = await _networkOutput.Reader.ReadAsync(_cts.Token);
                Console.WriteLine("Read nal list");
            }
            catch (OperationCanceledException)
            {
                return;
            }

            foreach (var nal in nals)
            {
                if (TryDecodeNal(nal, out var memoryOwner, out var width, out var height))
                {
                    try
                    {
                        await _decodingOutput.Writer.WriteAsync((memoryOwner, width, height));
                        Console.WriteLine("Wrote rgb");
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("failed to decode");
                    memoryOwner?.Dispose();
                }
            }
        }
    }

    private unsafe bool TryDecodeNal(ReadOnlySpan<byte> nal, [NotNullWhen(true)] out IMemoryOwner<byte>? result, out int width, out int height)
    {
        fixed (byte* pNalBytes = &nal[0])
        {
            result = _decoder.DecodeToRgb(
                pNalBytes,
                nal.Length,
                out _,
                out var success,
                out width,
                out height);

            return success;
        }
    }
    
    private async Task ConvertStreamAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            IMemoryOwner<byte> rgbOwner;
            int width;
            int height;

            try
            {
                (rgbOwner, width, height) = await _decodingOutput.Reader.ReadAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            var rgbaOwner = MemoryPool<byte>.Shared.Rent(width * height * 4);

            try
            {
                unsafe
                {
                    fixed (byte* pRgbBytes = &rgbOwner.Memory.Span[0])
                    fixed (byte* pRgbaBytes = &rgbaOwner.Memory.Span[0])
                    {
                        var pRgb = (Rgb*)pRgbBytes;
                        var pRgba = (Rgba*)pRgbaBytes;

                        var pixels = width * height;

                        for (var i = 0; i < pixels; i++)
                        {
                            pRgba->R = pRgb->R;
                            pRgba->G = pRgb->G;
                            pRgba->B = pRgb->B;
                            pRgba->A = 0xFF;

                            ++pRgb;
                            ++pRgba;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        

            rgbOwner.Dispose();

            try
            {
                await _conversionOutput.Writer.WriteAsync((rgbaOwner, width, height));
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly ref struct Rgb
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;
    }

    [StructLayout(LayoutKind.Sequential)]
    public ref struct Rgba
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;
    }

    #endregion

    private void Close()
    {
        _cts.Cancel();
        _cts.Dispose();
        OnRemove?.Invoke();
    }

    public void OnClosed()
    {
        Close();
    }
}
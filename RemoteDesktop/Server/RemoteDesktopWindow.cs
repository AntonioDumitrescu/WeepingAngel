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
using Veldrid.OpenGLBinding;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;
using Yggdrasil.Api.Server;
using DataGridViewBindingCompleteEventArgs = System.Windows.Forms.DataGridViewBindingCompleteEventArgs;
using PixelFormat = Veldrid.PixelFormat;
using Rectangle = System.Drawing.Rectangle;

namespace RemoteDesktop.Server;

internal class RemoteDesktopWindow : IMessageReceiver
{
    private const int PresentOverheadAverageCount = 10;

    private static readonly UsageType[] UsageTypes = Enum.GetValues<UsageType>();
    private static readonly string[] UsageTypeLabels = UsageTypes.Select(x => x.ToString()).ToArray();

    private readonly ILogger<RemoteDesktopWindow> _logger;
    private readonly IRemoteClient _client;
    private readonly IServerWindow _serverWindow;
    private readonly OpenH264BeginMessage _openMessage = new();
    private readonly DecoderWrapper _decoder = new();
    private readonly CancellationTokenSource _cts = new();

    #region Pipeline

    private readonly Channel<(List<byte[]>, DateTime)> _networkOutput =
        Channel.CreateBounded<(List<byte[]>, DateTime)>(
            new BoundedChannelOptions(2)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });

    private readonly Channel<(IMemoryOwner<byte>, int, int, DateTime)> _decodingOutput =
        Channel.CreateBounded<(IMemoryOwner<byte>, int, int, DateTime)>(
            new BoundedChannelOptions(2)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });

    // do not change the bounded size of this one:
    private readonly Channel<(IMemoryOwner<byte>, int, int, DateTime)> _conversionOutput =
        Channel.CreateBounded<(IMemoryOwner<byte>, int, int, DateTime)>(
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

    private double _lastPresentedOverhead;
    private readonly List<double> _presentedOverheadValues = new();

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

    private unsafe void UploadImage(ReadOnlySpan<byte> bgra, int width, int height)
    {
        if (_texture == null || _texture.Width != width || _texture.Height != height)
        {
            _texture?.Dispose();
            CreateTexture(width, height);
        }
        
        Console.WriteLine($"Uploading {bgra.Length} span");

        fixed (void* pBgraBytes = &bgra[0])
        {
            _graphicsDevice.UpdateTexture(
                _texture,
                new IntPtr(pBgraBytes),
                (uint)(width * height * 4),
                0,
                0,
                0,
                (uint)width,
                (uint)height,
                1,
                0,
                0);

            _graphicsDevice.WaitForIdle();
        }

        _binding = _serverWindow.GetOrCreateImGuiBinding(_graphicsDevice.ResourceFactory, _texture!);
    }

    public void Render()
    {
        RenderDesktop();
        RenderOptions();
        RenderStatus();
    }

    private void RenderDesktop()
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
                var time = item.Item4;

                UploadImage(rgbaOwner.Memory.Span, width, height);

                rgbaOwner.Dispose();

                _lastPresentedOverhead = (DateTime.Now - time).TotalMilliseconds;

                _presentedOverheadValues.Add(_lastPresentedOverhead);

                while (_presentedOverheadValues.Count > PresentOverheadAverageCount)
                {
                    _presentedOverheadValues.RemoveAt(0);
                }
            }


            if (_texture != null)
            {
                ImGui.Image(_binding, ImGui.GetWindowSize());
            }
        }

        ImGui.End();
    }

    private void RenderOptions()
    {
        var targetBitRate = _openMessage.TargetBitRate;
        var idrInterval = _openMessage.IdrInterval;
        var usageType = _openMessage.UsageType;

        int index;

        var close = false;

        for (index = 0; index < UsageTypes.Length; index++)
        {
            if (UsageTypes[index] == usageType)
            {
                break;
            }
        }

        if (ImGui.Begin($"Remote Desktop ({_client.Username} / {_client.Connection}) settings"))
        {
            ImGui.SliderInt("Target Bit Rate", ref targetBitRate, 1000, 10000);
            ImGui.SliderInt("IDR Interval", ref idrInterval, 1, 5);
            ImGui.Combo("Usage Type", ref index, UsageTypeLabels, UsageTypeLabels.Length);
            close = ImGui.Button("Stop stream.");
        }

        ImGui.End();

        if (close)
        {
            _client.Connection.Send(new StreamEndMessage()).AsTask().Wait();
            Close();
            return;
        }

        usageType = UsageTypes[index];

        var updateRequired =
            targetBitRate != _openMessage.TargetBitRate ||
            idrInterval != _openMessage.IdrInterval ||
            usageType != _openMessage.UsageType;

        _openMessage.TargetBitRate = targetBitRate;
        _openMessage.MaxBitRate = targetBitRate;
        _openMessage.IdrInterval = idrInterval;
        _openMessage.UsageType = usageType;

        if (updateRequired)
        {
            _client.Connection.Send(_openMessage);
        }
    }

    private void RenderStatus()
    {
        if (ImGui.Begin($"Remote Desktop ({_client.Username} / {_client.Connection}) status"))
        {
            ImGui.Text($"Last presented overhead: {_lastPresentedOverhead:F} ms");
            ImGui.Text($"Presented overhead average (last {PresentOverheadAverageCount} values): " +
                       $"{_presentedOverheadValues.Sum() / _presentedOverheadValues.Count:F}");
        }

        ImGui.End();
    }

    public event Action? OnRemove;
    public void RegisterHandlers(IHandlerRegister register)
    {
        register.Register<NalStreamMessage>(ReceiveStreamAsync);
    }

    #region Pipeline

    private ValueTask ReceiveStreamAsync(NalStreamMessage message)
    {
        if (!_networkOutput.Writer.TryWrite((message.Nals, message.Time)))
        {
            _logger.LogError("Dropped received frame!");
        }

        return ValueTask.CompletedTask;
    }

    private async Task DecodeStreamAsync()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                List<byte[]> nals;
                DateTime time;

                try
                {
                    (nals, time) = await _networkOutput.Reader.ReadAsync(_cts.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                foreach (var nal in nals)
                {
                    if (TryDecodeNal(nal, out var memoryOwner, out var width, out var height))
                    {
                        if (!_decodingOutput.Writer.TryWrite((memoryOwner, width, height, time)))
                        {
                            _logger.LogError("Dropped decoded frame!");
                            memoryOwner.Dispose();
                        }
                    }
                    else
                    {
                        memoryOwner?.Dispose();
                    }
                }
            }
        }
        finally
        {
            _decoder.Dispose();
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
            DateTime time;

            try
            {
                (rgbOwner, width, height, time) = await _decodingOutput.Reader.ReadAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            Console.WriteLine($"bgra renting {width * height * 4}");
            var bgraOwner = MemoryPool<byte>.Shared.Rent(width * height * 4);

            try
            {
                unsafe
                {
                    fixed (byte* pRgbBytes = &rgbOwner.Memory.Span[0])
                    fixed (byte* pRgbaBytes = &bgraOwner.Memory.Span[0])
                    {
                        var pRgb = (Rgb*)pRgbBytes;
                        var pBgra = (Bgra*)pRgbaBytes;

                        var pixels = width * height;

                        for (var i = 0; i < pixels; i++)
                        {
                            pBgra->R = pRgb->R;
                            pBgra->G = pRgb->G;
                            pBgra->B = pRgb->B;
                            pBgra->A = 0xFF;

                            ++pRgb;
                            ++pBgra;
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

            if (!_conversionOutput.Writer.TryWrite((bgraOwner, width, height, time)))
            {
                _logger.LogError("Dropped converted frame!");
                bgraOwner.Dispose();
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
    public ref struct Bgra
    {
        public byte B;
        public byte G;
        public byte R;
        public byte A;
    }

    #endregion

    private void Close()
    {
        _cts.Cancel();
        _cts.Dispose();
        _texture?.Dispose();
        _task.Wait();
        _client.Connection.RemoveReceiver(this);
        OnRemove?.Invoke();
    }

    public void OnClosed()
    {
        Close();
    }
}
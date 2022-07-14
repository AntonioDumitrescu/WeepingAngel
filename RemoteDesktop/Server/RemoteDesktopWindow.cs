using System.Diagnostics;
using System.Drawing.Imaging;
using ImGuiNET;
using System.Numerics;
using System.Threading.Channels;
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
    private readonly IRemoteClient _client;
    private readonly IServerWindow _serverWindow;
    private readonly OpenH264BeginMessage _openMessage = new();
    private readonly DecoderWrapper _decoder = new();
    private readonly CancellationTokenSource _cts = new();

    private Bitmap? _latest = null;
    private readonly object _latestSync = new();
    private int _lastShownImageCode;

    private readonly GraphicsDevice _graphicsDevice;
    private Texture? _texture;
    private IntPtr? _binding;


    public RemoteDesktopWindow(IRemoteClient client, IServerWindow serverWindow)
    {
        _client = client;
        client.Connection.AddReceiver(this);
        _serverWindow = serverWindow;
        _graphicsDevice = serverWindow.GraphicsDevice;

        client.Connection.Send(_openMessage);
    }

    private void CreateTexture(int width, int height)
    {
        Console.WriteLine($"Creating tex {width} {height}");

        _texture = _graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            (uint)width,
            (uint)height,
            1,
            1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled));
    }

    private void UploadImage(Bitmap bmp)
    {
        var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        if (_texture != null)
        {
            if (_texture.Width != bmp.Width || _texture.Height != bmp.Height)
            {
                Console.WriteLine("Texture already exists");
                _texture.Dispose();
                CreateTexture(bmp.Width, bmp.Height);
            }
        }
        else
        {
            Console.WriteLine("creating first texture");
            CreateTexture(bmp.Width, bmp.Height);
        }
        
        _graphicsDevice.UpdateTexture(
            _texture, 
            data.Scan0, 
            (uint)(bmp.Width * bmp.Height *  4), 
            0, 
            0, 
            0,
            (uint)bmp.Width, 
            (uint)bmp.Height,
            1,
            0,
            0);

        _binding = _serverWindow.GetOrCreateImGuiBinding(_graphicsDevice.ResourceFactory, _texture!);

        Console.WriteLine("uploaded tex");


        bmp.UnlockBits(data);
        bmp.Save("c:\\users\\fnafm\\desktop\\img.png");
        bmp.Dispose();
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

            lock (_latestSync)
            {
                if (_latest != null)
                {
                    if (_latest.GetHashCode() != _lastShownImageCode)
                    {
                        _lastShownImageCode = _latest.GetHashCode();
                        UploadImage(_latest);
                    }
                    Console.WriteLine($"image: {_binding.Value}");
                    ImGui.Image(_binding!.Value, new Vector2(_texture!.Width, _texture.Height));
                }
            }
        }

        ImGui.End();
    }

    public event Action? OnRemove;
    public void RegisterHandlers(IHandlerRegister register)
    {
        register.Register<NalStreamMessage>(ReceiveStreamAsync);
    }

    private async ValueTask ReceiveStreamAsync(NalStreamMessage message)
    {
        foreach (var messageNal in message.Nals)
        {
            Bitmap? bmp = null;
            BitmapData? data = null;

            unsafe
            {
                fixed (byte* ptr = &messageNal[0])
                {
                    _decoder.DecodeToRgb(
                        ptr,
                        messageNal.Length,
                        out var state,
                        out _,
                        out _,
                        (w, h) =>
                        {
                            bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                            data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly,
                                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                            return data.Scan0;
                        });

                    if (state != DecodingState.DsErrorFree || bmp == null)
                    {
                        if (bmp != null)
                        {
                            bmp.UnlockBits(data!);
                            bmp.Dispose();
                        }

                        continue;
                    }
                }
            }

            bmp.UnlockBits(data!);

            lock (_latestSync)
            {
                _latest?.Dispose();
                _latest = bmp;
            }
        }
    }

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
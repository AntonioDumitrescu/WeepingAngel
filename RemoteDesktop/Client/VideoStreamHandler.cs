using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteDesktop.Messages;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace RemoteDesktop.Client;

internal sealed class VideoStreamHandler : IMessageReceiver
{
    private readonly ILogger<VideoStreamHandler> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IBilskirnir _client;
    private readonly BitmapPool _bitmapPool;

    private VideoStream? _stream;

    public VideoStreamHandler(
        ILogger<VideoStreamHandler> logger, 
        IServiceProvider serviceProvider,
        IBilskirnir client,
        BitmapPool bitmapPool)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _client = client;
        _bitmapPool = bitmapPool;
    }

    public void Start()
    {
        _logger.LogInformation("Setting up video stream handler.");
        _client.AddReceiver(this);
    }

    public void RegisterHandlers(IHandlerRegister register)
    {
        register.Register<OpenH264BeginMessage>(HandleOpenH264);
        register.Register<StreamEndMessage>(HandleStreamEnd);
    }

    private async ValueTask HandleStreamEnd(StreamEndMessage arg)
    {
        if (_stream == null)
        {
            _logger.LogCritical("Tried to close stream, but stream was null!");
            return;
        }

        await _stream.Close();
    }

    private async ValueTask HandleOpenH264(OpenH264BeginMessage message)
    {
        _logger.LogInformation("Received OpenH264 message.");

        if (_stream != null)
        {
            _logger.LogCritical("Already streaming! Re-creating stream.");
            await _stream.Close();
        }

        _logger.LogInformation("Creating frame source.");

        var frameSource = new GdiFrameSource(_bitmapPool);

        _logger.LogInformation("Creating encoder.");
     
        var encoder = new OpenH264Encoder(
            message.TargetBitRate, 
            message.MaxBitRate, 
            message.UsageType,
            message.IdrInterval);

        _logger.LogInformation("Creating video stream.");

        _stream = ActivatorUtilities.CreateInstance<VideoStream>(_serviceProvider, frameSource, encoder);

        _logger.LogInformation("Starting stream.");
        _stream.BeginStreaming();
    }

    public void OnClosed()
    {
        _logger.LogInformation("Closing video stream handler.");
        _stream?.Close().RunSynchronously();
    }
}
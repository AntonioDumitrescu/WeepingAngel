using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteDesktop.Messages;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;
using Yggdrasil.Api.Server;

namespace RemoteDesktop.Client;

internal sealed class VideoStreamService : IHostedService, IMessageReceiver
{
    private readonly ILogger<VideoStreamService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IRemoteClient _client;
    private readonly BitmapPool _bitmapPool;

    private VideoStream? _stream;

    public VideoStreamService(
        ILogger<VideoStreamService> logger, 
        IServiceProvider serviceProvider,
        IRemoteClient client,
        BitmapPool bitmapPool)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _client = client;
        _bitmapPool = bitmapPool;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting stream service.");


    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {

    }

    public void RegisterHandlers(IHandlerRegister register)
    {
        register.Register<OpenH264BeginMessage>(HandleOpenH264Message);
    }

    private async ValueTask HandleOpenH264Message(OpenH264BeginMessage message)
    {
        _logger.LogInformation("Received OpenH264 message.");

        if (_stream != null)
        {
            _logger.LogError("Already streaming!");
            return;
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
        
    }
}
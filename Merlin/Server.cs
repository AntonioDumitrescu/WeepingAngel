using System.Net;
using System.Text;
using Bilskirnir;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Merlin;

internal sealed class Server : IHostedService
{
    private readonly ILogger<Server> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Bilskirnir.Server _server;
    private Task? _listenTask;

    public Server(ILogger<Server> logger, ServerLaunchSettings launchSettings, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        _server = new Bilskirnir.Server(
            new IPEndPoint(
                IPAddress.Parse(launchSettings.Interface), 
                launchSettings.Port), 
            Encoding.UTF8.GetBytes(launchSettings.Password));

        _server.NewClient += NewClient;
    }

    private void NewClient(Client client)
    {
        var handler = ActivatorUtilities.CreateInstance<ClientConnectionHandler>(_serviceProvider, client);
        client.AddReceiver(handler);
        _logger.LogInformation("Added authentication receiver.");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating listen task.");
        _listenTask = _server.Listen();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping listener.");
        _server.Dispose();
        return _listenTask ?? Task.CompletedTask;
    }
}
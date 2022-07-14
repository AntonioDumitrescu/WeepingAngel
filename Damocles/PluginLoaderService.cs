using Bilskirnir;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Yggdrasil.Api.Setup;
using Yggdrasil.Messages.ClientToServer;

namespace Damocles;

internal sealed class PluginLoaderService : IHostedService
{
    private readonly ILogger<PluginLoaderService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<ClientPluginInfo> _pluginInfo;
    private readonly Client _client;

    public PluginLoaderService(ILogger<PluginLoaderService> logger, IServiceProvider serviceProvider, List<ClientPluginInfo> pluginInfo, Client client)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _pluginInfo = pluginInfo;
        _client = client;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting up plugins.");

        foreach (var clientPluginInfo in _pluginInfo)
        {
            _logger.LogInformation("Setting up {name}", clientPluginInfo.Name);

            clientPluginInfo.Instance =
                (ClientPluginBase)ActivatorUtilities.CreateInstance(_serviceProvider, clientPluginInfo.PluginType);

            await clientPluginInfo.Instance.StartAsync();
        }

        await _client.Send(new SetupComplete());
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var plugin in _pluginInfo.Where(plugin => plugin.Instance != null))
        {
            _logger.LogInformation("Stopping plugin {name}.", plugin.Name);
            await plugin.Instance!.StopAsync();
        }
    }
}
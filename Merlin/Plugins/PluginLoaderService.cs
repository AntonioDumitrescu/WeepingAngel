using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Yggdrasil.Api;
using Yggdrasil.Api.Setup;

namespace Merlin.Plugins;

internal sealed class PluginLoaderService : IHostedService
{
    private readonly ILogger<PluginLoaderService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<ServerPluginInfo> _plugins;

    public PluginLoaderService(
        ILogger<PluginLoaderService> logger, 
        IServiceProvider serviceProvider,
        List<ServerPluginInfo> plugins)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _plugins = plugins;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting up plugins.");

        var profiler = new Profiler();
        profiler.PushSection("Start");

        foreach (var serverPluginInfo in _plugins)
        {
            _logger.LogInformation("Setting up {name} by {author}", serverPluginInfo.FriendlyName, serverPluginInfo.Author);

            serverPluginInfo.Instance =
                (ServerPluginBase)ActivatorUtilities.CreateInstance(_serviceProvider, serverPluginInfo.PluginType);

            await serverPluginInfo.Instance.StartAsync();
        }

        _logger.LogInformation("Loaded {count} plugins in {time} milliseconds!", _plugins.Count, profiler.PopSectionRemove().ElapsedMilliseconds);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var plugin in _plugins.Where(plugin => plugin.Instance != null))
        {
            _logger.LogInformation("Stopping plugin {name}.", plugin.FriendlyName);
            await plugin.Instance!.StopAsync();
        }
    }
}
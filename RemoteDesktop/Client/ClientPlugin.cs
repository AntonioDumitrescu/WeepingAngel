using Microsoft.Extensions.DependencyInjection;
using Yggdrasil.Api.Setup;

namespace RemoteDesktop.Client;

internal sealed class ClientPlugin : ClientPluginBase
{
    private readonly VideoStreamHandler _handler;

    public ClientPlugin(IServiceProvider serviceProvider)
    {
        _handler = ActivatorUtilities.CreateInstance<VideoStreamHandler>(serviceProvider);
    }

    public override Task StartAsync()
    {
        _handler.Start();
        return Task.CompletedTask;
    }
}
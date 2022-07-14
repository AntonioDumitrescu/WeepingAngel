using Microsoft.Extensions.DependencyInjection;
using Yggdrasil.Api.Setup;

namespace RemoteTerminal.Client;

internal sealed class ClientPlugin : ClientPluginBase
{
    private readonly RemoteTerminalHandler _handler;

    public ClientPlugin(IServiceProvider serviceProvider)
    {
        _handler = ActivatorUtilities.CreateInstance<RemoteTerminalHandler>(serviceProvider);
    }

    public override Task StartAsync()
    {
        _handler.Start();
        return Task.CompletedTask;
    }
}
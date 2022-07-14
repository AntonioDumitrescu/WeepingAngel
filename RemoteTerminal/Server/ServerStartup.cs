using Microsoft.Extensions.DependencyInjection;
using Yggdrasil.Api.Setup;

namespace RemoteTerminal.Server;

internal sealed class ServerStartup : ServerPluginStartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<TerminalWindowManager>();
    }
}
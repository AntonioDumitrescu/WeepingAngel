using Microsoft.Extensions.DependencyInjection;
using Yggdrasil.Api.Setup;

namespace RemoteDesktop.Server;

public sealed class ServerStartup : ServerPluginStartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<RemoteDesktopWindowManager>();
    }
}
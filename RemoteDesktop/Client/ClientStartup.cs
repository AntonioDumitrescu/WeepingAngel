using Microsoft.Extensions.DependencyInjection;
using Yggdrasil.Api.Setup;

namespace RemoteDesktop.Client;

internal sealed class ClientStartup : ClientPluginStartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<BitmapPool>();
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Yggdrasil.Api.Setup;

public abstract class HostingSetup
{
    public virtual void ConfigureHost(IHostBuilder builder) { }

    /// <summary>
    ///     Used to register services for dependency injection.
    /// </summary>
    /// <param name="services"></param>
    public virtual void ConfigureServices(IServiceCollection services) { }
}
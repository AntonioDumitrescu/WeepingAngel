namespace Yggdrasil.Api.Setup;

public abstract class ClientPluginBase
{
    public virtual async Task StartAsync() { }

    public virtual async Task StopAsync() { }
}
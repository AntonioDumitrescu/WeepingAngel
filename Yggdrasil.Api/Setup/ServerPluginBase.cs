namespace Yggdrasil.Api.Setup;

/// <summary>
///     Plugins must implement one class that inherits from this one.
///     You may add any arguments in the constructor, as long as they are registered
///     in the dependency injection container.
/// </summary>
public abstract class ServerPluginBase
{
    public virtual async Task StartAsync() { }

    public virtual async Task StopAsync() { }
}
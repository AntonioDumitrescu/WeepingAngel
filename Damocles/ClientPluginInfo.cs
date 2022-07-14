using Yggdrasil.Api.Setup;

namespace Damocles;

internal sealed class ClientPluginInfo
{
    public ClientPluginStartupBase? Startup { get; }
    public Type PluginType { get; }
    public string Name { get; }
    public ClientPluginBase? Instance { get; set; }

    public ClientPluginInfo(ClientPluginStartupBase? startup, Type pluginType, string name)
    {
        Startup = startup;
        PluginType = pluginType;
        Name = name;
    }
}
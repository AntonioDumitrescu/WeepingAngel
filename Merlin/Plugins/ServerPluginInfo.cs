using System.Reflection;
using Yggdrasil.Api.Setup;

namespace Merlin.Plugins;

internal sealed class ServerPluginInfo
{
    private readonly ServerPluginAttribute _pluginAttribute;
    private readonly List<PackageDependencyAttribute> _dependencyAttributes;

    public ServerPluginInfo(ServerPluginStartupBase? startup, Type pluginType, string path, AssemblyName assemblyName)
    {
        Startup = startup;
        PluginType = pluginType;
        Path = path;
        AssemblyName = assemblyName;
        _pluginAttribute = pluginType.GetCustomAttribute<ServerPluginAttribute>()!;
        _dependencyAttributes = pluginType.GetCustomAttributes<PackageDependencyAttribute>().ToList();
    }

    public string FriendlyName => _pluginAttribute.FriendlyName;
    public string Package => _pluginAttribute.Package;
    public string Version => _pluginAttribute.Version;
    public string Author => _pluginAttribute.Author;

    public bool HasDependencies => _dependencyAttributes.Count > 0;
    public List<PackageDependencyAttribute> Dependencies => _dependencyAttributes.ToList();

    public ServerPluginStartupBase? Startup { get; }
    public Type PluginType { get; }
    public string Path { get; }
    public AssemblyName AssemblyName { get; }

    public ServerPluginBase? Instance { get; set; }
}
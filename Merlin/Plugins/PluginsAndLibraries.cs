namespace Merlin.Plugins;

internal sealed class PluginsAndLibraries
{
    public ServerPluginInfo[] Plugins { get; }

    public PluginLoader.AssemblyInfo[] Libraries { get; }

    public PluginsAndLibraries(ServerPluginInfo[] plugins, PluginLoader.AssemblyInfo[] libraries)
    {
        Plugins = plugins;
        Libraries = libraries;
    }
}
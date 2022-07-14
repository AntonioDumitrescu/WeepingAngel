using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Yggdrasil.Api;
using Yggdrasil.Api.Setup;

namespace Merlin.Plugins;

internal static class PluginLoader
{
    public class AssemblyInfo
    {
        public AssemblyName AssemblyName { get; }

        public string Path { get; }

        public bool IsPlugin { get; }

        public AssemblyInfo(AssemblyName assemblyName, string path, bool isPlugin)
        {
            AssemblyName = assemblyName;
            Path = path;
            IsPlugin = isPlugin;
        }

        private Assembly? _assembly;

        public Assembly GetOrLoad(AssemblyLoadContext context)
        {
            if (_assembly != null) return _assembly!;

            using var stream = File.Open(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            _assembly = context.LoadFromStream(stream);
            return _assembly;
        }
    }


    private static readonly ILogger Logger = Log.ForContext(typeof(PluginLoader));

    public static IHostBuilder UsePluginLoader(this IHostBuilder builder)
    {
        Logger.Information("Bootstrapping plugin loader!");
        var profiler = new Profiler();

        var context = AssemblyLoadContext.Default;

        var pluginPath = "plugins";
        var libraryPath = "libraries";

        if (!Directory.Exists(pluginPath))
        {
            Directory.CreateDirectory(pluginPath);
        }
        if (!Directory.Exists(libraryPath))
        {
            Directory.CreateDirectory(libraryPath);
        }

        profiler.PushSection("Probe");

        var assemblyInfo = GetAssemblies(pluginPath, true);
        var libInfo = GetAssemblies(libraryPath, false).ToArray();

        assemblyInfo = assemblyInfo.Concat(libInfo).ToList();
       
        var assemblyInfoLookup = assemblyInfo
            .Where(x=>!string.IsNullOrEmpty(x.AssemblyName.Name))
            .ToDictionary(x => x.AssemblyName.Name!);

        Logger.Information("Probing files took {time} ms.", profiler.PopSectionRemove().ElapsedMilliseconds);

        // solve dependencies (libraries)
        context.Resolving += (loadContext, assemblyName) =>
        {
            Logger.Verbose("Loading assembly {name} {version}", assemblyName.Name, assemblyName.Version);

            if (assemblyName.Name == "Yggdrasil.Api") { return typeof(ServerPluginStartupBase).Assembly; }

            if (assemblyName.Name == null || !assemblyInfoLookup.TryGetValue(assemblyName.Name, out var inf))
            {
                return null;
            }

            return inf.GetOrLoad(loadContext);
        };

        profiler.PushSection("Streaming...");

        var pluginAssemblies = assemblyInfo
            .Where(x => x.IsPlugin)
            .Select(x => (context.LoadFromAssemblyName(x.AssemblyName), x.Path))
            .ToList();

        Logger.Information("Streaming assemblies from the disk took {time} ms.", profiler.PopSectionRemove().ElapsedMilliseconds);

        var pluginInformation = new List<ServerPluginInfo>();

        profiler.PushSection("Scanning...");

        foreach (var (pluginAssembly, p) in pluginAssemblies)
        {
            // get the type for the startup class
            var startup = pluginAssembly
                .GetTypes()
                .Where(x =>
                    typeof(ServerPluginStartupBase).IsAssignableFrom(x)
                    && x.IsClass)
                .ToList();

            if (startup.Count > 1)
            {
                Logger.Error("Plugin {path} contains more than 1 startup! Skipped loading.", pluginAssembly.Location);
                continue;
            }


            // get the type for the plugin class
            var plugin = pluginAssembly
                .GetTypes()
                .Where(x =>
                    typeof(ServerPluginBase).IsAssignableFrom(x)
                    && x.IsClass
                    && !x.IsAbstract
                    && x.GetCustomAttribute<ServerPluginAttribute>() != null)
                .ToList();

            if (plugin.Count != 1)
            {
                Logger.Error("Plugin {path} must have 1 plugin, but it has {count}! Skipping.", pluginAssembly.Location, plugin.Count);
                continue;
            }

            pluginInformation.Add(
                new ServerPluginInfo(
                    startup
                        .Select(Activator.CreateInstance)   // create instance
                        .Cast<ServerPluginStartupBase>()    // cast to desired type
                        .FirstOrDefault(),                  // should only be one or null
                    plugin.First(), p, pluginAssembly.GetName()));
        }

        Logger.Information("Scanning plugins took {time} ms.", profiler.PopSectionRemove().ElapsedMilliseconds);

        profiler.PushSection("Ordering");
        var ordered = LoadUsingDependencies(pluginInformation);
        Logger.Information("Ordering dependencies took {time} ms.", profiler.PopSectionRemove().ElapsedMilliseconds);

        profiler.PushSection("Configure Host");
        foreach (var plugin in ordered)
        {
            plugin.Startup?.ConfigureHost(builder);
        }
        Logger.Information("Configuring hosts took {time} ms.", profiler.PopSectionRemove().ElapsedMilliseconds);

        builder.ConfigureServices(services =>
        {
            services.AddSingleton(new PluginsAndLibraries(ordered.ToArray(), libInfo));

            // this will dispatch loading to the plugins
            services.AddHostedService(provider => ActivatorUtilities.CreateInstance<PluginLoaderService>(provider, ordered));

            profiler.PushSection("Configure Services");
            foreach (var plugin in ordered)
            {
                plugin.Startup?.ConfigureServices(services);
            }
            Logger.Information("Configuring services took {time} ms.", profiler.PopSectionRemove().ElapsedMilliseconds);
        });

        return builder;
    }

    private static List<ServerPluginInfo> LoadUsingDependencies(IEnumerable<ServerPluginInfo> plugins)
    {
        var pluginDictionary = new Dictionary<string, ServerPluginInfo>();
        var hardDependencies = new Dictionary<string, List<string>>();

        foreach (var plugin in plugins)
        {
            pluginDictionary[plugin.Package] = plugin;

            hardDependencies[plugin.Package] = plugin
                .Dependencies
                .Where(p => p.Type == PackageDependencyAttribute.DependencyType.Required)
                .Select(p => p.Package)
                .ToList();
        }

        var presentPlugins = pluginDictionary.Keys.ToList();

        // Check whether the Required Dependencies are present and remove those without.
        var checkedPlugins = CheckHardDependencies(presentPlugins, hardDependencies);

        var dependencyGraph = checkedPlugins.ToDictionary(p => p, _ => new List<string>());

        foreach (var plugin in checkedPlugins)
        {
            foreach (var dependency in pluginDictionary[plugin].Dependencies.Where(d => checkedPlugins.Contains(d.Package)))
            {
                if (dependency.Type == PackageDependencyAttribute.DependencyType.Optional)
                {
                    dependencyGraph[dependency.Package].Add(plugin);
                }
                else
                {
                    dependencyGraph[plugin].Add(dependency.Package);
                }
            }
        }

        var processed = new List<string>();
        var ordered = new List<ServerPluginInfo>();

        foreach (var plugin in checkedPlugins.Where(plugin => !processed.Contains(plugin)))
        {
            RecursiveOrder(plugin, dependencyGraph, processed, ordered, pluginDictionary);
        }

        return ordered;
    }

    private static List<string> CheckHardDependencies(
        List<string> plugins,
        IReadOnlyDictionary<string, List<string>> hardDependencies)
    {
        foreach (var plugin in plugins)
        {
            if (!hardDependencies.ContainsKey(plugin))
            {
                continue;
            }

            foreach (var dependency in hardDependencies[plugin].Where(dependency => !plugins.Contains(dependency)))
            {
                Logger.Error(
                    "The plugin {plugin} has defined the plugin {dependency} as a hard dependency but its not present! {plugin} will not loaded.",
                    plugin,
                    dependency,
                    plugin
                );

                // Remove the plugin from the plugins to load.
                plugins.Remove(plugin);

                // Since other plugins might have defined the removed plugin as a hard dependency a recheck is necessary.
                return CheckHardDependencies(plugins, hardDependencies);
            }
        }

        return plugins;
    }

    private static void RecursiveOrder(
        string plugin,
        IReadOnlyDictionary<string, List<string>> dependencyGraph,
        ICollection<string> processed,
        ICollection<ServerPluginInfo> ordered,
        IReadOnlyDictionary<string, ServerPluginInfo> pluginDictionary)
    {
        processed.Add(plugin);

        foreach (var dependency in dependencyGraph[plugin].Where(dependency => !processed.Contains(dependency)))
        {
            RecursiveOrder(dependency, dependencyGraph, processed, ordered, pluginDictionary);
        }

        ordered.Add(pluginDictionary[plugin]);
    }

    private static List<AssemblyInfo> GetAssemblies(string path, bool isPlugin)
    {
        var results = new List<AssemblyInfo>();

        var files = Directory.EnumerateFiles(path)
            .Where(x => x.EndsWith(".dll"))
            .Where(x => !x.EndsWith("Yggdrasil.Api.dll"))
            .ToList();

        Parallel.ForEach(files, file =>
        {
            AssemblyName assemblyName;

            try
            {
                assemblyName = AssemblyName.GetAssemblyName(file);
            }
            catch (BadImageFormatException)
            {
                Logger.Error("Bad image format for {file}!", file);
                return;
            }

            lock (results) results.Add(new AssemblyInfo(assemblyName, file, isPlugin));
        });

        return results;
    }
}
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Yggdrasil.Api.Setup;
using Yggdrasil.Messages.ServerToClient;

namespace Damocles;

internal static class PluginLoader
{
    public static IHostBuilder UsePluginLoader(this IHostBuilder hostBuilder, List<AssemblyStreamMessage> pluginBinaries, List<AssemblyStreamMessage> libraryBinaries)
    {
        var context = new AssemblyLoadContext(Guid.NewGuid().ToString(), true);
        var assemblyInfoLookup =
            pluginBinaries
                .ToDictionary(x => x.Name!);

        foreach (var (key, value) in libraryBinaries.ToDictionary(x => x.Name!))
        {
            assemblyInfoLookup.Add(key, value);
        }

        context.Resolving += (loadContext, assemblyName) =>
        {
            Log.Information("Loading assembly {name} {version}", assemblyName.Name, assemblyName.Version);

            try
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
                if (assembly != null)
                {
                    Log.Information("Found within default context.");
                    return assembly;
                }
            }
            catch (Exception e)
            {
                // ignored
            }

            if (assemblyName.Name == "Yggdrasil.Api") { return typeof(ServerPluginStartupBase).Assembly; }

            if (assemblyName.Name == null || !assemblyInfoLookup.TryGetValue(assemblyName.Name, out var inf))
            {
                return null;
            }

            return inf.GetOrLoad(loadContext);
        };

        // then load the plugins

        var pluginAssemblies = pluginBinaries
            .Select(x => (x.GetOrLoad(context), x.Name))
            .ToList();

        var pluginInformation = new List<ClientPluginInfo>();

        foreach (var (pluginAssembly, name) in pluginAssemblies)
        {
            // get the type for the startup class
            var startup = pluginAssembly
                .GetTypes()
                .Where(x =>
                    typeof(ClientPluginStartupBase).IsAssignableFrom(x)
                    && x.IsClass)
                .ToList();

            if (startup.Count > 1)
            {
                Log.Error("Plugin {path} contains more than 1 startup! Skipped loading.", name);
                continue;
            }


            var plugin = pluginAssembly
                .GetTypes()
                .Where(x =>
                    typeof(ClientPluginBase).IsAssignableFrom(x)
                    && x.IsClass
                    && !x.IsAbstract
                    )
                .ToList();

            if (plugin.Count != 1)
            {
                Log.Error("Plugin {path} must have 1 plugin, but it has {count}! Skipping.", name, plugin.Count);
                continue;
            }

            pluginInformation.Add(
                new ClientPluginInfo(
                    startup
                        .Select(Activator.CreateInstance)   // create instance
                        .Cast<ClientPluginStartupBase>()    // cast to desired type
                        .FirstOrDefault(),                  // should only be one or null
                    plugin.First(), name!));
        }

        Log.Information("Configuring hosts.");
        
        foreach (var startup in pluginInformation.Select(x=>x.Startup))
        {
            startup?.ConfigureHost(hostBuilder);
        }

        hostBuilder.ConfigureServices(services =>
        {
            services.AddHostedService(_ => new UnloadService(context));
            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<PluginLoaderService>(sp, pluginInformation));
            Log.Information("Configuring services.");
            foreach (var startup in pluginInformation.Select(x=>x.Startup))
            {
                startup?.ConfigureServices(services);
            }
        });

        return hostBuilder;
    }

    class UnloadService : IHostedService
    {
        private readonly AssemblyLoadContext _ctx;

        public UnloadService(AssemblyLoadContext ctx)
        {
            _ctx = ctx;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _ctx.Unload();
            Log.Warning("Unloaded assemblies!");
            return Task.CompletedTask;
        }
    }
}
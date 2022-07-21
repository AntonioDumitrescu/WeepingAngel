using System.Net;
using System.Text.Json;
using Merlin.Extensions;
using Merlin.Gui;
using Merlin.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Yggdrasil.Api.Events.System;
using Yggdrasil.Api.Server;
using Yggdrasil.Events;

namespace Merlin;

internal static class Program
{
    private static readonly ILogger Logger = Log.ForContext(typeof(Program));
    private const string ConfigPath = "CONFIG.json";

    static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .Enrich.FromLogContext()
            .WriteTo.File("logs/logs.txt",
                LogEventLevel.Debug,
                rollingInterval: RollingInterval.Hour)
            .WriteTo.Console()
            .WriteTo.Aggregator()
            .CreateLogger();

        if (!File.Exists(ConfigPath))
        {
            var defaultConfig = new ServerLaunchSettings("0.0.0.0", 6666, "password");
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(defaultConfig));
            Log.Fatal("Not configured. Please edit \"{0}\"", ConfigPath);
            Thread.Sleep(3000);
            return;
        }

        var config = JsonSerializer.Deserialize<ServerLaunchSettings>(File.ReadAllText(ConfigPath));

        if (config == null)
        {
            Log.Fatal("Failed to deserialize config. Deleting...");
            File.Delete(ConfigPath);
            Thread.Sleep(3000);
            return;
        }

        if (config.Port is < 1 or > ushort.MaxValue)
        {
            Log.Fatal("Invalid port: {0}", config.Port);
            Thread.Sleep(3000);
            return;
        }

        Log.Information("PORT: {0} ADDRESS: \"{1}\" PASSWORD: \"{2}\"", config.Port, config.Interface, config.Password);

        var host = CreateHostBuilder(args, config).Build();

        try
        {
            host.Run();
        }
        catch (Exception e)
        {
            Logger.Fatal("Exception occurred: {e}", e);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args, ServerLaunchSettings settings)
    {
        return Host.CreateDefaultBuilder(args)
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureServices((host, services) =>
            {
                services.AddSingleton(settings);

                services.AddSingleton<EventManager>();
                services.AddSingleton<IEventManager>(sp => sp.GetRequiredService<EventManager>());

                services.AddSingleton<ClientManager>();
                services.AddHostedService<Server>();

                services.AddSingleton<MainWindow>();
                services.AddSingleton<IServerWindow>(sp => sp.GetRequiredService<MainWindow>());
                services.AddHostedService(sp => sp.GetRequiredService<MainWindow>());
            })
            .UseSerilog()
            .UseConsoleLifetime()
            .UsePluginLoader();
    }
}
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

        var host = CreateHostBuilder(args).Build();

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

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureServices((host, services) =>
            {
                services.AddSingleton(new ServerSettings("0.0.0.0", 666, ""));

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
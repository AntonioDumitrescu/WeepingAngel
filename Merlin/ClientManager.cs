using System.Collections.Concurrent;
using Bilskirnir;
using Microsoft.Extensions.Logging;
using Transport;
using Yggdrasil.Messages.ClientToServer;

namespace Merlin;

internal sealed class ClientManager
{
    private readonly ILogger<ClientManager> _logger;

    private readonly ConcurrentDictionary<RemoteControlClient, byte> _clients = new();

    public ICollection<RemoteControlClient> Clients => _clients.Keys;

    public ClientManager(ILogger<ClientManager> logger)
    {
        _logger = logger;
    }

    public void AddConnection(Client incomingClient, AuthenticationInformation information)
    {
        var client = new RemoteControlClient(incomingClient, information);
        _clients.TryAdd(client, 0);
        incomingClient.Disconnected += ((_, reason, ex) => OnDisconnected(client, reason, ex));
    }

    private void OnDisconnected(RemoteControlClient c, DisconnectedReason reason, Exception? ex)
    {
        _logger.LogWarning("Client {c} disconnected because of {reason}, with exception {ex}", c, reason, ex);
        _clients.TryRemove(c, out _);
    }
}
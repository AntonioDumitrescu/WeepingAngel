using Bilskirnir;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;
using Yggdrasil.Api.Server;
using Yggdrasil.Messages.ClientToServer;

namespace Merlin;

internal sealed class RemoteControlClient : IRemoteClient
{
    public Client Connection { get; }

    public AuthenticationInformation AuthenticationInformation { get; }

    IBilskirnir IRemoteClient.Connection => Connection;

    string IRemoteClient.Username => AuthenticationInformation.UserAccount;

    public RemoteControlClient(Client connection, AuthenticationInformation information)
    {
        Connection = connection;
        AuthenticationInformation = information;
    }
}
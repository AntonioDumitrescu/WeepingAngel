using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace Yggdrasil.Api.Server;

public interface IRemoteClient
{
    IBilskirnir Connection { get; }

    string Username { get; }
}
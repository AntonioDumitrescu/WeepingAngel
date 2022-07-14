using Yggdrasil.Api.Client;

namespace Yggdrasil.Api.Server;

public interface IRemoteClient
{
    IBilskirnir Connection { get; }

    string Username { get; }
}
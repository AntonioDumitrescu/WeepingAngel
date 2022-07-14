using Transport;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace Yggdrasil.Messages.ServerToClient;

public struct AuthenticationResult : IMessage
{
    public bool Accepted { get; private set; }

    public AuthenticationResult(bool accepted)
    {
        Accepted = accepted;
    }

    public void Serialize(IPacketWriter writer)
    {
        writer.WriteBool(Accepted);
    }

    public void Deserialize(IPacketReader reader)
    {
        Accepted = reader.ReadBool();
    }
}
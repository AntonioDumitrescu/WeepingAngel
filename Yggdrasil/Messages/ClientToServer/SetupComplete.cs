using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace Yggdrasil.Messages.ClientToServer;

public struct SetupComplete : IMessage
{
    public void Serialize(IPacketWriter writer) { }

    public void Deserialize(IPacketReader reader) { }
}
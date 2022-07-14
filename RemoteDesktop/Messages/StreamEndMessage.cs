using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace RemoteDesktop.Messages;

public class StreamEndMessage : IMessage
{
    public void Serialize(IPacketWriter writer)
    {
        writer.WriteBool(false);
    }

    public void Deserialize(IPacketReader reader)
    {
        reader.ReadBool();
    }
}
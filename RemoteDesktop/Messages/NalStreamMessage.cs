using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace RemoteDesktop.Messages;

internal struct NalStreamMessage : IMessage
{
    public List<byte[]> Nals { get; private set; }

    public NalStreamMessage(List<byte[]> nals)
    {
        Nals = nals;
    }

    public NalStreamMessage()
    {
        Nals = new List<byte[]>();
    }

    public void Serialize(IPacketWriter writer)
    {
        writer.WriteInt(Nals.Count);

        foreach (var nal in Nals)
        {
            writer.WriteBytes(nal);
        }
    }

    public void Deserialize(IPacketReader reader)
    {
        var count = reader.ReadInt();

        for (var i = 0; i < count; i++)
        {
            Nals.Add(reader.ReadBytes());
        }
    }
}
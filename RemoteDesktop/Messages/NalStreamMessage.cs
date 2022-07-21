using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace RemoteDesktop.Messages;

internal struct NalStreamMessage : IMessage
{
    public List<byte[]> Nals { get; private set; }

    public DateTime Time { get; set; }

    public NalStreamMessage(List<byte[]> nals, DateTime time)
    {
        Nals = nals;
        Time = time;
    }

    public NalStreamMessage()
    {
        Nals = new List<byte[]>();
        Time = DateTime.MinValue;
    }

    public void Serialize(IPacketWriter writer)
    {
        writer.WriteInt(Nals.Count);

        foreach (var nal in Nals)
        {
            writer.WriteBytes(nal);
        }

        writer.WriteLong(Time.ToBinary());
    }

    public void Deserialize(IPacketReader reader)
    {
        var count = reader.ReadInt();

        for (var i = 0; i < count; i++)
        {
            Nals.Add(reader.ReadBytes());
        }

        Time = DateTime.FromBinary(reader.ReadLong());
    }
}
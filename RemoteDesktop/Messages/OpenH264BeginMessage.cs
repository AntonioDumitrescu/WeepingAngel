using OpenH264.Intermediaries;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace RemoteDesktop.Messages;

internal sealed class OpenH264BeginMessage : IMessage
{
    public int TargetBitRate { get; set; } = 5000;

    public int MaxBitRate { get; set; } = 5000;

    public int IdrInterval { get; set; } = 2;

    public UsageType UsageType { get; set; } = UsageType.ScreenContentRealTime;

    public void Serialize(IPacketWriter writer)
    {
        writer.WriteInt(TargetBitRate);
        writer.WriteInt(MaxBitRate);
        writer.WriteInt(IdrInterval);
        writer.WriteInt((int)UsageType);
    }

    public void Deserialize(IPacketReader reader)
    {
        TargetBitRate = reader.ReadInt();
        MaxBitRate = reader.ReadInt();
        IdrInterval = reader.ReadInt();
        UsageType = (UsageType)reader.ReadInt();
    }
}
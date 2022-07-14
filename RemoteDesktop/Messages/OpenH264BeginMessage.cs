using OpenH264.Intermediaries;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace RemoteDesktop.Messages;

internal sealed class OpenH264BeginMessage : IMessage 
{
    public int TargetWidth { get; private set; }

    public int TargetHeight { get; private set; }

    public int TargetFps { get; private set; }

    public int TargetBitRate { get; private set; }

    public int MaxBitRate { get; private set; }

    public int IdrInterval { get; private set; }

    public UsageType UsageType { get; private set; }

    public void Serialize(IPacketWriter writer)
    {
        writer.WriteInt(TargetWidth);
        writer.WriteInt(TargetHeight);
        writer.WriteInt(TargetFps);
        writer.WriteInt(TargetBitRate);
        writer.WriteInt(MaxBitRate);
        writer.WriteInt(IdrInterval);
        writer.WriteInt((int)UsageType);
    }

    public void Deserialize(IPacketReader reader)
    {
        TargetWidth = reader.ReadInt();
        TargetHeight = reader.ReadInt();
        TargetFps = reader.ReadInt();
        TargetBitRate = reader.ReadInt();
        MaxBitRate = reader.ReadInt();
        IdrInterval = reader.ReadInt();
        UsageType = (UsageType)reader.ReadInt();
    }
}
using OpenH264.Intermediaries;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace RemoteDesktop.Messages;

internal sealed class OpenH264BeginMessage : IMessage
{
    public int TargetWidth { get; set; } = 1280;

    public int TargetHeight { get; set; } = 720;

    public int TargetFps { get; set; } = 30;

    public int TargetBitRate { get; set; } = 5000;

    public int MaxBitRate { get; set; } = 5000;

    public int IdrInterval { get; set; } = 2;

    public UsageType UsageType { get; set; } = UsageType.ScreenContentRealTime;

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
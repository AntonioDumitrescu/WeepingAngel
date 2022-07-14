using System.Text;

namespace Yggdrasil.Api.Client;

public interface IPacketReader
{
    int Length { get; }
    byte[] Data { get; }
    int Remaining { get; }
    Span<byte> Span { get; }
    int Position { get; set; }
    byte ReadByte();
    bool ReadBool();
    int ReadInt();
    int ReadPackedInt();
    uint ReadUInt();
    uint ReadPackedUInt();
    short ReadShort();
    ushort ReadUShort();
    long ReadLong();
    ulong ReadULong();
    byte[] ReadBytes(int length = 0);
    byte[]? ReadNullableBytes();
    byte[][] ReadBytesArray();
    string ReadString(Encoding encoding);
    string ReadString();
    string? ReadNullableString();
    T ReadEnum<T>() where T : Enum;
}

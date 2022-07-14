using System.Runtime.Serialization;
using System.Text;

namespace Yggdrasil.Api.Networking;

public interface IPacketWriter
{
    void WriteByte(byte @byte);
    void WriteShort(short @short);
    void WriteUShort(ushort @ushort);
    void WriteInt(int @int);
    void WritePackedInt(int @int);
    void WriteUInt(uint @uint);
    void WritePackedUInt(uint @uint);
    void WriteLong(long @long);
    void WriteULong(ulong @long);
    void WriteBytes(ReadOnlySpan<byte> bytes, bool withLength = true);
    void WriteNullableBytes(byte[]? bytes);
    void WriteString(string str, Encoding encoding);
    void WriteString(string str);
    void WriteNullableString(string? str);
    void WriteBool(bool @bool);
    void WriteEnum<T>(T @enum) where T : Enum;
    int Length { get; }
    byte[] Compile();
    void Dispose();
}
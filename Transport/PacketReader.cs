using System.Buffers.Binary;
using System.Text;
using Yggdrasil.Api.Client;

namespace Transport;

public sealed class PacketReader : IPacketReader
{
    public PacketReader(byte[] data, int length)
    {
        Data = data;
        Length = length;
    }

    public int Length { get; }

    public byte[] Data { get; }

    public int Remaining => Length - Position;

    public Span<byte> Span => Data.AsSpan(Position, Remaining);

    public int Position { get; set; }

    public byte ReadByte()
    {
        var @byte = Span[0];
        Position++;
        return @byte;
    }

    public bool ReadBool()
    {
        return ReadByte() == 1;
    }

    public int ReadInt()
    {
        var @int = BinaryPrimitives.ReadInt32LittleEndian(Span);
        Position += sizeof(int);
        return @int;
    }

    public int ReadPackedInt()
    {
        unchecked
        {
            return (int)ReadPackedUInt();
        }
    }

    public uint ReadUInt()
    {
        var @uint = BinaryPrimitives.ReadUInt32LittleEndian(Span);
        Position += sizeof(uint);
        return @uint;
    }

    public uint ReadPackedUInt()
    {
        var readMore = true;
        var shift = 0;
        var output = 0u;

        while (readMore)
        {
            var b = ReadByte();
            if (b >= 0x80)
            {
                readMore = true;
                b ^= 0x80;
            }
            else
            {
                readMore = false;
            }

            output |= (uint)(b << shift);
            shift += 7;
        }

        return output;
    }

    public short ReadShort()
    {
        var @short = BinaryPrimitives.ReadInt16LittleEndian(Span);
        Position += sizeof(short);
        return @short;
    }

    public ushort ReadUShort()
    {
        var @ushort = BinaryPrimitives.ReadUInt16LittleEndian(Span);
        Position += sizeof(ushort);
        return @ushort;
    }

    public long ReadLong()
    {
        var @long = BinaryPrimitives.ReadInt64LittleEndian(Span);
        Position += sizeof(long);
        return @long;
    }

    public ulong ReadULong()
    {
        var @ulong = BinaryPrimitives.ReadUInt64LittleEndian(Span);
        Position += sizeof(ulong);
        return @ulong;
    }

    public byte[] ReadBytes(int length = 0)
    {
        if (length == 0) length = ReadInt();

        if (length == 0)
        {
            return Array.Empty<byte>();
        }

        var bytes = new byte[length];
        Span[..length].CopyTo(bytes);
        Position += length;

        return bytes;
    }

    public byte[]? ReadNullableBytes()
    {
        var length = ReadInt();

        if (length == -1) return null;

        return ReadBytes(length);
    }

    public byte[][] ReadBytesArray()
    {
        var size = ReadInt();
        var result = new byte[size][];

        for (var i = 0; i < size; i++) result[i] = ReadBytes();

        return result;
    }

    public string ReadString(Encoding encoding)
    {
        var bytes = ReadBytes();

        return encoding.GetString(bytes);
    }

    public string ReadString()
    {
        return ReadString(Encoding.UTF8);
    }

    public string? ReadNullableString()
    {
        var bytes = ReadNullableBytes();
        return bytes == null ? null : Encoding.UTF8.GetString(bytes);
    }

    public T ReadEnum<T>() where T : Enum
    {
        return (T)Enum.Parse(typeof(T), ReadString());
    }
}
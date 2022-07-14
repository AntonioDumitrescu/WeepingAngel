using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.IO;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace Transport;

public sealed class PacketWriter : IDisposable, IPacketWriter
{
    public const int MaxPacketSize = 1024 * 1024 * 50;

    private static readonly RecyclableMemoryStreamManager BufferManager = new();
    private readonly MemoryStream _buffer = new RecyclableMemoryStream(BufferManager);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void WriteByte(byte @byte)
    {
        CheckLength(1);
        _buffer.WriteByte(@byte);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void WriteShort(short @short)
    {
        CheckLength(2);
        var bytes = (Span<byte>)stackalloc byte[2];
        BinaryPrimitives.WriteInt16LittleEndian(bytes, @short);
        _buffer.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void WriteUShort(ushort @ushort)
    {
        CheckLength(2);
        var bytes = (Span<byte>)stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, @ushort);
        _buffer.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void WriteInt(int @int)
    {
        CheckLength(4);
        var bytes = (Span<byte>)stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, @int);
        _buffer.Write(bytes);
    }

    public void WritePackedInt(int @int)
    {
        unchecked
        {
            WritePackedUInt((uint)@int);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void WriteUInt(uint @uint)
    {
        CheckLength(4);
        var bytes = (Span<byte>)stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, @uint);
        _buffer.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void WritePackedUInt(uint @uint)
    {
        do
        {
            var b = (byte)(@uint & 0xFF);
            if (@uint >= 0x80)
            {
                b |= 0x80;
            }

            WriteByte(b);
            @uint >>= 7;
        } while (@uint > 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void WriteLong(long @long)
    {
        CheckLength(8);
        var bytes = (Span<byte>)stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, @long);
        _buffer.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void WriteULong(ulong @long)
    {
        CheckLength(8);
        var bytes = (Span<byte>)stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, @long);
        _buffer.Write(bytes);
    }

    public void WriteBytes(ReadOnlySpan<byte> bytes, bool withLength = true)
    {
        if (withLength)
        {
            CheckLength(bytes.Length + 4);
            WriteInt(bytes.Length);
        }
        else
        {
            CheckLength(bytes.Length);
        }
       
        _buffer.Write(bytes);
    }

    public void WriteNullableBytes(byte[]? bytes)
    {
        if (bytes == null)
        {
            CheckLength(4);
            WriteInt(-1);
            return;
        }

        WriteBytes(bytes);
    }

    public void WriteString(string str, Encoding encoding)
    {
        WriteBytes(encoding.GetBytes(str));
    }

    public void WriteString(string str)
    {
        WriteString(str, Encoding.UTF8);
    }

    public void WriteNullableString(string? str)
    {
        WriteNullableBytes(str == null ? null : Encoding.UTF8.GetBytes(str));
    }

    public void WriteBool(bool @bool)
    {
        WriteByte((byte)(@bool ? 1 : 0));
    }

    public void WriteEnum<T>(T @enum) where T : Enum
    {
        //todo: this is seriously lazy

        WriteString(@enum.ToString());
    }

    public int Length => (int)_buffer.Length;

    internal long Position
    {
        get => _buffer.Position;
        set => _buffer.Seek(value, SeekOrigin.Begin);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void CheckLength(int delta)
    {
        if (_buffer.Length + delta > MaxPacketSize)
        {
            throw new Exception($"Packet exceeded max size ({_buffer.Length}, max: {MaxPacketSize}).");
        }
    }

    public byte[] Compile()
    {
        return _buffer.ToArray();
    }

    public void Dispose()
    {
        _buffer.Dispose();
    }
}
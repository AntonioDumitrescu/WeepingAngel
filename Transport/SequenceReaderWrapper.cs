using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Transport;

public struct SequenceReaderWrapper
{
    public ReadOnlySequence<byte> Sequence { get; }

    private SequenceReader<byte> Reader => new(Sequence.Slice(Consumed));

    public SequencePosition Position => Reader.Position;

    public int Consumed { get; private set; }

    public int Available => (int)(Sequence.Length - Consumed);

    public SequenceReaderWrapper(ReadOnlySequence<byte> sequence)
    {
        Sequence = sequence;
        Consumed = 0;
    }

    #region Read Primitives

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
        if (!Reader.TryRead(out var @byte)) throw new InvalidOperationException("Ran out of data to consume!");

        Consumed++;

        return @byte;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte()
    {
        return (sbyte)ReadByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool()
    {
        return ReadByte() == 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShort()
    {
        var reader = Reader;
        if (reader.TryReadLittleEndian(out short @short))
            throw new InvalidOperationException("Ran out of data to consume!");

        Consumed += 2;

        return @short;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort()
    {
        return (ushort)ReadShort();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt()
    {
        var reader = Reader;
        if (!reader.TryReadLittleEndian(out int @int))
            throw new InvalidOperationException("Ran out of data to consume!");

        Consumed += 4;

        return @int;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt()
    {
        return (uint)ReadInt();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLong()
    {
        var reader = Reader;
        if (!reader.TryReadLittleEndian(out long @long))
            throw new InvalidOperationException("Ran out of data to consume!");

        Consumed += 8;

        return @long;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadULong()
    {
        return (ulong)ReadLong();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat()
    {
        var @int = ReadInt();
        return BitConverter.Int32BitsToSingle(@int);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble()
    {
        unsafe
        {
            var @long = ReadLong();

            return *(double*)&@long;
        }
    }

    #endregion

    #region Read Primitive Arrays

    /// <summary>
    ///     Reads the data, allocates a buffer and copies it to the buffer.
    /// </summary>
    /// <param name="length">The size of the data to read. If 0, an integer will be read from the buffer.</param>
    /// <returns>An array containing the data.</returns>
    /// <exception cref="InvalidOperationException">If there is no data available, or the copy fails.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] ReadBytes(int length = 0)
    {
        if (length == 0) length = ReadInt();

        var bytes = new byte[length];
        var destination = bytes.AsSpan();

        var sequencePosition = Reader.Position;
        var read = 0;

        while (Reader.Sequence.TryGet(ref sequencePosition, out var memory))
        {
            var span = memory.Span;

            if (span.Length >= length - read)
            {
                span[..(length - read)].CopyTo(destination);
                break;
            }

            read += span.Length;

            span.CopyTo(destination);
            if (sequencePosition.GetObject() != null)
                destination = destination[span.Length..];
            else
                break;
        }

        Reader.Advance(length);

        Consumed += length;

        return bytes;
    }

    // I don't have time to optimize these. I am sure there are better ways to do it.

    public int[] ReadIntArray(int length = 0)
    {
        if (length == 0) length = ReadInt();

        var array = new int[length];

        for (var i = 0; i < length; i++) array[i] = ReadInt();

        return array;
    }

    public uint[] ReadUIntArray(int length = 0)
    {
        if (length == 0) length = ReadInt();

        var array = new uint[length];

        for (var i = 0; i < length; i++) array[i] = ReadUInt();

        return array;
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString()
    {
        return ReadString(Encoding.UTF8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString(Encoding encoding)
    {
        return encoding.GetString(ReadBytes());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string[] ReadStringArray(int length = 0)
    {
        return ReadStringArray(Encoding.UTF8, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string[] ReadStringArray(Encoding encoding, int length = 0)
    {
        if (length == 0) length = ReadInt();

        var array = new string[length];

        for (var i = 0; i < length; i++) array[i] = ReadString(encoding);

        return array;
    }
}
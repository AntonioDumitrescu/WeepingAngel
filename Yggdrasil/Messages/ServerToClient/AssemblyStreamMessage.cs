using System.Reflection;
using System.Runtime.Loader;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace Yggdrasil.Messages.ServerToClient;

public sealed class AssemblyStreamMessage : IMessage
{
    public int Index { get; private set; }

    public int Count { get; private set; }

    public byte[]? Binary { get; private set; }

    public string? Name { get; private set; }

    public AssemblyStreamMessage(int index, int count, byte[]? binary, string? assemblyName)
    {
        Index = index;
        Count = count;
        Binary = binary;
        Name = assemblyName;
    }

    public AssemblyStreamMessage() { }

    public void Serialize(IPacketWriter writer)
    {
        writer.WriteInt(Index);
        writer.WriteInt(Count);
        writer.WriteNullableBytes(Binary);
        writer.WriteNullableString(Name);
    }

    public void Deserialize(IPacketReader reader)
    {
        Index = reader.ReadInt();
        Count = reader.ReadInt();
        Binary = reader.ReadNullableBytes();
        Name = reader.ReadNullableString();
    }

    private Assembly? _assembly;

    public Assembly GetOrLoad(AssemblyLoadContext context)
    {
        if (_assembly != null) return _assembly!;
        if (Binary == null) throw new Exception("Cannot use this assembly, it is null!");

        using var stream = new MemoryStream(Binary!);
        _assembly = context.LoadFromStream(stream);
        return _assembly;
    }
}
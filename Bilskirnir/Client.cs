using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Encryption;
using Transport;
using Yggdrasil.Api;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace Bilskirnir;

/// <summary>
///     Used to communicate over the network using messages.
/// </summary>
public sealed class Client : IDisposable, IBilskirnir
{
    private static int _stringIdIndex;
    private readonly int _toStringId = Interlocked.Increment(ref _stringIdIndex);

    private class HandlerRegister : IHandlerRegister
    {
        public readonly ConcurrentDictionary<Type, Func<object, ValueTask>> Handlers = new();

        public void Register<T>(Func<T, ValueTask> handler) where T : IMessage
        {
            if (!Handlers.TryAdd(typeof(T), async o => await handler((T)o)))
            {
                throw new Exception("Duplicate message type.");
            }
        }
    }

    private readonly Func<Profiler>? _receiveProfiler;
    private readonly AesGcmWrapper _aes;

    public IPEndPoint RemoteEndPoint => (IPEndPoint)NetworkTransport.Socket.Client.RemoteEndPoint!;
    public IPAddress RemoteAddress => RemoteEndPoint.Address;
    public ushort RemotePort => (ushort)RemoteEndPoint.Port;

    public readonly ConcurrentDictionary<string, (Type, Func<object, ValueTask>)> Handlers = new();
    public readonly ConcurrentDictionary<IMessageReceiver, ConcurrentDictionary<Type, Func<object, ValueTask>>> Receivers = new();

    public Client(IPEndPoint endPoint, byte[] aesKey, Func<Profiler>? receiveProfiler = null)
    {
        _receiveProfiler = receiveProfiler;
        _aes = new AesGcmWrapper(aesKey);
        var tcp = new TcpClient();
        tcp.Connect(endPoint);
        NetworkTransport = new NetworkTransport(tcp, HandleData, receiveProfiler);
        NetworkTransport.Disconnected += TransportOnDisconnected;
    }

    private void TransportOnDisconnected(Transport.NetworkTransport _, DisconnectedReason reason, Exception? ex)
    {
        Dispose();
        Disconnected?.Invoke(this, reason, ex);
    }

    public Client(TcpClient client, byte[] aesKey, Func<Profiler>? receiveProfiler = null)
    {
        _receiveProfiler = receiveProfiler;
        _aes = new AesGcmWrapper(aesKey);
        NetworkTransport = new Transport.NetworkTransport(client, HandleData, receiveProfiler);
        NetworkTransport.Disconnected += TransportOnDisconnected;
    }

    /// <summary>
    ///     Adds a message receiver to the client, which can register handler methods for packet classes;
    /// </summary>
    /// <param name="receiver">The receiver instance.</param>
    /// <exception cref="Exception">The receiver registered duplicate handlers.</exception>
    public void AddReceiver(IMessageReceiver receiver)
    {
        var store = new HandlerRegister();
        receiver.RegisterHandlers(store);

        if (!Receivers.TryAdd(receiver, store.Handlers))
        {
            throw new Exception("Duplicate receiver!");
        }

        foreach (var (type, handler) in store.Handlers)
        {
            if (!Handlers.TryAdd(type.AssemblyQualifiedName ?? throw new Exception($"Could not get name for {type}!"), (type, handler)))
            {
                throw new Exception($"Duplicate handler: {type}");
            }
        }
    }

    /// <summary>
    ///     Removes a receiver and it's handlers from the client.
    /// </summary>
    /// <param name="receiver"></param>
    /// <exception cref="Exception">The receiver was not found.</exception>
    public void RemoveReceiver(IMessageReceiver receiver)
    {
        if (!Receivers.TryRemove(receiver, out var handlers))
        {
            throw new Exception("Could not remove receiver!");
        }

        foreach (var (type, _) in handlers)
        {
            if (!Handlers.TryRemove(type.AssemblyQualifiedName ?? throw new Exception($"Could not get name for {type}!"), out _))
            {
                throw new Exception($"Could not remove handler for {type}!");
            }
        }
    }

    private async ValueTask HandleData(byte[] cipher)
    {
        var profiler = _receiveProfiler?.Invoke();

        byte[] data;
            
        profiler?.BeginSection("Decrypt");

        try
        {
            data = _aes.Decrypt(cipher);
        }
        catch (Exception e)
        {
            Console.WriteLine($"failed to decrypt: {e}");
            NetworkTransport.Disconnect();
            return;
        }

        profiler?.EndSection("Decrypt");

        profiler?.BeginSection("Allocate reader");
        var reader = new PacketReader(data, data.Length);
        profiler?.EndSection("Allocate reader");

        profiler?.BeginSection("Read ID");
        var packetId = reader.ReadString();
        profiler?.EndSection("Read ID");

        profiler?.BeginSection("Fetch handler");
        if (!Handlers.TryGetValue(packetId, out var entry))
        {
            Console.WriteLine($"Could not get handler for {packetId}");
            return;
        }
        profiler?.EndSection("Fetch handler");

        var packetType = entry.Item1;
        var packetHandler = entry.Item2;

        profiler?.BeginSection("Activate packet instance");
        if (Activator.CreateInstance(packetType) is not IMessage instance)
        {
            Console.WriteLine("Instance is null.");
            return;
        }
        profiler?.EndSection("Activate packet instance");

        profiler?.BeginSection("Deserialize");
        instance.Deserialize(reader);
        profiler?.EndSection("Deserialize");

        profiler?.BeginSection("Handle");
        try
        {
            await packetHandler(instance);
        }
        catch (Exception e)
        {
            Console.WriteLine("handler ex: " + e);
        }
        profiler?.EndSection("Handle");
    }

    /// <summary>
    ///     Sends a message to the remote end.
    /// </summary>
    /// <typeparam name="T">The message class to send.</typeparam>
    /// <param name="message">The instance of the message class. It's <see cref="IMessage.Serialize"/> method will be called to serialize the data.</param>
    /// <returns></returns>
    /// <exception cref="Exception">An internal error occured.</exception>
    public async ValueTask Send<T>(T message, Profiler? profiler = null) where T : IMessage
    {
        profiler?.BeginSection("Total");

        await NetworkTransport.SendAsync(writer =>
        {
            var id = typeof(T).AssemblyQualifiedName ?? throw new Exception($"Could not get name for {typeof(T)}!");

            profiler?.BeginSection("Allocate sub-writer");
            using var subWriter = new PacketWriter();
            profiler?.EndSection("Allocate sub-writer");

            profiler?.BeginSection("Serialize message");
            subWriter.WriteString(id);
            message.Serialize(subWriter);
            profiler?.EndSection("Serialize message");

            profiler?.BeginSection("Compile sub-writer");
            var binary = subWriter.Compile();
            profiler?.EndSection("Compile sub-writer");

            profiler?.BeginSection("Encrypt");
            var encrypted = _aes.Encrypt(binary);
            profiler?.EndSection("Encrypt");

            profiler?.BeginSection("Write cipher");
            writer.WriteBytes(encrypted, false);
            profiler?.EndSection("Write cipher");
        }, profiler);

        profiler?.EndSection("Total");
    }

    public NetworkTransport NetworkTransport { get; }

    private bool _disposed;

    public void Dispose()
    {
        if(_disposed) return;

        _disposed = true;

        foreach (var receiver in Receivers)
        {
            receiver.Key.OnClosed();
        }

        NetworkTransport.Dispose();
        _aes.Dispose();
    }

    public override string ToString()
    {
        return $"{RemoteAddress} {_stringIdIndex}";
    }

    public event Action<Client, DisconnectedReason, Exception?>? Disconnected;
}
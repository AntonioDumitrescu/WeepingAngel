using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Yggdrasil.Api;

namespace Transport;

public class NetworkTransport : IDisposable
{
    private readonly Func<byte[], ValueTask> _handler;
    private readonly Func<Profiler>? _receiveProfiler;
    private readonly CancellationTokenSource _cts = new();

    public NetworkTransport(TcpClient socket, Func<byte[], ValueTask> handler, Func<Profiler>? receiveProfiler = null)
    {
        _handler = handler;
        _receiveProfiler = receiveProfiler;
        Socket = socket;
        Setup();
        Connected = true;
    }

    public TcpClient Socket { get; }

    public bool Connected { get; private set; }

    public void Dispose()
    {
        Disconnect();
    }

    public void Connect(IPEndPoint remoteEndPoint)
    {
        Socket.Connect(remoteEndPoint);
        Setup();
        Connected = true;
    }

    private void Setup()
    {
        var pipeline = new Pipe();
        Task.Factory.StartNew(() => ReadAsync(pipeline.Writer), TaskCreationOptions.LongRunning);
        Task.Factory.StartNew(() => ProcessAsync(pipeline.Reader), TaskCreationOptions.LongRunning);
    }

    private async Task ReadAsync(PipeWriter writer)
    {
        var stream = Socket.GetStream();
        var profiler = _receiveProfiler?.Invoke();

        try
        {
            while (!_cts.IsCancellationRequested)
            {
                profiler?.BeginSection("Get pipeline memory");
                var memory = writer.GetMemory(2048);
                profiler?.EndSection("Get pipeline memory");

                try
                {
                    profiler?.BeginSection("Read transport stream");

                    int read;
                    try
                    {
                        read = await stream.ReadAsync(memory, _cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (SocketException ex)
                    {
                        DisconnectInternal(DisconnectedReason.ReadError, ex);
                        return;
                    }

                    profiler?.EndSection("Read transport stream");

                    if (read == 0)
                    {
                        DisconnectInternal(DisconnectedReason.ConnectionClosed);
                        return;
                    }

                    profiler?.BeginSection("Advance writer");
                    writer.Advance(read);
                    profiler?.EndSection("Advance writer");

                    profiler?.BeginSection("Flush writer");

                    try
                    {
                        if ((await writer.FlushAsync(_cts.Token)).IsCanceled)
                        {
                            break;
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }

                    profiler?.EndSection("Flush writer");

                }
                catch (Exception ex)
                {
                    DisconnectInternal(DisconnectedReason.ReadError, ex);
                    break;
                }
            }
        }
        finally
        {
            await writer.CompleteAsync();
        }
    }

    private async Task ProcessAsync(PipeReader reader)
    {
        var profiler = _receiveProfiler?.Invoke();

        while (!_cts.IsCancellationRequested)
        {
            try
            {
                profiler?.BeginSection("Read from pipeline");
                var result = await reader.ReadAtLeastAsync(4, _cts.Token);
                profiler?.EndSection("Read from pipeline");

                if (result.Buffer.Length == 0)
                {
                    break;
                }

                profiler?.BeginSection("Parse total");
                var buffer = await ParsePackets(result.Buffer);
                profiler?.EndSection("Parse total");

                profiler?.BeginSection("Advance reader");
                reader.AdvanceTo(buffer.Start, buffer.End);
                profiler?.EndSection("Advance reader");
            }
            catch (Exception e)
            {
                if (!_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                }
            }
        }

        await reader.CompleteAsync();
    }

    private async ValueTask<ReadOnlySequence<byte>> ParsePackets(ReadOnlySequence<byte> sequence)
    {
        var reader = new SequenceReaderWrapper(sequence);

        while (reader.Available > 4 && !_cts.IsCancellationRequested)
        {
            var length = reader.ReadInt();

            if (length > sequence.Length)
            {
                // we need more data
                break;
            }

            var original = reader.Consumed;
            var remaining = length - (reader.Consumed - original) - 4;

            var data = reader.ReadBytes(remaining);

            await _handler(data);

            sequence = sequence.Slice(length);
        }

        return sequence;
    }

    public async Task SendAsync(Action<PacketWriter> serializer, Profiler? profiler = null)
    {
        profiler?.BeginSection("Allocate writer");
        using var writer = new PacketWriter();
        profiler?.EndSection("Allocate writer");

        try
        {
            profiler?.BeginSection("Fetch stream");
            var stream = Socket.GetStream();
            profiler?.EndSection("Fetch stream");

            profiler?.BeginSection("Reserve header size");
            writer.WriteInt(0); // reserve capacity
            profiler?.EndSection("Reserve header size");

            profiler?.BeginSection("Serialization total");
            serializer(writer);
            profiler?.EndSection("Serialization total");

            profiler?.BeginSection("Seek starting position");
            writer.Position = 0;
            profiler?.EndSection("Seek starting position");

            profiler?.BeginSection("Write header");
            writer.WriteInt(writer.Length);
            profiler?.EndSection("Write header");

            profiler?.BeginSection("Compile final packet");
            var final = writer.Compile();
            profiler?.EndSection("Compile final packet");

            profiler?.BeginSection("Write data to transport stream");
            await stream.WriteAsync(final);
            profiler?.EndSection("Write data to transport stream");
        }
        catch (Exception ex)
        {
            DisconnectInternal(DisconnectedReason.WriteError, ex);
            throw;
        }
    }

    public void Disconnect()
    {
        if (!Connected)
        {
            throw new InvalidOperationException("Already closed!");
        }

        DisconnectInternal(DisconnectedReason.UserRequested);
    }

    private void DisconnectInternal(DisconnectedReason reason, Exception? ex = null)
    {
        Console.WriteLine($"disconnect: {reason} {ex}");
        Disconnected?.Invoke(this, reason, ex);

        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }

        Socket?.Close();
        Connected = false;
    }

    public event Action<NetworkTransport, DisconnectedReason, Exception?>? Disconnected;
}

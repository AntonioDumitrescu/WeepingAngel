using System.Net;
using System.Net.Sockets;

namespace Bilskirnir;

public sealed class Server : IDisposable
{
    private readonly byte[] _aesKey;
    private readonly CancellationTokenSource _cts = new();
    private readonly TcpListener _listener;

    public Server(IPEndPoint localEp, byte[] aesKey)
    {
        _aesKey = aesKey;
        LocalEp = localEp;
        _listener = new TcpListener(localEp);
    }

    public IPEndPoint LocalEp { get; }

    public Task Listen()
    {
        _listener.Start();
        return AcceptAsync();
    }

    private async Task AcceptAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            TcpClient tcpClient;
            try
            {
                tcpClient = await _listener.AcceptTcpClientAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            var client = new Client(tcpClient, _aesKey);
            Console.WriteLine("Connected");
            NewClient?.Invoke(client);
        }
    }

    public event Action<Client>? NewClient; 

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
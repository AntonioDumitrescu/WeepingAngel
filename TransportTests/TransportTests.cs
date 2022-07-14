using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NUnit.Framework;

namespace TransportTests
{
    public class Tests
    {
        [Test]
        public async Task TestRoundTrip()
        {
            var request = RandomNumberGenerator.GetBytes(Random.Shared.Next(10, 100000));
            var response = RandomNumberGenerator.GetBytes(Random.Shared.Next(10, 100000));

            const int port = 5511;
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            var serverTask = Task.Run(async () =>
            {
                var client = await listener.AcceptTcpClientAsync();
                Transport.NetworkTransport serverNetworkTransport = null;
                var tcs = new TaskCompletionSource();

                serverNetworkTransport = new Transport.NetworkTransport(client, async bytes =>
                {
                    Assert.IsTrue(bytes.SequenceEqual(request));
                    await serverNetworkTransport!.SendAsync(w => w.WriteBytes(response, false));
                    tcs.SetResult();
                });

                await tcs.Task;
            });

            var c = new TcpClient();
            await c.ConnectAsync(IPAddress.Loopback, port);
            Transport.NetworkTransport clientNetworkTransport = null;
            var clientTcs = new TaskCompletionSource();

            clientNetworkTransport = new Transport.NetworkTransport(c, async buffer =>
            {
                Assert.IsTrue(buffer.SequenceEqual(response));
                clientTcs.SetResult();
            });

            await clientNetworkTransport.SendAsync(w => w.WriteBytes(request, false));
            await Task.WhenAll(serverTask, clientTcs.Task);
        }
    }
}
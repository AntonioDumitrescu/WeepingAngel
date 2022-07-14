using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Bilskirnir;
using NUnit.Framework;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace BilskirnirTests
{
    public class BilskirnirTest
    {
        [Test]
        public async Task TestReceiver()
        {
            const int port = 6542;
            var key = RandomNumberGenerator.GetBytes(Random.Shared.Next(5, 50));
            var server = new Server(new IPEndPoint(IPAddress.Any, port), key);
            var listenTask = server.Listen();
            var data = RandomNumberGenerator.GetBytes(Random.Shared.Next(10, 1024));

            server.NewClient += async client =>
            {
                for (var i = 0; i < 10; i++)
                {
                    await client.Send(new Message(data));
                }
            };


            var tcs = new TaskCompletionSource();
            var receiver = new Receiver(tcs, data);

            var c = new Client(new IPEndPoint(IPAddress.Loopback, port), key);

            c.AddReceiver(receiver);

            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

            server.Dispose();
            await listenTask;
        }

        class Message : IMessage
        {
            public byte[] Data { get; private set; }

            public Message()
            {

            }

            public Message(byte[] data)
            {
                Data = data;
            }

            public void Serialize(IPacketWriter writer)
            {
                writer.WriteBytes(Data);   
            }

            public void Deserialize(IPacketReader reader)
            {
                Data = reader.ReadBytes();
            }
        }

        class Receiver : IMessageReceiver
        {
            private readonly TaskCompletionSource _tcs;
            private readonly byte[] _data;
            private int _count;

            public Receiver(TaskCompletionSource tcs, byte[] data)
            {
                _tcs = tcs;
                _data = data;
            }

            public void RegisterHandlers(IHandlerRegister register)
            {
                register.Register<Message>(Handle);
            }

            public void OnClosed() { }

            private async ValueTask Handle(Message message)
            {
                Assert.IsTrue(message.Data.SequenceEqual(_data));
                if (_count++ == 9)
                {
                    _tcs.SetResult();
                }
            }
        }
    }
}
using System.Net;
using Yggdrasil.Api.Networking;

namespace Yggdrasil.Api.Client;

/// <summary>
///     Abstracted Bilskirnir client.
/// </summary>
public interface IBilskirnir
{
    IPEndPoint RemoteEndPoint { get; }

    IPAddress RemoteAddress { get; }
    
    ushort RemotePort { get; }

    /// <summary>
    ///     Adds a message receiver to the client, which can register handler methods for packet classes;
    /// </summary>
    /// <param name="receiver">The receiver instance.</param>
    /// <exception cref="Exception">The receiver registered duplicate handlers.</exception>
    void AddReceiver(IMessageReceiver receiver);

    /// <summary>
    ///     Removes a receiver and it's handlers from the client.
    /// </summary>
    /// <param name="receiver"></param>
    /// <exception cref="Exception">The receiver was not found.</exception>
    void RemoveReceiver(IMessageReceiver receiver);

    /// <summary>
    ///     Sends a message to the remote end.
    /// </summary>
    /// <typeparam name="T">The message class to send.</typeparam>
    /// <param name="message">The instance of the message class. It's <see cref="IMessage.Serialize"/> method will be called to serialize the data.</param>
    /// <param name="profiler">Measure overhead of this method.</param>
    /// <returns></returns>
    /// <exception cref="Exception">An internal error occured.</exception>
    ValueTask Send<T>(T message, Profiler? profiler = null) where T : IMessage;

    void Dispose();

    string ToString();
}
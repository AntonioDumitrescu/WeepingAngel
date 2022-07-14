using Yggdrasil.Api.Networking;

namespace Yggdrasil.Api.Client;

public interface IHandlerRegister
{
    /// <summary>
    ///     Used to register a function that handles messages.
    ///     The function must return a ValueTask; it will be awaited down the chain.
    /// </summary>
    /// <typeparam name="T">The message class. It must be unique across assemblies. It's <see cref="IMessage.Deserialize"/> method
    /// will be called to load the binary data into the instance.</typeparam>
    /// <param name="handler">A function for handling the message. It will be awaited down the processing chain.</param>
    void Register<T>(Func<T, ValueTask> handler) where T : IMessage;
}
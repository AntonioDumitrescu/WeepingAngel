using Yggdrasil.Api.Client;

namespace Yggdrasil.Api.Networking;

public interface IMessageReceiver
{
    /// <summary>
    ///     Called by the client class after the handler is added.
    ///     It is used to register handler methods for packet classes.
    /// </summary>
    /// <param name="register"></param>
    void RegisterHandlers(IHandlerRegister register);

    /// <summary>
    ///     Called when the client closed.
    /// </summary>
    void OnClosed();
}
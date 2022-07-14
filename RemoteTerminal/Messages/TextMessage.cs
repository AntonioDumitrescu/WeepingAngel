using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace RemoteTerminal.Messages;

/// <summary>
///     Carries CMD output (client->server) and commands (server->client)
/// </summary>
internal struct TextMessage : IMessage
{
    public string Message { get; private set; }

    public TextMessage(string message)
    {
        Message = message;
    }

    public TextMessage()
    {
        Message = string.Empty;
    }

    public void Serialize(IPacketWriter writer)
    {
        writer.WriteString(Message);
    }

    public void Deserialize(IPacketReader reader)
    {
        Message = reader.ReadString();
    }
}
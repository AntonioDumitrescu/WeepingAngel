using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace RemoteTerminal.Messages;

/// <summary>
///     Sent in order to open/close the CMD stream.
/// </summary>
internal sealed class SetTerminalStateMessage : IMessage
{
    public enum State
    {
        Open,
        Closed
    }

    public State Target { get; private set; }

    public SetTerminalStateMessage(State state)
    {
        Target = state;
    }

    public SetTerminalStateMessage() { }

    public void Serialize(IPacketWriter writer)
    {
        writer.WriteEnum(Target);        
    }

    public void Deserialize(IPacketReader reader)
    {
        Target = reader.ReadEnum<State>();
    }
}
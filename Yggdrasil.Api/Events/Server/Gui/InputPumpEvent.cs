using Veldrid;

namespace Yggdrasil.Api.Events.Server.Gui;

public readonly struct InputPumpEvent : IGuiEvent
{
    public InputSnapshot Snapshot { get; }

    public InputPumpEvent(InputSnapshot snapshot)
    {
        Snapshot = snapshot;
    }
}
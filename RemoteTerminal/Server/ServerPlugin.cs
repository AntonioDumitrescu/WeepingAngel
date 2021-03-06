using ImGuiNET;
using Yggdrasil.Api.Events.Server.Gui.Render;
using Yggdrasil.Api.Events.System;
using Yggdrasil.Api.Setup;

namespace RemoteTerminal.Server;

[ServerPlugin(
    "Remote Command Prompt",
    "builtin.remoteTerminal",
    "0",
    "Antonio Dumitrescu")]
public sealed class ServerPlugin : ServerPluginBase, IEventReceiver
{
    private readonly TerminalWindowManager _windowManager;

    public ServerPlugin(IEventManager eventManager, TerminalWindowManager windowManager)
    {
        _windowManager = windowManager;
        eventManager.AddReceiver(this);
    }

    [SubscribeEvent]
    private void OnClientListWindowRender(ClientListWindowRenderEvent @event)
    {
        if (@event.SelectedClient == null) return;

        if (ImGui.Button("Open Remote Terminal"))
        {
            _windowManager.HandleClient(@event.SelectedClient);
        }
    }
}
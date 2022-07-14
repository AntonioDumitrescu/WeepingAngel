using ImGuiNET;
using Yggdrasil.Api.Events.Server.Gui.Render;
using Yggdrasil.Api.Events.System;
using Yggdrasil.Api.Setup;

namespace RemoteTerminal.Server;

[ServerPlugin(
    "Remote Terminal",
    "greeper.remoteTerminal",
    "0",
    "Alioth Merak")]
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

        ImGui.Text("Remote Terminal");
        ImGui.TreePush();
        {
            if (ImGui.Button("Open Remote Terminal"))
            {
                _windowManager.HandleClient(@event.SelectedClient);
            }
        }
        ImGui.TreePop();
    }
}
using ImGuiNET;
using Yggdrasil.Api.Events.Server.Gui.Render;
using Yggdrasil.Api.Events.System;
using Yggdrasil.Api.Setup;

namespace RemoteDesktop.Server;

[ServerPlugin(
    "Remote Desktop",
    "builtin.remoteDesktop",
    "0",
    "Antonio Dumitrescu")]
public sealed class ServerPlugin : ServerPluginBase, IEventReceiver
{
    private readonly RemoteDesktopWindowManager _windowManager;

    public ServerPlugin(IEventManager eventManager, RemoteDesktopWindowManager windowManager)
    {
        _windowManager = windowManager;
        eventManager.AddReceiver(this);
    }

    [SubscribeEvent]
    private void OnClientListWindowRender(ClientListWindowRenderEvent @event)
    {
        if (@event.SelectedClient == null) return;

        if (ImGui.Button("Open Remote Desktop"))
        {
            _windowManager.HandleClient(@event.SelectedClient);
        }
    }
}
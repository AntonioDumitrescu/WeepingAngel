using Yggdrasil.Api.Server;

namespace Yggdrasil.Api.Events.Server.Gui.Render;

public readonly struct ClientListWindowRenderEvent : IRenderEvent
{
    public IRemoteClient[] Clients { get; }

    public IRemoteClient? SelectedClient { get; }

    public ClientListWindowRenderEvent(IRemoteClient[] clients, IRemoteClient? selectedClient)
    {
        Clients = clients;
        SelectedClient = selectedClient;
    }
}
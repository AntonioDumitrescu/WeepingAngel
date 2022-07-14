using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using Yggdrasil.Api.Events.Server.Gui.Render;
using Yggdrasil.Api.Events.System;
using Yggdrasil.Api.Server;

namespace RemoteDesktop.Server;

public sealed class RemoteDesktopWindowManager : IEventReceiver
{
    private readonly ILogger<RemoteDesktopWindowManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<IRemoteClient, RemoteDesktopWindow> _windows = new();

    public RemoteDesktopWindowManager(
        ILogger<RemoteDesktopWindowManager> logger, 
        IServiceProvider serviceProvider,
        IEventManager eventManager)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        eventManager.AddReceiver(this);
    }

    public void HandleClient(IRemoteClient client)
    {
        if (_windows.ContainsKey(client))
        {
            _logger.LogError("Remote desktop already opened!");
            return;
        }

        var instance = ActivatorUtilities.CreateInstance<RemoteDesktopWindow>(_serviceProvider, client);

        _logger.LogInformation("Registering window.");

        Trace.Assert(_windows.TryAdd(client, instance));

        instance.OnRemove += () => Trace.Assert(_windows.TryRemove(client, out _));
    }

    [SubscribeEvent]
    public void RenderWindows(ViewportRenderEvent @event)
    {
        foreach (var window in _windows.Values)
        {
            window.Render();
        }
    }
}
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Yggdrasil.Api.Events.Server.Gui.Render;
using Yggdrasil.Api.Events.System;
using Yggdrasil.Api.Server;

namespace RemoteTerminal.Server;

public sealed class TerminalWindowManager : IEventReceiver
{
    private readonly ILogger<TerminalWindowManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<IRemoteClient, TerminalWindow> _windows = new();

    public TerminalWindowManager(ILogger<TerminalWindowManager> logger, IServiceProvider serviceProvider, IEventManager eventManager)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        eventManager.AddReceiver(this);
    }

    public void HandleClient(IRemoteClient client)
    {
        if (_windows.ContainsKey(client))
        {
            _logger.LogError("Remote terminal already opened!");
            return;
        }

        var instance = ActivatorUtilities.CreateInstance<TerminalWindow>(_serviceProvider, client);

        _logger.LogInformation("Registering window.");

        Trace.Assert(_windows.TryAdd(client, instance));

        instance.OnRemove += () => Trace.Assert(_windows.TryRemove(client, out _));
    }

    [SubscribeEvent]
    public void RenderWindow(ViewportRenderEvent @event)
    {
        foreach (var terminalWindow in _windows.Values)
        {
            terminalWindow.Render();
        }
    }
}
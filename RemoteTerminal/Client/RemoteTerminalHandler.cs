using Microsoft.Extensions.Logging;
using RemoteTerminal.Messages;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace RemoteTerminal.Client;

internal sealed class RemoteTerminalHandler : IMessageReceiver
{
    private readonly ILogger<RemoteTerminalHandler> _logger;
    private readonly IBilskirnir _client;

    private RemoteTerminal? _terminal;

    public RemoteTerminalHandler(ILogger<RemoteTerminalHandler> logger, IBilskirnir client)
    {
        _logger = logger;
        _client = client;
    }

    public void Start()
    {
        _logger.LogInformation("Adding remote terminal handler.");
        _client.AddReceiver(this);
    }

    public void RegisterHandlers(IHandlerRegister register)
    {
        register.Register<SetTerminalStateMessage>(ReceiveSetTerminalState);
        _logger.LogInformation("Registered terminal handler message.");
    }
    
    private ValueTask ReceiveSetTerminalState(SetTerminalStateMessage message)
    {
        var currentState = _terminal == null 
            ? SetTerminalStateMessage.State.Closed 
            : SetTerminalStateMessage.State.Open;

        if (message.Target == currentState)
        {
            _logger.LogCritical("Invalid state request. Target: {t}, open: {c}", message.Target, _terminal != null);
            return ValueTask.CompletedTask;
        }
        
        if (message.Target == SetTerminalStateMessage.State.Open)
        {
            _terminal = new RemoteTerminal(_client);
            _client.AddReceiver(_terminal);
        }
        else
        {
            _terminal!.Close();
            _client.RemoveReceiver(_terminal);
            _terminal = null;
        }

        return ValueTask.CompletedTask;
    }

    public void OnClosed()
    {
        _logger.LogInformation("Closing remote terminal handler.");
    }
}
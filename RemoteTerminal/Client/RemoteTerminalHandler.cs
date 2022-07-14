using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using RemoteTerminal.Messages;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace RemoteTerminal.Client;

internal sealed class RemoteTerminalHandler : IMessageReceiver
{
    private readonly ILogger<RemoteTerminalHandler> _logger;
    private readonly IBilskirnir _client;

    private Terminal? _terminal;

    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions()
    {
        SingleReader = true,
        SingleWriter = true
    });

    public RemoteTerminalHandler(ILogger<RemoteTerminalHandler> logger, IBilskirnir client)
    {
        _logger = logger;
        _client = client;
    }

    public void Start()
    {
        _logger.LogInformation("Adding remote terminal receiver.");
        _client.AddReceiver(this);
    }

    public void RegisterHandlers(IHandlerRegister register)
    {
        register.Register<SetTerminalStateMessage>(ReceiveSetTerminalState);
        register.Register<TextMessage>(ReceiveTextMessage);
        _logger.LogInformation("Registered terminal message handlers.");
    }
    
    private ValueTask ReceiveSetTerminalState(SetTerminalStateMessage message)
    {
        if (message.Target == (_terminal == null ? SetTerminalStateMessage.State.Closed : SetTerminalStateMessage.State.Open))
        {
            _logger.LogCritical("Invalid state request. Target: {t}, open: {c}", message.Target, _terminal != null);
            return ValueTask.CompletedTask;
        }
        var cts = new CancellationTokenSource();


        if (message.Target == SetTerminalStateMessage.State.Open)
        {
            _logger.LogInformation("Creating terminal!");

            _terminal = new Terminal();

            Task.Factory.StartNew(async () =>
            {
                var sb = new StringBuilder();

                while (!cts.IsCancellationRequested)
                {
                    await foreach (var item in _channel.Reader.ReadAllAsync(cts.Token))
                    {
                        await _client.Send(new TextMessage(item + "\r\n"));
                    }
                }
            }, TaskCreationOptions.LongRunning);

            _terminal.NewLine += async s =>
            {
                await _channel.Writer.WriteAsync(s, cts.Token);
            };
        }
        else
        {
            _logger.LogInformation("Closing terminal.");
            _terminal!.Dispose();
            cts.Cancel();
        }

        return ValueTask.CompletedTask;
    }

    private async ValueTask ReceiveTextMessage(TextMessage message)
    {
        _logger.LogInformation("Received command: {txt}", message.Message);

        if (_terminal == null)
        {
            _logger.LogCritical("Received command but terminal is closed!");
            return;
        }

        await _terminal.Send(message.Message);
    }

    public void OnClosed()
    {
        _terminal?.Dispose();
    }
}
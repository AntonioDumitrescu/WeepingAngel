using RemoteTerminal.Messages;
using System.Text;
using System.Threading.Channels;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace RemoteTerminal.Client;

internal class RemoteTerminal : IMessageReceiver
{
    private const int BufferMs = 20;

    private readonly IBilskirnir _client;
    private readonly CommandPromptWrapper _commands = new();
    private readonly Channel<string> _cmdOutputQueue = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

    private readonly CancellationTokenSource _cts = new();

    public RemoteTerminal(IBilskirnir client)
    {
        _client = client;
        Task.Factory.StartNew(DequeueAndSendAsync, TaskCreationOptions.LongRunning);

        _commands.NewLine += async s =>
        {
            await _cmdOutputQueue.Writer.WriteAsync(s, _cts.Token);
        };
    }
    
    public void RegisterHandlers(IHandlerRegister register)
    {
        register.Register<TextMessage>(ReceiveCommandAndExecuteAsync);
    }

    /// <summary>
    ///     Aggregates commands and sends the results every <see cref="BufferMs"/> milliseconds, if any are available.
    ///     This is done to reduce the message rate.
    /// </summary>
    /// <returns></returns>
    private async Task DequeueAndSendAsync()
    {
        var sb = new StringBuilder();

        while (!_cts.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(BufferMs, _cts.Token);
                await _cmdOutputQueue.Reader.WaitToReadAsync(_cts.Token);

                while (_cmdOutputQueue.Reader.TryRead(out var line))
                {
                    sb.AppendLine(line);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            var content = sb.ToString();

            if (!string.IsNullOrEmpty(content))
            {
                await _client.Send(new TextMessage(content));
            }

            sb.Clear();
        }
    }

    private async ValueTask ReceiveCommandAndExecuteAsync(TextMessage message)
    {
        await _commands.Send(message.Message);
    }

    /// <summary>
    ///     Called when the connection is interrupted.
    /// </summary>
    public void OnClosed()
    {
        Close();
    }

    /// <summary>
    ///     Called when the user closes the window.
    /// </summary>
    public void Close()
    {
        _cts.Cancel();
        _cts.Dispose();
        _commands.Dispose();
    }
}
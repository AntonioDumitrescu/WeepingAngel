using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using RemoteTerminal.Messages;
using Veldrid;
using Yggdrasil.Api;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Events.System;
using Yggdrasil.Api.Networking;
using Yggdrasil.Api.Server;

namespace RemoteTerminal.Server;

internal sealed class TerminalWindow : IMessageReceiver
{
    private readonly ILogger<TerminalWindow> _logger;
    private readonly IRemoteClient _client;
    public event Action? OnRemove;

    private string _content = "[empty]";
    private readonly StringBuilder _sb = new();
    private const int MaxContent = 1024 * 64;

    private readonly object _renderSync = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _sendTask;

    public TerminalWindow(ILogger<TerminalWindow> logger, IRemoteClient client)
    {
        _logger = logger;
        _client = client;

        _client.Connection.AddReceiver(this);
       
        _ = Task.Run(async () =>
        {
            _logger.LogInformation("Setting up.");
            await client.Connection.Send(new SetTerminalStateMessage(SetTerminalStateMessage.State.Open));
            _logger.LogInformation("Sent setup textMessage.");
        });
    }

    public void RegisterHandlers(IHandlerRegister register)
    {
        _logger.LogInformation("Registering text handler.");
        register.Register<TextMessage>(ReceiveText);
    }

    private readonly byte[] _inputBuffer = new byte[1024];

    private int lastVersion = 0;

    public void Render()
    {
        ImGui.SetNextWindowSize(new Vector2(100, 100), ImGuiCond.FirstUseEver);

        if (ImGui.Begin($"Remote Terminal ({_client.Username} / {_client.Connection})"))
        {
            lock (_renderSync)
            {
                if (lastVersion != _content.GetHashCode())
                {
                    lastVersion = _content.GetHashCode();
                    Console.WriteLine($"new: {_content}");
                }

                ImGui.TextColored(new Vector4(0, 1, 0, 1), _content);
            }

            if (_sendTask is { IsCompleted: false })
            {
                ImGui.TextDisabled("sending...");
            }
            else if (ImGui.InputText("command", _inputBuffer, (uint)_inputBuffer.Length, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                var len = 0;

                for (var index = 0; index < _inputBuffer.Length; index++)
                {
                    if (_inputBuffer[index] == 0)
                    {
                        len = index;
                        break;
                    }
                }

                var txt = Encoding.UTF8.GetString(_inputBuffer.AsSpan(0, len));
                Array.Clear(_inputBuffer);

                if (!string.IsNullOrEmpty(txt))
                {
                    _sendTask = Task.Run(async () =>
                    {
                        await _client.Connection.Send(new TextMessage(txt));
                    });
                }
            }

            if (ImGui.Button("Close"))
            {
                if (_sendTask is not { IsCompleted: false })
                {
                    _logger.LogInformation("Closing remote shell!");
                    _client.Connection.Send(new SetTerminalStateMessage(SetTerminalStateMessage.State.Closed));
                    OnClosed();
                }
            }

            if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
            {
                ImGui.SetScrollHereY(1.0f);
            }
        }

        ImGui.End();
    }

    private ValueTask ReceiveText(TextMessage textMessage)
    {
        Console.WriteLine($"Received {textMessage.Message.Length}");

        _sb.Append(textMessage.Message);

        var overflow = _sb.Length - MaxContent;

        if (overflow > 0)
        {
            _sb.Remove(0, overflow);
        }

        lock (_renderSync)
        {
            _content = _sb.ToString();
            Console.WriteLine($"new content len: {_content.Length}");
        }

        return ValueTask.CompletedTask;
    }

    public void OnClosed()
    {
        _logger.LogInformation("Closing!");

        try
        {
            _client.Connection.RemoveReceiver(this);
        }
        catch
        {
            // ignored
        }
        _cts.Cancel();
        OnRemove?.Invoke();
    }
}
using Bilskirnir;
using Serilog;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;
using Yggdrasil.Messages.ServerToClient;
using Yggdrasil.Utilities;

namespace Damocles;

internal sealed class AssemblyStreamHandler : IMessageReceiver
{
    private enum Stage
    {
        Libraries, Plugins
    }

    private readonly TaskCompletionSource _tcs;

    private Stage _stage = Stage.Libraries;

    public AssemblyStreamHandler(TaskCompletionSource tcs, Client client)
    {
        _tcs = tcs;

        client.NetworkTransport.Disconnected += (_, _, ex) =>
        {
            if(tcs.Task.IsCompleted) return;
            tcs.SetException(ex ?? new Exception("Lost connection during transit!"));
        };
    }

    public List<AssemblyStreamMessage> LibraryBinaries { get; } = new();

    public List<AssemblyStreamMessage> PluginBinaries { get; } = new();

    public void RegisterHandlers(IHandlerRegister register)
    {
        register.Register<AssemblyStreamMessage>(Handle);
    }

    private ValueTask Handle(AssemblyStreamMessage message)
    {
        // no plugins are available
        if (message.Count == 0)
        {
            Log.Information("No {stage} are available.", _stage);
            if (_stage == Stage.Plugins)
            {
                _tcs.SetResult();
            }
            else
            {
                _stage = Stage.Plugins;
            }
            return ValueTask.CompletedTask;
        }

        Log.Information("Streaming {stage} {name} {current}/{count} [{size}]",
            _stage, 
            message.Name,
            message.Index + 1,
            message.Count,
            Suffix.Convert(message.Binary!.Length));

        (_stage == Stage.Libraries ? LibraryBinaries : PluginBinaries).Add(message);

        if (message.Index == message.Count - 1)
        {
            if (_stage == Stage.Plugins)
            {
                Log.Information("Received last plugin assembly. Ending stream.");
                _tcs.SetResult();

            }
            else
            {
                Log.Information("Received all library assemblies!");
                _stage = Stage.Plugins;
            }
        }

        return ValueTask.CompletedTask;
    }

    public void OnClosed() { }
}
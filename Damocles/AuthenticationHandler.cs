using Serilog;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;
using Yggdrasil.Messages.ServerToClient;

namespace Damocles;

class AuthenticationHandler : IMessageReceiver
{
    private readonly TaskCompletionSource<bool> _tcs;

    public AuthenticationHandler(TaskCompletionSource<bool> tcs)
    {
        _tcs = tcs;
    }

    public void RegisterHandlers(IHandlerRegister register)
    {
        Log.Information("Registering authentication handler.");
        register.Register<AuthenticationResult>(HandleResponse);
    }

    private async ValueTask HandleResponse(AuthenticationResult result)
    {
        _tcs.SetResult(result.Accepted);
    }

    public void OnClosed() { }
}
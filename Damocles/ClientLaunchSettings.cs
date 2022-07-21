namespace Damocles;

public sealed class ClientLaunchSettings
{
    public ClientLaunchSettings(int port, string @interface, string password)
    {
        Port = port;
        Interface = @interface;
        Password = password;
    }

    public int Port { get; }

    public string Interface { get; }

    public string Password { get; }
}
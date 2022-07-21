namespace Merlin;

public sealed class ServerLaunchSettings
{
    public string Interface { get; }
        
    public ushort Port { get; }

    public string Password { get; }

    public ServerLaunchSettings(string @interface, ushort port, string password)
    {
        Interface = @interface;
        Port = port;
        Password = password;
    }
}
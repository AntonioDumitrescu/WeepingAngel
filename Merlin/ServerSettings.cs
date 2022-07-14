namespace Merlin;

public sealed class ServerSettings
{
    public string Interface { get; }
        
    public ushort Port { get; }

    public string Password { get; }

    public ServerSettings(string @interface, ushort port, string password)
    {
        Interface = @interface;
        Port = port;
        Password = password;
    }
}
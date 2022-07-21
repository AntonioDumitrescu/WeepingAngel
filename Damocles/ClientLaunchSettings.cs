namespace Damocles;

public sealed class ClientLaunchSettings
{
    public ClientLaunchSettings(int port, string address, string password)
    {
        Port = port;
        Address = address;
        Password = password;
    }

    public int Port { get; }

    public string Address { get; }

    public string Password { get; }
}
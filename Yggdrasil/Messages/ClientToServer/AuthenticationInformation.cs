using Transport;
using Yggdrasil.Api.Client;
using Yggdrasil.Api.Networking;

namespace Yggdrasil.Messages.ClientToServer;

public struct AuthenticationInformation : IMessage  
{
    public string UserAccount { get; private set; }

    public int Version { get; private set; }

    public AuthenticationInformation(string userAccount, int version)
    {
        UserAccount = userAccount;
        Version = version;
    }

    public void Serialize(IPacketWriter writer)
    {
        writer.WriteString(UserAccount);
        writer.WriteInt(Version);
    }

    public void Deserialize(IPacketReader reader)
    {
        UserAccount = reader.ReadString();
        Version = reader.ReadInt();
    }
}
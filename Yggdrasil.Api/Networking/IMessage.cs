using Yggdrasil.Api.Client;

namespace Yggdrasil.Api.Networking;

/// <summary>
///     Encapsulates data that can be transferred over the network.
/// </summary>
public interface IMessage
{
    /// <summary>
    ///     Called if this message is passed to the Send method of the client.
    ///     You must write the information you wish to transfer here.
    /// </summary>
    /// <param name="writer">A serialization wrapper. Note that this has a size limit.</param>
    void Serialize(IPacketWriter writer);

    /// <summary>
    ///     Called if this message has been received over the network.
    ///     You must read the information you serialized in the <see cref="Serialize"/> method and load it into this instance's data, for further access.
    /// </summary>
    /// <param name="reader">A deserialization wrapper.</param>
    void Deserialize(IPacketReader reader);
}
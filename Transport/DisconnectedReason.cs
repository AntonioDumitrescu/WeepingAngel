namespace Transport;

public enum DisconnectedReason
{
    /// <summary>
    ///     The Disconnect method was called on the local client.
    /// </summary>
    UserRequested,

    /// <summary>
    ///     The remote client closed the connection.
    /// </summary>
    ConnectionClosed,

    /// <summary>
    ///     A reading error occurred.
    /// </summary>
    ReadError,

    /// <summary>
    ///     A writing error occurred.
    /// </summary>
    WriteError
}
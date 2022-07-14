using System.Drawing;

namespace RemoteDesktop.Client;

internal interface IEncoder : IDisposable
{
    bool Encode(Bitmap image, out byte[][] results);
}
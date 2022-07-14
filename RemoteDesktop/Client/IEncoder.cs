using System.Drawing;

namespace RemoteDesktop.Client;

internal interface IEncoder
{
    bool Encode(Bitmap image, out byte[][] results);
}
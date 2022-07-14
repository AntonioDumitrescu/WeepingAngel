using System.Drawing;

namespace RemoteDesktop.Client;

internal interface IFrameSource : IDisposable
{
    BitmapPool.BitmapProvider GetImage();
}
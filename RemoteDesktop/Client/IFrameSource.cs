using System.Drawing;

namespace RemoteDesktop.Client;

internal interface IFrameSource
{
    BitmapPool.BitmapProvider GetImage();
}
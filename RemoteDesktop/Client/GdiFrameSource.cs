namespace RemoteDesktop.Client;

internal sealed class GdiFrameSource : IFrameSource
{
    private readonly BitmapPool _pool;

    public GdiFrameSource(BitmapPool pool)
    {
        _pool = pool;
    }

    public BitmapPool.BitmapProvider GetImage()
    {
        var bounds = Screen.GetBounds(Point.Empty);

        var provider = _pool.GetProvider(bounds.Size);
        using var g = Graphics.FromImage(provider.Bitmap);
        g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);

        return provider;
    }

    public void Dispose()
    {
    }
}
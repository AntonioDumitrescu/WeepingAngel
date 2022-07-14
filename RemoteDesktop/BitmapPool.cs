using System.Collections.Concurrent;
using System.Drawing.Imaging;

namespace RemoteDesktop;

internal sealed class BitmapPool
{
    private readonly ConcurrentDictionary<Size, ConcurrentBag<Bitmap>> _pools = new();

    public void Return(Bitmap bitmap)
    {
        if (!_pools.TryGetValue(new Size(bitmap.Width, bitmap.Height), out var pool))
        {
            throw new KeyNotFoundException("Could not find pool.");
        }

        pool.Add(bitmap);
    }

    public Bitmap Get(Size size)
    {
        var pool = _pools.GetOrAdd(size, _ => new ConcurrentBag<Bitmap>());
        return pool.TryTake(out var bmp) ? bmp : new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
    }

    public BitmapProvider GetProvider(Size size)
    {
        return new BitmapProvider(Get(size), this);
    }

    public sealed class BitmapProvider : IDisposable
    {
        public Bitmap Bitmap { get; }

        private readonly BitmapPool _source;

        public BitmapProvider(Bitmap image, BitmapPool source)
        {
            Bitmap = image;
            _source = source;
        }

        private void Cleanup()
        {
            _source.Return(Bitmap);
        }

        public void Dispose()
        {
            Cleanup();
        }
    }


}
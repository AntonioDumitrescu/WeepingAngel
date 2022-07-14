using System.Collections.Concurrent;

namespace RemoteDesktop;

internal sealed class BitmapPool
{
    private readonly ConcurrentDictionary<Size, ConcurrentBag<Bitmap>> _pools = new();

    public void Return(Bitmap bitmap)
    {
        if (!_pools.TryGetValue(bitmap.Size, out var pool))
        {
            throw new KeyNotFoundException("Could not find pool.");
        }

        pool.Add(bitmap);
    }

    public Bitmap Get(Size size)
    {
        var pool = _pools.GetOrAdd(size, _ => new ConcurrentBag<Bitmap>());

        return pool.TryTake(out var bmp) ? bmp : new Bitmap(size.Width, size.Height);
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

        ~BitmapProvider()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            _source.Return(Bitmap);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Cleanup();
        }
    }


}
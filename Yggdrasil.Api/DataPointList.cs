namespace Yggdrasil.Api;

public sealed class DataPointList<T>
{
    public int MaxPoints { get; }

    private readonly List<T> _points = new();

    private T[] _array;

    public bool NeedsUpdate { get; private set; } = true;

    public DataPointList(int maxPoints)
    {
        MaxPoints = maxPoints;
    }

    public void AddPoints(IEnumerable<T> points)
    {
        _points.AddRange(points);

        if (_points.Count > MaxPoints)
        {
            _points.RemoveRange(0, _points.Count - MaxPoints);
        }

        NeedsUpdate = true;
    }

    public void AddPoint(T point)
    {
        _points.Add(point);

        while (_points.Count > MaxPoints)
        {
            _points.RemoveAt(0);
        }

        NeedsUpdate = true;
    }

    public int Count => _points.Count;

    public T[] AsArray()
    {
        if (NeedsUpdate)
        {
            _array = _points.ToArray();
            NeedsUpdate = false;
        }

        return _array;
    }
}
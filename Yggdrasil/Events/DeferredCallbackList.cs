using System.Collections.Concurrent;

namespace Yggdrasil.Events;

internal sealed class DeferredCallbackList 
{
    private sealed class UnregisterEvent : IDisposable
    {
        private readonly DeferredCallbackList _register;
        private readonly int _id;

        public UnregisterEvent(DeferredCallbackList register, int id)
        {
            _register = register;
            _id = id;
        }

        public void Dispose()
        {
            _register.Remove(_id);
        }
    }

    private readonly ConcurrentDictionary<int, IEventBus> _callbacks = new();
    private int _id;

    public IEnumerable<IEventBus> GetBusCollection()
    {
        return _callbacks.Select(x => x.Value);
    }

    public IDisposable Add(IEventBus callback)
    {
        var id = Interlocked.Increment(ref _id);

        if (!_callbacks.TryAdd(id, callback))
        {
            Environment.FailFast("Failed");
        }

        return new UnregisterEvent(this, id);
    }

    private void Remove(int id)
    {
        _callbacks.TryRemove(id, out _);
    }
}
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Yggdrasil.Api;

public sealed class Profiler
{
    public readonly struct DisposableCallback : IDisposable
    {
        private readonly Action _closing;

        public DisposableCallback(Action closing)
        {
            _closing = closing;
        }

        public void Dispose()
        {
            _closing();
        }
    }

    private readonly ConcurrentDictionary<string, Stopwatch> _counters = new();
    private readonly ConcurrentStack<string> _stack = new();

    public void BeginSection(string key)
    {
        if (!_counters.TryGetValue(key, out var sw))
        {
            sw = new Stopwatch();
            sw.Start();
            _counters.TryAdd(key, sw);
            return;
        }

        sw.Start();
    }

    public void EndSection(string key)
    {
        if (!_counters.TryGetValue(key, out var sw))
        {
            throw new Exception("Counter not found!");
        }

        sw.Stop();
    }

    public void PushSection(string key)
    {
        _stack.Push(key);
        BeginSection(key);
    }

    public Stopwatch PopSectionRemove()
    {
        if (!_stack.TryPop(out var section))
        {
            throw new Exception("Failed to get section!");
        }

        EndSection(section);
        var sw = GetSection(section);
        RemoveSection(section);
        return sw;
    }

    public void PopSection()
    {
        if (!_stack.TryPop(out var section))
        {
            throw new Exception("Failed to get section!");
        }

        EndSection(section);
    }

    public void ProfileSection(string key, Action body)
    {
        BeginSection(key);
        body();
        EndSection(key);
    }

    public DisposableCallback ProfileScope(string key)
    {
        BeginSection(key);
        return new DisposableCallback(() => EndSection(key));
    }

    public Stopwatch GetSection(string key)
    {
        if (!_counters.TryGetValue(key, out var sw))
        {
            throw new Exception("Counter not found!");
        }

        return sw;
    }

    public IEnumerable<(string, Stopwatch)> GetSections()
    {
        return _counters.Select(x => (x.Key, x.Value));
    }

    public IEnumerable<(string, Stopwatch)> GetSortedSections()
    {
        return GetSections().OrderBy(x => x.Item2.ElapsedTicks).ToArray();
    }

    public void RemoveSection(string key)
    {
        if (!_counters.TryRemove(key, out _)) throw new Exception("Section not found!");
    }

    public void Clear()
    {
        _counters.Clear();
    }
}
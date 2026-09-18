using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Core.History;

public sealed class RollingMetricsBuffer
{
    private readonly SystemSnapshot[] _buffer;
    private readonly int              _capacity;
    private int                       _head;
    private int                       _count;
    private readonly object           _lock = new();
    private readonly TimeProvider     _timeProvider;

    public RollingMetricsBuffer(int capacity = 600, TimeProvider? timeProvider = null)
    {
        _capacity = capacity;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _buffer   = new SystemSnapshot[capacity];
    }

    public void Add(SystemSnapshot snapshot)
    {
        lock (_lock)
        {
            _buffer[_head] = snapshot;
            _head          = (_head + 1) % _capacity;
            if (_count < _capacity) _count++;
        }
    }

    public int Count
    {
        get { lock (_lock) return _count; }
    }

    /// <summary>Returns snapshots from the last <paramref name="seconds"/> seconds, newest-last.</summary>
    public IReadOnlyList<SystemSnapshot> GetRecent(int seconds)
    {
        lock (_lock)
        {
            if (_count == 0) return Array.Empty<SystemSnapshot>();

            var cutoff  = _timeProvider.GetUtcNow().AddSeconds(-seconds);
            var results = new List<SystemSnapshot>(_count);

            for (int i = 0; i < _count; i++)
            {
                int idx      = (_head - 1 - i + _capacity) % _capacity;
                var snapshot = _buffer[idx];
                if (snapshot is null) break;
                if (snapshot.CollectedAt < cutoff) break;
                results.Add(snapshot);
            }

            results.Reverse();
            return results;
        }
    }

    /// <summary>Returns all buffered snapshots in collection order (oldest first).</summary>
    public IReadOnlyList<SystemSnapshot> GetAll()
    {
        lock (_lock)
        {
            if (_count == 0) return Array.Empty<SystemSnapshot>();

            var results = new SystemSnapshot[_count];
            for (int i = 0; i < _count; i++)
            {
                int idx     = (_head - _count + i + _capacity) % _capacity;
                results[i]  = _buffer[idx];
            }
            return results;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            Array.Clear(_buffer, 0, _capacity);
            _head  = 0;
            _count = 0;
        }
    }
}

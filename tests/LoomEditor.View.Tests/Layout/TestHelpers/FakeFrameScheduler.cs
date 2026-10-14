namespace LoomEditor.View.Tests.Layout.TestHelpers;

/// <summary>Stands in for "run this at the next animation frame": callbacks run when the test calls <see cref="RunFrame"/>.</summary>
/// <remarks>Threading: not thread-safe; tests only.</remarks>
internal sealed class FakeFrameScheduler
{
    private readonly List<Entry> _pending = [];

    public int PendingCount => _pending.Count;

    public int DisposedCount { get; private set; }

    public IDisposable Schedule(Action callback)
    {
        var entry = new Entry(this, callback);
        _pending.Add(entry);
        return entry;
    }

    public void RunFrame()
    {
        var frame = _pending.ToArray();
        _pending.Clear();
        foreach (var entry in frame)
        {
            entry.Callback();
        }
    }

    private sealed class Entry(FakeFrameScheduler owner, Action callback) : IDisposable
    {
        public Action Callback { get; } = callback;

        public void Dispose()
        {
            if (owner._pending.Remove(this))
            {
                owner.DisposedCount++;
            }
        }
    }
}

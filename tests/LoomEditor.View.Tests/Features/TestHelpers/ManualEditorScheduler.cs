using LoomEditor.View.Features.Scheduling;

namespace LoomEditor.View.Tests.Features.TestHelpers;

/// <summary>An <see cref="IEditorScheduler"/> with a fake clock: actions run when the test advances time.</summary>
internal sealed class ManualEditorScheduler : IEditorScheduler
{
    private sealed class Entry(TimeSpan due, long sequence, Action action) : IDisposable
    {
        public TimeSpan Due { get; } = due;

        public long Sequence { get; } = sequence;

        public Action Action { get; } = action;

        public bool Cancelled { get; private set; }

        public ManualEditorScheduler? Owner { get; set; }

        public void Dispose()
        {
            Cancelled = true;
            Owner?._pending.Remove(this);
        }
    }

    private readonly List<Entry> _pending = [];
    private long _sequence;

    public TimeSpan Now { get; private set; }

    public int PendingCount => _pending.Count;

    public IDisposable Schedule(TimeSpan delay, Action action)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(action);
        var entry = new Entry(Now + delay, _sequence++, action) { Owner = this };
        _pending.Add(entry);
        return entry;
    }

    /// <summary>Runs every pending action due within <paramref name="delta"/>, in due time then scheduling order.</summary>
    public void AdvanceBy(TimeSpan delta)
    {
        TimeSpan end = Now + delta;
        while (true)
        {
            Entry? next = null;
            foreach (Entry entry in _pending)
            {
                if (entry.Due <= end && (next is null || entry.Due < next.Due || (entry.Due == next.Due && entry.Sequence < next.Sequence)))
                {
                    next = entry;
                }
            }

            if (next is null)
            {
                break;
            }

            _pending.Remove(next);
            if (next.Due > Now)
            {
                Now = next.Due;
            }

            next.Action();
        }

        Now = end;
    }

    /// <summary>Runs actions until nothing is pending.</summary>
    public void RunAll()
    {
        while (_pending.Count > 0)
        {
            AdvanceBy(_pending.Min(entry => entry.Due) - Now);
        }
    }
}

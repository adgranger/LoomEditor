using LoomEditor.Core.Threading;

namespace LoomEditor.TextMate.Tests.TestHelpers;

/// <summary>An <see cref="IUiDispatcher"/> that queues the actions until <see cref="RunAll"/> is called (the "UI thread" of a test).</summary>
internal sealed class ManualUiDispatcher : IUiDispatcher
{
    private readonly Queue<Action> _queue = new();
    private readonly Lock _lock = new();

    public int PendingCount
    {
        get
        {
            lock (_lock)
            {
                return _queue.Count;
            }
        }
    }

    public bool Post(Action action)
    {
        lock (_lock)
        {
            _queue.Enqueue(action);
        }

        return true;
    }

    /// <summary>Runs the queued actions, and those they queue, in order.</summary>
    public void RunAll()
    {
        while (true)
        {
            Action action;
            lock (_lock)
            {
                if (_queue.Count == 0)
                {
                    return;
                }

                action = _queue.Dequeue();
            }

            action();
        }
    }
}

namespace LoomEditor.Lsp.Tests.TestHelpers;

/// <summary>A <c>post</c> delegate that queues the actions until <see cref="RunAll"/> runs them on the calling thread.</summary>
/// <remarks>Threading: <c>Post</c> may be called from any thread; <see cref="RunAll"/> from the test thread.</remarks>
internal sealed class ManualPost
{
    private readonly Queue<Action> _queue = new();
    private readonly object _gate = new();

    public bool Accepting { get; set; } = true;

    public int PendingCount
    {
        get
        {
            lock (_gate)
            {
                return _queue.Count;
            }
        }
    }

    public bool Post(Action action)
    {
        if (!Accepting)
        {
            return false;
        }

        lock (_gate)
        {
            _queue.Enqueue(action);
        }

        return true;
    }

    public void RunAll()
    {
        while (true)
        {
            Action action;
            lock (_gate)
            {
                if (!_queue.TryDequeue(out action!))
                {
                    return;
                }
            }

            action();
        }
    }
}

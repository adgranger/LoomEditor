using LoomEditor.Core.Model.Tokens;

namespace LoomEditor.Core.Tests.Model.Tokens.TestHelpers;

/// <summary>A scheduler driven by the test: nothing runs until <see cref="RunIdle"/> or <see cref="RunPosted"/> is called.</summary>
internal sealed class ManualTokenizationScheduler : ITokenizationScheduler
{
    private readonly List<Action<IdleDeadline>> _idle = [];
    private readonly Queue<Action> _posted = new();

    public int PendingIdleCount => _idle.Count;

    public IDisposable RunWhenIdle(Action<IdleDeadline> callback)
    {
        _idle.Add(callback);
        return new Registration(() => _idle.Remove(callback));
    }

    public void Post(Action callback) => _posted.Enqueue(callback);

    /// <summary>Runs the idle callbacks registered so far with a deadline of <paramref name="remainingMilliseconds"/>.</summary>
    public void RunIdle(double remainingMilliseconds)
    {
        Action<IdleDeadline>[] callbacks = [.. _idle];
        _idle.Clear();
        foreach (Action<IdleDeadline> callback in callbacks)
        {
            callback(new IdleDeadline(() => remainingMilliseconds));
        }
    }

    /// <summary>Runs the posted callbacks, including those they post, until none is left.</summary>
    public void RunPosted()
    {
        while (_posted.TryDequeue(out Action? callback))
        {
            callback();
        }
    }

    private sealed class Registration(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}

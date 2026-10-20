// Ported from microsoft/vscode: src/vs/base/common/async.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Scheduling;

/// <summary>
/// Runs the last task given to <see cref="Trigger"/> once, a delay after the last call.
/// </summary>
/// <remarks>
/// Behavior difference: no promises. Monaco's <c>trigger</c> returns a promise that a cancel rejects; here a
/// cancelled task simply never runs, and a caller that chained on the promise puts the continuation in the task.
/// <c>MicrotaskDelay</c> is not ported. Threading: UI thread only.
/// </remarks>
internal sealed class Delayer(IEditorScheduler scheduler, TimeSpan defaultDelay) : IDisposable
{
    private IDisposable? _deferred;
    private Action? _task;

    /// <summary>The delay <see cref="Trigger"/> uses when it is called without one.</summary>
    public TimeSpan DefaultDelay { get; set; } = defaultDelay;

    /// <summary>True from <see cref="Trigger"/> until the task runs or is cancelled.</summary>
    public bool IsTriggered => _deferred is not null;

    /// <summary>Keeps <paramref name="task"/> as the task to run and restarts the delay.</summary>
    public void Trigger(Action task, TimeSpan? delay = null)
    {
        ArgumentNullException.ThrowIfNull(task);
        _task = task;
        CancelTimeout();
        _deferred = scheduler.Schedule(delay ?? DefaultDelay, OnElapsed);
    }

    /// <summary>Drops the pending task: it never runs.</summary>
    public void Cancel()
    {
        CancelTimeout();
        _task = null;
    }

    /// <summary>Same as <see cref="Cancel"/>.</summary>
    public void Dispose() => Cancel();

    private void OnElapsed()
    {
        _deferred = null;
        Action? task = _task;
        _task = null;
        task?.Invoke();
    }

    private void CancelTimeout()
    {
        _deferred?.Dispose();
        _deferred = null;
    }
}

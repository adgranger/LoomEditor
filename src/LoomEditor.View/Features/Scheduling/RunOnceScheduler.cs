// Ported from microsoft/vscode: src/vs/base/common/async.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Scheduling;

/// <summary>Runs a runner once, a delay after the last <see cref="Schedule"/> call.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class RunOnceScheduler(IEditorScheduler scheduler, Action runner, TimeSpan delay) : IDisposable
{
    private Action? _runner = runner;
    private IDisposable? _timeoutToken;

    /// <summary>The delay <see cref="Schedule"/> uses when it is called without one.</summary>
    public TimeSpan Delay { get; set; } = delay;

    /// <summary>True while a run is scheduled.</summary>
    public bool IsScheduled => _timeoutToken is not null;

    /// <summary>Cancels the runner and drops it: it never runs again.</summary>
    public void Dispose()
    {
        Cancel();
        _runner = null;
    }

    /// <summary>Cancels the scheduled run, if any.</summary>
    public void Cancel()
    {
        _timeoutToken?.Dispose();
        _timeoutToken = null;
    }

    /// <summary>Cancels the previous run (if any) and schedules a new one.</summary>
    public void Schedule(TimeSpan? delay = null)
    {
        Cancel();
        _timeoutToken = scheduler.Schedule(delay ?? Delay, OnTimeout);
    }

    /// <summary>Runs the runner now if a run is scheduled.</summary>
    public void Flush()
    {
        if (IsScheduled)
        {
            Cancel();
            _runner?.Invoke();
        }
    }

    private void OnTimeout()
    {
        _timeoutToken = null;
        _runner?.Invoke();
    }
}

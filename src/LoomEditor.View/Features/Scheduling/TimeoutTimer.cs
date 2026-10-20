// Ported from microsoft/vscode: src/vs/base/common/async.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Scheduling;

/// <summary>A one-shot timer that can be replaced (<see cref="CancelAndSet"/>) or kept (<see cref="SetIfNotSet"/>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class TimeoutTimer(IEditorScheduler scheduler) : IDisposable
{
    private IDisposable? _token;
    private bool _isDisposed;

    /// <summary>Cancels the timer; later <see cref="CancelAndSet"/> and <see cref="SetIfNotSet"/> throw.</summary>
    public void Dispose()
    {
        Cancel();
        _isDisposed = true;
    }

    /// <summary>Cancels the pending runner, if any.</summary>
    public void Cancel()
    {
        _token?.Dispose();
        _token = null;
    }

    /// <summary>Cancels the pending runner and schedules <paramref name="runner"/>.</summary>
    /// <exception cref="InvalidOperationException">The timer is disposed.</exception>
    public void CancelAndSet(Action runner, TimeSpan timeout)
    {
        ThrowIfDisposed();
        Cancel();
        Set(runner, timeout);
    }

    /// <summary>Schedules <paramref name="runner"/> unless one is already pending.</summary>
    /// <exception cref="InvalidOperationException">The timer is disposed.</exception>
    public void SetIfNotSet(Action runner, TimeSpan timeout)
    {
        ThrowIfDisposed();
        if (_token is null)
        {
            Set(runner, timeout);
        }
    }

    private void Set(Action runner, TimeSpan timeout)
    {
        _token = scheduler.Schedule(timeout, () =>
        {
            _token = null;
            runner();
        });
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new InvalidOperationException("The TimeoutTimer is disposed.");
        }
    }
}

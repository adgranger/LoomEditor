// Ported from microsoft/vscode: src/vs/base/browser/dom.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using Avalonia.Controls;
using Avalonia.Threading;

namespace LoomEditor.Avalonia.Controls;

/// <summary>
/// Runs callbacks once at the next frame. Monaco: <c>scheduleAtNextAnimationFrame</c>.
/// </summary>
/// <remarks>
/// Threading: UI thread only (<see cref="TopLevel.RequestAnimationFrame"/> calls back there). It asks for a frame only while at
/// least one callback is pending; without a top level (the control is not in a window) the callbacks run at the next render
/// turn of the dispatcher. Behavior difference: Monaco keeps one queue per window and orders callbacks by priority; here
/// callbacks run in scheduling order.
/// </remarks>
internal sealed class AnimationFrameScheduler(Func<TopLevel?> topLevel)
{
    private List<Callback> _pending = [];
    private bool _isRequested;

    /// <summary>Schedules <paramref name="callback"/> at the next frame; disposing the result cancels it.</summary>
    public IDisposable Schedule(Action callback)
    {
        var scheduled = new Callback(callback);
        _pending.Add(scheduled);
        if (!_isRequested)
        {
            _isRequested = true;
            if (topLevel() is { } top)
            {
                top.RequestAnimationFrame(_ => OnFrame());
            }
            else
            {
                Dispatcher.UIThread.Post(OnFrame, DispatcherPriority.Render);
            }
        }

        return scheduled;
    }

    private void OnFrame()
    {
        // Callbacks may schedule again: that new callback runs at the following frame.
        List<Callback> frame = _pending;
        _pending = [];
        _isRequested = false;

        foreach (Callback callback in frame)
        {
            callback.Run();
        }
    }

    private sealed class Callback(Action action) : IDisposable
    {
        private Action? _action = action;

        public void Run() => _action?.Invoke();

        public void Dispose() => _action = null;
    }
}

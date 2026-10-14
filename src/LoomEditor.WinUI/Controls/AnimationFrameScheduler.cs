// Ported from microsoft/vscode: src/vs/base/browser/dom.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls;

/// <summary>
/// Runs callbacks once at the next frame. Monaco: <c>scheduleAtNextAnimationFrame</c>.
/// </summary>
/// <remarks>
/// Threading: UI thread only (<see cref="CompositionTarget.Rendering"/> is raised there). It listens to
/// <c>CompositionTarget.Rendering</c> only while at least one callback is pending. Behavior difference: Monaco
/// keeps one queue per window and orders callbacks by priority; here callbacks run in scheduling order.
/// </remarks>
internal sealed class AnimationFrameScheduler
{
    private List<Callback> _pending = [];
    private bool _isSubscribed;

    /// <summary>Schedules <paramref name="callback"/> at the next frame; disposing the result cancels it.</summary>
    public IDisposable Schedule(Action callback)
    {
        var scheduled = new Callback(callback);
        _pending.Add(scheduled);
        if (!_isSubscribed)
        {
            CompositionTarget.Rendering += OnRendering;
            _isSubscribed = true;
        }

        return scheduled;
    }

    private void OnRendering(object? sender, object e)
    {
        // Callbacks may schedule again: that new callback runs at the following frame.
        List<Callback> frame = _pending;
        _pending = [];
        CompositionTarget.Rendering -= OnRendering;
        _isSubscribed = false;

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

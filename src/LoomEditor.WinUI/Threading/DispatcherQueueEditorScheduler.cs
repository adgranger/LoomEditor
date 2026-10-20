using LoomEditor.View.Features.Scheduling;
using Microsoft.UI.Dispatching;

namespace LoomEditor.WinUI.Threading;

/// <summary>An <see cref="IEditorScheduler"/> that uses one <see cref="DispatcherQueueTimer"/> per call.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class DispatcherQueueEditorScheduler(DispatcherQueue queue) : IEditorScheduler
{
    private sealed class Scheduled : IDisposable
    {
        private readonly DispatcherQueueTimer _timer;
        private readonly Action _action;

        public Scheduled(DispatcherQueue queue, TimeSpan delay, Action action)
        {
            _action = action;
            _timer = queue.CreateTimer();
            _timer.Interval = delay;
            _timer.IsRepeating = false;
            _timer.Tick += OnTick;
            _timer.Start();
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Tick -= OnTick;
        }

        private void OnTick(DispatcherQueueTimer sender, object args)
        {
            Dispose();
            _action();
        }
    }

    /// <inheritdoc />
    public IDisposable Schedule(TimeSpan delay, Action action)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(action);
        return new Scheduled(queue, delay, action);
    }
}

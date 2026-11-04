using Avalonia.Threading;
using LoomEditor.View.Features.Scheduling;

namespace LoomEditor.Avalonia.Threading;

/// <summary>An <see cref="IEditorScheduler"/> that uses one <see cref="DispatcherTimer"/> per call.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class DispatcherEditorScheduler : IEditorScheduler
{
    /// <inheritdoc />
    public IDisposable Schedule(TimeSpan delay, Action action)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(action);
        return DispatcherTimer.RunOnce(action, delay);
    }
}

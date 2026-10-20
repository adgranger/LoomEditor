namespace LoomEditor.View.Features.Scheduling;

/// <summary>Runs an action on the UI thread after a delay. Features use it instead of timers of their own.</summary>
/// <remarks>Threading: call it on the UI thread only; the action runs on the UI thread.</remarks>
public interface IEditorScheduler
{
    /// <summary>
    /// Runs <paramref name="action"/> once, after at least <paramref name="delay"/>. Disposing the result before
    /// the action runs cancels it; disposing it afterwards does nothing.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delay"/> is negative.</exception>
    IDisposable Schedule(TimeSpan delay, Action action);
}

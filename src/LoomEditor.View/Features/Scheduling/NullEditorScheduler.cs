namespace LoomEditor.View.Features.Scheduling;

/// <summary>An <see cref="IEditorScheduler"/> whose actions never run.</summary>
/// <remarks>Threading: stateless, any thread.</remarks>
internal sealed class NullEditorScheduler : IEditorScheduler
{
    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    public static readonly NullEditorScheduler Instance = new();

    /// <inheritdoc />
    public IDisposable Schedule(TimeSpan delay, Action action)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(action);
        return new NoopDisposable();
    }
}

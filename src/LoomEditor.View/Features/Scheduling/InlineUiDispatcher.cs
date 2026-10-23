using LoomEditor.Core.Threading;

namespace LoomEditor.View.Features.Scheduling;

/// <summary>
/// An <see cref="IUiDispatcher"/> that runs the action at once on the calling thread. Used when an editor has no dispatcher
/// (tests, the parameterless <see cref="EditorContext"/>): callbacks run on the thread that completes the task.
/// </summary>
/// <remarks>Threading: stateless, can be used from any thread.</remarks>
internal sealed class InlineUiDispatcher : IUiDispatcher
{
    public static readonly InlineUiDispatcher Instance = new();

    private InlineUiDispatcher()
    {
    }

    /// <inheritdoc />
    public bool Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        action();
        return true;
    }
}

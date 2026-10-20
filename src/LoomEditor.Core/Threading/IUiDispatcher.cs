namespace LoomEditor.Core.Threading;

/// <summary>
/// Runs an action on the thread that owns the editor (ADR 0008, decision 5): the way a background thread hands results
/// to the UI thread without knowing the UI framework. The WinUI control implements it with a <c>DispatcherQueue</c>,
/// tests with a manual queue.
/// </summary>
/// <remarks>Threading: <see cref="Post"/> can be called from any thread; the action runs later, in order, on the owner thread.</remarks>
internal interface IUiDispatcher
{
    /// <summary>Queues <paramref name="action"/>; returns <see langword="false"/> when the owner thread cannot run it anymore (it is shutting down).</summary>
    bool Post(Action action);
}

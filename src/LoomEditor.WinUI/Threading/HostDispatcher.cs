using Microsoft.UI.Dispatching;

namespace LoomEditor.WinUI.Threading;

/// <summary>How code shared by both hosts (ADR 0017) posts work to the UI thread.</summary>
/// <remarks>Threading: call from the UI thread.</remarks>
internal static class HostDispatcher
{
    /// <summary>Posts <paramref name="action"/> to the UI thread at normal priority.</summary>
    public static bool Post(Action action) => Enqueue(DispatcherQueuePriority.Normal, action);

    /// <summary>Posts <paramref name="action"/> to the UI thread at low priority.</summary>
    public static bool PostLow(Action action) => Enqueue(DispatcherQueuePriority.Low, action);

    private static bool Enqueue(DispatcherQueuePriority priority, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return DispatcherQueue.GetForCurrentThread().TryEnqueue(priority, () => action());
    }
}

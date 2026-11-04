using Avalonia.Threading;

namespace LoomEditor.Avalonia.Threading;

/// <summary>How code shared by both hosts (ADR 0017) posts work to the UI thread.</summary>
/// <remarks>Threading: call from the UI thread.</remarks>
internal static class HostDispatcher
{
    /// <summary>Posts <paramref name="action"/> to the UI thread at normal priority.</summary>
    public static bool Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Dispatcher.UIThread.Post(action);
        return true;
    }

    /// <summary>Posts <paramref name="action"/> to the UI thread at background priority.</summary>
    public static bool PostLow(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Dispatcher.UIThread.Post(action, DispatcherPriority.Background);
        return true;
    }
}

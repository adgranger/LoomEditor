using Avalonia.Threading;

namespace LoomEditor.Gallery;

/// <summary>Posts work to the UI thread, for the Gallery code that both Galleries share.</summary>
/// <remarks>Threading: call from the UI thread.</remarks>
internal static class GalleryDispatcher
{
    /// <summary>Posts <paramref name="action"/> to the UI thread.</summary>
    public static bool Post(Action action)
    {
        Dispatcher.UIThread.Post(action);
        return true;
    }
}

using LoomEditor.Core.Threading;
using Microsoft.UI.Dispatching;

namespace LoomEditor.WinUI.Threading;

/// <summary>An <see cref="IUiDispatcher"/> over the <see cref="DispatcherQueue"/> of the UI thread.</summary>
/// <remarks>Threading: <see cref="Post"/> can be called from any thread.</remarks>
internal sealed class DispatcherQueueUiDispatcher(DispatcherQueue queue) : IUiDispatcher
{
    /// <inheritdoc />
    public bool Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return queue.TryEnqueue(() => action());
    }
}

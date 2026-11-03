using Avalonia.Threading;
using LoomEditor.Core.Threading;

namespace LoomEditor.Avalonia.Threading;

/// <summary>An <see cref="IUiDispatcher"/> over <see cref="Dispatcher.UIThread"/>.</summary>
/// <remarks>Threading: <see cref="Post"/> can be called from any thread.</remarks>
internal sealed class DispatcherUiDispatcher : IUiDispatcher
{
    /// <inheritdoc />
    public bool Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Dispatcher.UIThread.Post(action);
        return true;
    }
}

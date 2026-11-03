using LoomEditor.View.DiffEditor;

#if LOOM_WINUI
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif

/// <summary>Data of <see cref="LoomDiffEditor.BlockActionInvoked"/>: the block whose button was clicked and which button.</summary>
/// <remarks>Threading: immutable.</remarks>
public sealed class DiffBlockActionInvokedEventArgs : EventArgs
{
    /// <summary>Creates the arguments.</summary>
    /// <param name="block">The block of the button.</param>
    /// <param name="action">The button.</param>
    /// <exception cref="ArgumentNullException"><paramref name="block"/> is null.</exception>
    public DiffBlockActionInvokedEventArgs(DiffChangeBlock block, DiffBlockAction action)
    {
        ArgumentNullException.ThrowIfNull(block);
        Block = block;
        Action = action;
    }

    /// <summary>The block, the same instance as in <see cref="LoomDiffEditor.Changes"/>.</summary>
    public DiffChangeBlock Block { get; }

    /// <summary>The button that was clicked.</summary>
    public DiffBlockAction Action { get; }
}

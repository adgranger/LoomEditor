namespace LoomEditor.WinUI.Controls;

/// <summary>Data of <see cref="LoomDiffEditor.LineActionInvoked"/>: the lines the action applies to.</summary>
/// <remarks>Threading: immutable.</remarks>
public sealed class DiffLineActionInvokedEventArgs : EventArgs
{
    /// <summary>Creates the arguments.</summary>
    /// <param name="lines">The lines, never empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="lines"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="lines"/> is empty.</exception>
    public DiffLineActionInvokedEventArgs(IReadOnlyList<DiffLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count == 0)
        {
            throw new ArgumentException("At least one line is required.", nameof(lines));
        }

        Lines = lines;
    }

    /// <summary>
    /// The lines: the selection (in the order of <see cref="LoomDiffEditor.SelectedLines"/>) when the action applies to it, else the one line of the button or
    /// of the caret.
    /// </summary>
    public IReadOnlyList<DiffLine> Lines { get; }
}

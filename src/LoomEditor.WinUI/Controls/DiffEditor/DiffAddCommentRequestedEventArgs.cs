namespace LoomEditor.WinUI.Controls;

/// <summary>Data of <see cref="LoomDiffEditor.AddCommentRequested"/>: the side and line a thread was asked for.</summary>
/// <remarks>Threading: immutable.</remarks>
/// <param name="side">The side of the line.</param>
/// <param name="lineNumber">The 1-based line in that side's text.</param>
public sealed class DiffAddCommentRequestedEventArgs(DiffSide side, int lineNumber) : EventArgs
{
    /// <summary>The side of the line.</summary>
    public DiffSide Side { get; } = side;

    /// <summary>The 1-based line in that side's text.</summary>
    public int LineNumber { get; } = lineNumber;
}

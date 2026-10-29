namespace LoomEditor.WinUI.Controls;

/// <summary>Data of <see cref="LoomDiffEditor.AddCommentRequested"/>: the side and lines a thread was asked for.</summary>
/// <remarks>Threading: immutable.</remarks>
public sealed class DiffAddCommentRequestedEventArgs : EventArgs
{
    /// <summary>Creates the arguments for one line.</summary>
    /// <param name="side">The side of the line.</param>
    /// <param name="lineNumber">The 1-based line in that side's text.</param>
    public DiffAddCommentRequestedEventArgs(DiffSide side, int lineNumber) : this(side, lineNumber, lineNumber)
    {
    }

    /// <summary>Creates the arguments for a range of lines.</summary>
    /// <param name="side">The side of the lines.</param>
    /// <param name="startLineNumber">The 1-based first line in that side's text.</param>
    /// <param name="lineNumber">The 1-based last line in that side's text.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="startLineNumber"/> is greater than <paramref name="lineNumber"/>.</exception>
    public DiffAddCommentRequestedEventArgs(DiffSide side, int startLineNumber, int lineNumber)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startLineNumber, lineNumber);
        Side = side;
        StartLineNumber = startLineNumber;
        LineNumber = lineNumber;
    }

    /// <summary>The side of the lines.</summary>
    public DiffSide Side { get; }

    /// <summary>The first line of the requested range; equals <see cref="LineNumber"/> for one line.</summary>
    public int StartLineNumber { get; }

    /// <summary>The last line of the requested range (1-based, in that side's text).</summary>
    public int LineNumber { get; }
}

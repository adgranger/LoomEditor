#if LOOM_WINUI
using Range = LoomEditor.Core.Text.Range; // only the Avalonia project has the global alias
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif

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

    /// <summary>Creates the arguments for the exact text of a range.</summary>
    /// <param name="side">The side of the text.</param>
    /// <param name="textRange">The text the user asked to comment on, in that side's text.</param>
    public DiffAddCommentRequestedEventArgs(DiffSide side, Range textRange)
    {
        Side = side;
        StartLineNumber = textRange.StartLineNumber;
        LineNumber = textRange.EndLineNumber;
        TextRange = textRange;
    }

    /// <summary>The side of the lines.</summary>
    public DiffSide Side { get; }

    /// <summary>The first line of the requested range; equals <see cref="LineNumber"/> for one line.</summary>
    public int StartLineNumber { get; }

    /// <summary>The last line of the requested range (1-based, in that side's text).</summary>
    public int LineNumber { get; }

    /// <summary>
    /// The exact text the user asked to comment on; <see langword="null"/> when the request is for whole lines
    /// (<see cref="StartLineNumber"/> to <see cref="LineNumber"/>). Pass it to <c>AddCommentThread(DiffSide, Range, …)</c>.
    /// </summary>
    public Range? TextRange { get; }
}

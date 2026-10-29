namespace LoomEditor.View.Comments;

/// <summary>A selection in a comment's text: 0-based UTF-16 offsets, <c>0 &lt;= Start &lt;= End</c>.</summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
/// <param name="Start">The first selected offset (also the caret when the selection is empty).</param>
/// <param name="End">The offset after the last selected character.</param>
internal readonly record struct ToolbarSelection(int Start, int End)
{
    /// <summary>The first selected offset.</summary>
    public int Start { get; } = Start >= 0 ? Start : throw new ArgumentOutOfRangeException(nameof(Start));

    /// <summary>The offset after the last selected character.</summary>
    public int End { get; } = End >= Start ? End : throw new ArgumentOutOfRangeException(nameof(End));

    /// <summary>The number of selected characters.</summary>
    public int Length => End - Start;

    /// <summary>True when nothing is selected (a caret).</summary>
    public bool IsEmpty => Start == End;
}

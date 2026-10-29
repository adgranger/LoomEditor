using LoomEditor.Core.Diagnostics;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Comments;

/// <summary>Where a comment thread is anchored: whole lines (<see cref="TextRange"/> is null) or the exact text.</summary>
/// <remarks>Not a port. Threading: pure; <see cref="FromRange"/> and <see cref="FromSelection"/> read a
/// <see cref="TextModel"/>: UI thread.</remarks>
internal readonly record struct CommentRange(int StartLineNumber, int LineNumber, Range? TextRange)
{
    /// <summary>Whether the anchor covers whole lines rather than an exact text range.</summary>
    public bool IsWholeLines => TextRange is null;

    /// <summary>An anchor on whole lines <paramref name="startLineNumber"/> to <paramref name="lineNumber"/>.</summary>
    public static CommentRange Lines(int startLineNumber, int lineNumber)
    {
        Guard.Assert(1 <= startLineNumber && startLineNumber <= lineNumber);
        return new(startLineNumber, lineNumber, null);
    }

    /// <summary>The anchor of a selection; the direction does not matter and an empty selection gives its cursor line.</summary>
    public static CommentRange FromSelection(TextModel model, Selection selection) => FromRange(model, selection.Range);

    /// <summary>
    /// The anchor of a range: (1) the range is clamped to the model; (2) a range ending at column 1 of a later line stops at
    /// the end of the previous line; (3) an empty range is the whole line it is on; (4) a range from column 1 to the end of
    /// its last line is whole lines; (5) anything else is the exact text.
    /// </summary>
    public static CommentRange FromRange(TextModel model, Range range)
    {
        Range r = model.ValidateRange(range);
        int start = r.StartLineNumber;
        int end = r.EndLineNumber;
        int endColumn = r.EndColumn;
        if (endColumn == 1 && end > start)
        {
            end--;
            endColumn = model.GetLineMaxColumn(end);
        }

        if (start == end && endColumn <= r.StartColumn)
        {
            return Lines(start, start);
        }

        if (r.StartColumn == 1 && endColumn == model.GetLineMaxColumn(end))
        {
            return Lines(start, end);
        }

        return new(start, end, new Range(start, r.StartColumn, end, endColumn));
    }
}

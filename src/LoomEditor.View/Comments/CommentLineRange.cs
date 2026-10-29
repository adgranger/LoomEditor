using LoomEditor.Core.Text;

namespace LoomEditor.View.Comments;

/// <summary>Turns a selection into the lines a comment thread covers.</summary>
/// <remarks>Not a port. Threading: pure and thread-safe.</remarks>
internal static class CommentLineRange
{
    /// <summary>
    /// The cursor line for an empty selection, else the selected lines; a selection ending at column 1 of a later line
    /// (whole lines selected with Shift+Down) does not include that last line.
    /// </summary>
    public static (int StartLineNumber, int LineNumber) FromSelection(Selection selection)
    {
        if (selection.IsEmpty)
        {
            return (selection.PositionLineNumber, selection.PositionLineNumber);
        }

        Range r = selection.Range;
        int end = r.EndColumn == 1 && r.EndLineNumber > r.StartLineNumber ? r.EndLineNumber - 1 : r.EndLineNumber;
        return (r.StartLineNumber, end);
    }
}

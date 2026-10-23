using LoomEditor.Core.Text;

namespace LoomEditor.View.Accessibility;

/// <summary>
/// The line unit: every view line (a wrapped model line is several lines) starts at the model position of its minimum column.
/// Text hidden by folding belongs to the line before it.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class LineUnitStarts(AutomationTextDocument document) : AutomationUnitStarts(document)
{
    /// <summary>The number of view lines.</summary>
    public int ViewLineCount => Document.ViewModel.GetLineCount();

    /// <summary>The model position where the 1-based view line <paramref name="viewLine"/> starts.</summary>
    public Position ViewStart(int viewLine) =>
        Document.ViewModel.CoordinatesConverter.ConvertViewPositionToModelPosition(
            new Position(viewLine, Document.ViewModel.GetLineMinColumn(viewLine)));

    /// <summary>The greatest view line whose start is &lt;= <paramref name="position"/> (binary search).</summary>
    public int ViewLineAtOrBefore(Position position)
    {
        int low = 1;
        int high = ViewLineCount;
        while (low < high)
        {
            int mid = (low + high + 1) / 2;
            if (ViewStart(mid) <= position)
            {
                low = mid;
            }
            else
            {
                high = mid - 1;
            }
        }

        return low;
    }

    public override Position StartAtOrBefore(Position position) => ViewStart(ViewLineAtOrBefore(Document.Validate(position)));

    public override Position? PreviousStart(Position position)
    {
        position = Document.Validate(position);
        int viewLine = ViewLineAtOrBefore(position);
        var start = ViewStart(viewLine);
        if (start < position)
        {
            return start;
        }

        return viewLine > 1 ? ViewStart(viewLine - 1) : null;
    }

    public override Position? NextStartStrict(Position position)
    {
        int viewLine = ViewLineAtOrBefore(Document.Validate(position));
        return viewLine < ViewLineCount ? ViewStart(viewLine + 1) : null;
    }
}

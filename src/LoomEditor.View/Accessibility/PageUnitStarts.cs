using LoomEditor.Core.Text;

namespace LoomEditor.View.Accessibility;

/// <summary>
/// The page unit: groups of as many view lines as the completely visible range holds (at least one), counted from the first view line.
/// The page size is read at each call because the viewport may have been resized.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class PageUnitStarts(AutomationTextDocument document) : AutomationUnitStarts(document)
{
    private readonly LineUnitStarts _lines = new(document);

    public override Position StartAtOrBefore(Position position)
    {
        int n = PageSize();
        int viewLine = _lines.ViewLineAtOrBefore(Document.Validate(position));
        return _lines.ViewStart(((viewLine - 1) / n * n) + 1);
    }

    public override Position? PreviousStart(Position position)
    {
        position = Document.Validate(position);
        int n = PageSize();
        int page = (_lines.ViewLineAtOrBefore(position) - 1) / n;
        var start = _lines.ViewStart((page * n) + 1);
        if (start < position)
        {
            return start;
        }

        return page > 0 ? _lines.ViewStart(((page - 1) * n) + 1) : null;
    }

    public override Position? NextStartStrict(Position position)
    {
        int n = PageSize();
        int next = (((_lines.ViewLineAtOrBefore(Document.Validate(position)) - 1) / n) + 1) * n + 1;
        return next <= _lines.ViewLineCount ? _lines.ViewStart(next) : null;
    }

    private int PageSize()
    {
        var range = Document.ViewModel.GetCompletelyVisibleViewRange();
        return Math.Max(1, range.EndLineNumber - range.StartLineNumber + 1);
    }
}

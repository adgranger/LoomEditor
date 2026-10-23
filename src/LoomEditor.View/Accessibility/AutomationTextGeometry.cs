using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Accessibility;

/// <summary>
/// The UI-free half of <c>GetBoundingRectangles</c>, <c>RangeFromPoint</c> and <c>GetVisibleRanges</c>: from a range (or a point) to
/// editor-relative rectangles (or a position), using the measures of an <see cref="IAutomationLineMetrics"/>.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class AutomationTextGeometry(AutomationTextDocument document, IAutomationLineMetrics metrics)
{
    private readonly ViewModel _viewModel = document.ViewModel;

    /// <summary>
    /// One rectangle per visible view line the range touches, clipped to the content area, top to bottom. A collapsed range gives a
    /// caret-wide rectangle (width 1) if its line is visible.
    /// </summary>
    public IReadOnlyList<AutomationRect> GetBoundingRectangles(Range modelRange)
    {
        Range range = _viewModel.CoordinatesConverter.ConvertModelRangeToViewRange(document.Validate(modelRange));
        var data = _viewModel.ViewLayout.GetLinesViewportData();
        double scrollTop = _viewModel.ViewLayout.GetCurrentScrollTop();
        double scrollLeft = _viewModel.ViewLayout.GetCurrentScrollLeft();
        double spaceWidth = _viewModel.FontInfo.SpaceWidth;
        bool collapsed = range.IsEmpty;
        var result = new List<AutomationRect>();
        int first = Math.Max(range.StartLineNumber, data.StartLineNumber);
        int last = Math.Min(range.EndLineNumber, data.EndLineNumber);
        for (int v = first; v <= last; v++)
        {
            int startColumn = v == range.StartLineNumber ? range.StartColumn : _viewModel.GetLineMinColumn(v);
            int endColumn = v == range.EndLineNumber ? range.EndColumn : _viewModel.GetLineMaxColumn(v);
            if (!metrics.TryGetHorizontalRange(v, startColumn, endColumn, out double left, out double width))
            {
                continue;
            }

            bool lineBreakSelected = v < range.EndLineNumber;
            if (collapsed)
            {
                width = 1;
            }
            else
            {
                if (lineBreakSelected)
                {
                    width += spaceWidth;
                }

                if (width <= 0)
                {
                    continue;
                }
            }

            var rect = new AutomationRect(
                _viewModel.LayoutInfo.ContentLeft - scrollLeft + left,
                _viewModel.ViewLayout.GetVerticalOffsetForLineNumber(v) - scrollTop,
                width,
                _viewModel.ViewLayout.GetLineHeightForLineNumber(v));
            if (Clip(rect) is { } clipped)
            {
                result.Add(clipped);
            }
        }

        return result;
    }

    /// <summary>The model range of the visible view lines, or the collapsed document start when none is visible.</summary>
    public Range GetVisibleRange()
    {
        var data = _viewModel.ViewLayout.GetLinesViewportData();
        int first = data.StartLineNumber;
        int last = data.EndLineNumber;
        if (_viewModel.ViewLayout.GetCurrentViewport().Height <= 0 || last < first || first < 1 || last > _viewModel.GetLineCount())
        {
            return Range.Collapsed(document.DocumentStart);
        }

        return _viewModel.CoordinatesConverter.ConvertViewRangeToModelRange(
            new Range(first, _viewModel.GetLineMinColumn(first), last, _viewModel.GetLineMaxColumn(last)));
    }

    /// <summary>The model position nearest to a point given in editor-relative DIPs.</summary>
    public Position PositionFromPoint(double x, double y)
    {
        double scrollTop = _viewModel.ViewLayout.GetCurrentScrollTop();
        double scrollLeft = _viewModel.ViewLayout.GetCurrentScrollLeft();
        int v = Math.Clamp(_viewModel.ViewLayout.GetLineNumberAtVerticalOffset((int)(y + scrollTop)), 1, _viewModel.GetLineCount());
        double offset = Math.Max(0, x - _viewModel.LayoutInfo.ContentLeft + scrollLeft);
        Position viewPosition = metrics.GetPositionAtCoordinate(v, offset) ?? new Position(v, _viewModel.GetLineMinColumn(v));
        return _viewModel.CoordinatesConverter.ConvertViewPositionToModelPosition(viewPosition);
    }

    private AutomationRect? Clip(AutomationRect rect)
    {
        double left = Math.Max(rect.X, _viewModel.LayoutInfo.ContentLeft);
        double right = Math.Min(rect.X + rect.Width, _viewModel.LayoutInfo.ContentLeft + _viewModel.LayoutInfo.ContentWidth);
        double top = Math.Max(rect.Y, 0);
        double bottom = Math.Min(rect.Y + rect.Height, _viewModel.LayoutInfo.Height);
        return right > left && bottom > top ? new AutomationRect(left, top, right - left, bottom - top) : null;
    }
}

using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

// What the horizontal reveal needs from the renderer (Monaco: `_visibleRangesForLineRange` and `_ensureMaxLineWidth`).
internal sealed partial class TextViewRenderer
{
    /// <summary>
    /// Measures the characters <paramref name="startColumn"/> to <paramref name="endColumn"/> of a line, from the start of
    /// the line, in DIPs. Monaco: <c>_visibleRangesForLineRange</c> (one range per line: no RTL, no word wrap).
    /// </summary>
    /// <param name="viewModel">The view that owns the line.</param>
    /// <param name="lineNumber">The line.</param>
    /// <param name="startColumn">First column of the range.</param>
    /// <param name="endColumn">Column after the last character of the range.</param>
    /// <param name="left">X of the start of the range, relative to the start of the line.</param>
    /// <param name="width">Width of the range.</param>
    /// <returns><c>false</c> if the line does not exist.</returns>
    public bool TryGetHorizontalRange(
        ViewModel viewModel, int lineNumber, int startColumn, int endColumn, out double left, out double width)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        left = 0;
        width = 0;
        if (lineNumber < 1 || lineNumber > viewModel.GetLineCount())
        {
            return false;
        }

        int lineLength = viewModel.GetLineLength(lineNumber);
        // Same text as `Render`, so the layout is the one in the cache. A very long line is cut: clamp to its end.
        string text = lineLength > StopRenderingLineAfter
            ? viewModel.GetLineContent(lineNumber)[..StopRenderingLineAfter]
            : viewModel.GetLineContent(lineNumber);
        ILineLayout layout = _cache.Get(text);

        left = layout.GetCharacterX(Math.Clamp(startColumn - 1, 0, text.Length));
        double right = layout.GetCharacterX(Math.Clamp(endColumn - 1, 0, text.Length));
        width = Math.Max(0, right - left);
        return true;
    }

    /// <summary>
    /// Makes the scroll width at least <paramref name="lineWidth"/> (Monaco: <c>_ensureMaxLineWidth</c>), so that a caret
    /// at the end of a line that was not drawn yet can be scrolled to.
    /// </summary>
    public void EnsureMaxLineWidth(ViewModel viewModel, double lineWidth)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        int width = (int)Math.Ceiling(lineWidth);
        if (_maxLineWidth < width)
        {
            _maxLineWidth = width;
            viewModel.ViewLayout.SetMaxLineWidth(_maxLineWidth);
        }
    }
}

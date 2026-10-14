using System.Globalization;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

/// <summary>
/// Draws the viewport of a <see cref="ViewModel"/> on an <see cref="IRenderSurface"/>: the visible lines at their
/// offsets relative to the scroll position, the selection, and the gutter.
/// </summary>
/// <remarks>
/// <para>
/// Coordinates follow ADR 0005, decision 2: vertical offsets are computed in <c>int</c> relative to the viewport
/// (<c>scrollTop - BigNumbersDelta</c>) before they become <c>float</c>, so a 100 MB document does not lose precision.
/// Monaco: the lines container offset of <c>viewLines.ts</c>.
/// </para>
/// <para>
/// The renderer also reports the widest line it drew to <see cref="ViewLayout.SetMaxLineWidth"/>
/// (Monaco: <c>_updateLineWidths</c> / <c>_ensureMaxLineWidth</c>). Call <see cref="ResetMaxLineWidth"/> when the
/// document is flushed, or the font or the layout changes (Monaco resets <c>_maxLineWidth</c> there).
/// </para>
/// <para>
/// Line layouts are kept in a <see cref="LineLayoutCache"/> owned by the renderer. <see cref="Dispose"/> releases it.
/// Threading: UI thread only (it reads the view model).
/// </para>
/// </remarks>
internal sealed class TextViewRenderer : IDisposable
{
    /// <summary>
    /// Lines longer than this are cut, and a "Show more" label is drawn after them.
    /// Monaco: the <c>stopRenderingLineAfter</c> editor option (default 10000).
    /// </summary>
    private const int StopRenderingLineAfter = 10_000;

    private const int MinimumCacheCapacity = 256;

    private readonly ITextMeasurer _measurer;
    private readonly LineLayoutCache _cache;
    private int _maxLineWidth;

    /// <summary>Creates a renderer that lays out lines with <paramref name="measurer"/>.</summary>
    /// <remarks>Create a new renderer when the measurer changes (new font or new graphics device).</remarks>
    public TextViewRenderer(ITextMeasurer measurer)
    {
        ArgumentNullException.ThrowIfNull(measurer);

        _measurer = measurer;
        _cache = new LineLayoutCache(measurer, MinimumCacheCapacity);
    }

    /// <summary>The colors to use.</summary>
    public EditorTheme Theme { get; set; } = EditorTheme.Dark;

    /// <summary>Forgets the widest line seen so far. Monaco: <c>this._maxLineWidth = 0</c>.</summary>
    public void ResetMaxLineWidth() => _maxLineWidth = 0;

    /// <summary>Draws the visible part of the document at the current scroll position.</summary>
    /// <param name="surface">Where to draw.</param>
    /// <param name="viewModel">The view to draw.</param>
    /// <param name="selection">The selection to show.</param>
    public void Render(IRenderSurface surface, ViewModel viewModel, Selection selection)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(viewModel);

        ViewportData data = viewModel.GetViewportData(selection);
        EditorLayoutInfo layoutInfo = viewModel.LayoutInfo;
        FontInfo font = _measurer.FontInfo;
        TextModel model = viewModel.Model;

        int lineCount = data.EndLineNumber - data.StartLineNumber + 1;
        // Two layouts per visible line (number and text), plus the labels of very long lines.
        _cache.EnsureCapacity(Math.Max(MinimumCacheCapacity, 4 * lineCount));

        // Integer arithmetic first: scrollTop is of the order of the document height, the offsets are small.
        int scrollTop = (int)viewModel.ViewLayout.GetCurrentScrollTop();
        double scrollLeft = viewModel.ViewLayout.GetCurrentScrollLeft();
        int topShift = scrollTop - data.BigNumbersDelta;

        var layouts = new ILineLayout[lineCount];
        var labels = new ILineLayout?[lineCount];
        double widestLine = 1;
        for (int i = 0; i < lineCount; i++)
        {
            int lineNumber = data.StartLineNumber + i;
            int lineLength = model.GetLineLength(lineNumber);
            bool isOverflowing = lineLength > StopRenderingLineAfter;
            // Never read a whole very long line during a frame: only the part that is drawn.
            string text = isOverflowing
                ? model.GetValueInRange(new Range(lineNumber, 1, lineNumber, StopRenderingLineAfter + 1))
                : model.GetLineContent(lineNumber);

            layouts[i] = _cache.Get(text);
            double width = layouts[i].Width;
            if (isOverflowing)
            {
                labels[i] = _cache.Get(OverflowLabel.Format(lineLength - StopRenderingLineAfter));
                width += labels[i]!.Width;
            }

            widestLine = Math.Max(widestLine, width);
        }

        surface.Clear(Theme.Background);
        for (int i = 0; i < lineCount; i++)
        {
            float top = data.RelativeVerticalOffset[i] - topShift;
            DrawSelection(surface, selection, data.StartLineNumber + i, layouts[i], layoutInfo, scrollLeft, top, font);
        }

        for (int i = 0; i < lineCount; i++)
        {
            float top = data.RelativeVerticalOffset[i] - topShift;
            float left = (float)(layoutInfo.ContentLeft - scrollLeft);
            surface.DrawLineLayout(layouts[i], left, top, Theme.Foreground);
            if (labels[i] is { } label)
            {
                surface.DrawLineLayout(label, (float)(layoutInfo.ContentLeft + layouts[i].Width - scrollLeft), top, Theme.LineNumberForeground);
            }
        }

        // The gutter hides the text that is scrolled under it. Line numbers and the highlight are refined in P2-15.
        surface.FillRectangle(0, 0, layoutInfo.ContentLeft, layoutInfo.Height, Theme.Background);
        for (int i = 0; i < lineCount; i++)
        {
            float top = data.RelativeVerticalOffset[i] - topShift;
            ILineLayout number = _cache.Get((data.StartLineNumber + i).ToString(CultureInfo.InvariantCulture));
            float x = layoutInfo.LineNumbersLeft + layoutInfo.LineNumbersWidth - (float)number.Width;
            surface.DrawLineLayout(number, x, top, Theme.LineNumberForeground);
        }

        UpdateMaxLineWidth(viewModel, data, widestLine);
    }

    /// <summary>Releases the layout cache.</summary>
    public void Dispose() => _cache.Dispose();

    // Monaco: `_updateLineWidths` (the rendered lines are always measured) and `_ensureMaxLineWidth`.
    private void UpdateMaxLineWidth(ViewModel viewModel, ViewportData data, double localMaxLineWidth)
    {
        if (data.StartLineNumber == 1 && data.EndLineNumber == viewModel.Model.LineCount)
        {
            // We know the max line width for all the lines.
            _maxLineWidth = 0;
        }

        int lineWidth = (int)Math.Ceiling(localMaxLineWidth);
        if (_maxLineWidth < lineWidth)
        {
            _maxLineWidth = lineWidth;
            viewModel.ViewLayout.SetMaxLineWidth(_maxLineWidth);
        }
    }

    private void DrawSelection(
        IRenderSurface surface, Selection selection, int lineNumber, ILineLayout layout,
        EditorLayoutInfo layoutInfo, double scrollLeft, float top, FontInfo font)
    {
        Range range = selection.Range;
        if (selection.IsEmpty || lineNumber < range.StartLineNumber || lineNumber > range.EndLineNumber)
        {
            return;
        }

        int startColumn = lineNumber == range.StartLineNumber ? range.StartColumn : 1;
        // The layout of a very long line is cut: clamp to its end.
        double startX = layout.GetCharacterX(Math.Min(startColumn - 1, layout.Text.Length));
        // Selected line breaks are shown as one extra space, like in VS Code.
        double endX = lineNumber == range.EndLineNumber
            ? layout.GetCharacterX(Math.Min(range.EndColumn - 1, layout.Text.Length))
            : layout.Width + font.SpaceWidth;

        surface.FillRectangle(
            (float)(layoutInfo.ContentLeft + startX - scrollLeft), top, (float)(endX - startX), font.LineHeight, Theme.SelectionBackground);
    }
}

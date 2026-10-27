using LoomEditor.Core.Text;
using LoomEditor.Themes;
using LoomEditor.View.Features.Scheduling;
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
internal sealed partial class TextViewRenderer : IDisposable
{
    /// <summary>
    /// Lines longer than this are cut, and a "Show more" label is drawn after them.
    /// Monaco: the <c>stopRenderingLineAfter</c> editor option (default 10000).
    /// </summary>
    private const int StopRenderingLineAfter = 10_000;

    private const int MinimumCacheCapacity = 256;

    private ITextMeasurer _measurer;
    private LineLayoutCache _cache;
    private int _maxLineWidth;
    private EditorTheme _theme = EditorTheme.Dark;

    /// <summary>Creates a renderer that lays out lines with <paramref name="measurer"/>.</summary>
    /// <remarks>Create a new renderer when the measurer changes (new font or new graphics device).</remarks>
    /// <param name="measurer">Lays out the lines.</param>
    /// <param name="scheduler">Runs the minimap auto-hide timer; <see langword="null"/> keeps the minimap shown after a scroll.</param>
    /// <param name="requestRedraw">Asks the host to draw again (the auto-hide timer fired).</param>
    public TextViewRenderer(ITextMeasurer measurer, IEditorScheduler? scheduler = null, Action? requestRedraw = null)
    {
        ArgumentNullException.ThrowIfNull(measurer);

        _measurer = measurer;
        MinimapPresentation = new MinimapPresentationState(scheduler, requestRedraw);
        _cache = new LineLayoutCache(measurer, MinimumCacheCapacity);
    }

    /// <summary>The colors to use.</summary>
    /// <remarks>Setting it forgets the decoration colors resolved with the previous theme.</remarks>
    public EditorTheme Theme
    {
        get => _theme;
        set
        {
            _theme = value;
            _decorationColors.Clear();
        }
    }

    /// <summary>
    /// Width of the caret in DIPs; 0 means the default of 2. Monaco: the <c>cursorWidth</c> editor option (default 0).
    /// </summary>
    public int CursorWidth { get; set; }

    /// <summary>First line drawn by the last <see cref="Render"/> (0 before the first draw).</summary>
    public int RenderedStartLineNumber { get; private set; }

    /// <summary>Last line drawn by the last <see cref="Render"/> (0 before the first draw).</summary>
    public int RenderedEndLineNumber { get; private set; }

    /// <summary>
    /// Switches to a new measurer (font, tab size or DPI change): disposes every cached layout and forgets the widest
    /// line. Monaco: <c>ViewLines.onConfigurationChanged</c> invalidates every line and sets <c>_maxLineWidth = 0</c>.
    /// </summary>
    /// <remarks>The caller owns the measurers: the renderer does not dispose the old one.</remarks>
    public void OnFontChanged(ITextMeasurer measurer)
    {
        ArgumentNullException.ThrowIfNull(measurer);

        _cache.Dispose();
        _measurer = measurer;
        _cache = new LineLayoutCache(measurer, MinimumCacheCapacity);
        ResetMaxLineWidth();
    }

    /// <summary>Forgets the widest line seen so far. Monaco: <c>this._maxLineWidth = 0</c>.</summary>
    public void ResetMaxLineWidth() => _maxLineWidth = 0;

    /// <summary>Draws the visible part of the document at the current scroll position.</summary>
    /// <param name="surface">Where to draw.</param>
    /// <param name="viewModel">The view to draw.</param>
    /// <param name="selections">The selections to show, the primary one first (Monaco: <c>getSelections</c>).</param>
    /// <param name="hasFocus">Whether the editor has the keyboard focus.</param>
    /// <param name="cursorVisible">Whether the blinking caret is in its visible phase (see <see cref="CursorBlinking"/>).</param>
    public void Render(IRenderSurface surface, ViewModel viewModel, IReadOnlyList<Selection> selections, bool hasFocus, bool cursorVisible)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(selections);
        ArgumentOutOfRangeException.ThrowIfZero(selections.Count);

        ViewportData data = viewModel.GetViewportData(selections);
        RenderedStartLineNumber = data.StartLineNumber;
        RenderedEndLineNumber = data.EndLineNumber;
        EditorLayoutInfo layoutInfo = viewModel.LayoutInfo;
        FontInfo font = _measurer.FontInfo;

        int lineCount = data.EndLineNumber - data.StartLineNumber + 1;
        // Two layouts per visible line (number and text), plus the labels of very long lines and up to 20 sticky lines.
        _cache.EnsureCapacity(Math.Max(MinimumCacheCapacity, (4 * lineCount) + 40 + (2 * CountDeletedLines(data))));

        // Integer arithmetic first: scrollTop is of the order of the document height, the offsets are small.
        int scrollTop = (int)viewModel.ViewLayout.GetCurrentScrollTop();
        double scrollLeft = viewModel.ViewLayout.GetCurrentScrollLeft();
        int topShift = scrollTop - data.BigNumbersDelta;

        uint defaultForeground = LineStyle.ToArgb(Theme.Foreground);
        var layouts = new ILineLayout[lineCount];
        var labels = new ILineLayout?[lineCount];
        double widestLine = 1;
        for (int i = 0; i < lineCount; i++)
        {
            int lineNumber = data.StartLineNumber + i;
            int lineLength = viewModel.GetLineLength(lineNumber);
            bool isOverflowing = lineLength > StopRenderingLineAfter;
            // Never read a whole very long line during a frame: only the part that is drawn.
            string text = isOverflowing
                ? viewModel.GetLineContent(lineNumber)[..StopRenderingLineAfter]
                : viewModel.GetLineContent(lineNumber);

            // The renderer only reads tokens: the model or the background worker tokenizes.
            LineStyle style = LineStyle.Create(
                data.GetTokens(lineNumber), text.Length, viewModel.TokenColorMap, defaultForeground,
                GetLineOverrides(viewModel, data, selections, lineNumber, text.Length));
            layouts[i] = _cache.Get(text, style);
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
            foreach (Selection selection in selections)
            {
                DrawSelection(surface, selection, data.StartLineNumber + i, layouts[i], layoutInfo, scrollLeft, top, font);
            }
        }

        DrawViewZones(surface, viewModel, data, layoutInfo, scrollTop, scrollLeft);

        // Monaco: the current line highlight overlay is under the text.
        // Monaco: `_selectionIsEmpty` is true only when every selection is empty; one highlight per distinct line.
        HashSet<int> cursorLines = GetCursorLineNumbers(selections);
        if (ShouldRenderHighlightInContent(selections.All(selection => selection.IsEmpty), hasFocus))
        {
            foreach (int lineNumber in cursorLines)
            {
                int index = lineNumber - data.StartLineNumber;
                if (index >= 0 && index < lineCount)
                {
                    DrawLineHighlight(
                        surface, layoutInfo.ContentLeft, layoutInfo.Width - layoutInfo.ContentLeft,
                        data.RelativeVerticalOffset[index] - topShift, font.LineHeight);
                }
            }
        }

        // Monaco: the decorations overlay is after the selections overlay, under the text.
        DrawDecorations(surface, viewModel, data, layouts, layoutInfo, scrollLeft, topShift, font);
        DrawGuides(surface, viewModel, data, selections, layouts, layoutInfo, scrollLeft, topShift, font);

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

        DrawAfterContents(surface, data, layouts, layoutInfo, scrollLeft, topShift, font);
        DrawCompositionUnderlines(surface, viewModel, data, layouts, layoutInfo, scrollLeft, topShift, font);

        if (hasFocus && cursorVisible)
        {
            // Monaco: `ViewCursors` has one caret per selection, all with the same style.
            foreach (Selection selection in selections)
            {
                int caretIndex = selection.PositionLineNumber - data.StartLineNumber;
                if (caretIndex >= 0 && caretIndex < lineCount)
                {
                    DrawCaret(
                        surface, selection, layouts[caretIndex], layoutInfo, scrollLeft,
                        data.RelativeVerticalOffset[caretIndex] - topShift, font);
                }
            }
        }

        // The gutter hides the text that is scrolled under it.
        surface.FillRectangle(0, 0, layoutInfo.ContentLeft, layoutInfo.Height, Theme.Background);
        DrawGutter(surface, viewModel, data, layoutInfo, cursorLines, selections, hasFocus, topShift, font);
        DrawDeletedLinesMargins(surface, data, layoutInfo, scrollTop, font);

        // Monaco: the sticky scroll widget is an overlay widget above the text and the gutter.
        DrawStickyScroll(surface, viewModel, layoutInfo, scrollLeft);

        // Monaco: the minimap is a view part of its own, beside the text.
        DrawMinimap(surface, viewModel, data, layoutInfo);

        // Monaco: the overview ruler sits on top of the text, under the scrollbar.
        DrawOverviewRuler(surface, viewModel, selections, layoutInfo, font);

        UpdateMaxLineWidth(viewModel, data, widestLine);
    }

    /// <summary>Releases the layout cache.</summary>
    public void Dispose()
    {
        _cache.Dispose();
        DisposeMinimap();
        MinimapPresentation.Dispose();
    }

    // Monaco: `_updateLineWidths` (the rendered lines are always measured) and `_ensureMaxLineWidth`.
    private void UpdateMaxLineWidth(ViewModel viewModel, ViewportData data, double localMaxLineWidth)
    {
        if (data.StartLineNumber == 1 && data.EndLineNumber == viewModel.GetLineCount())
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

    // Monaco: `ViewCursor._prepareRender` for `TextEditorCursorStyle.Line`.
    private void DrawCaret(
        IRenderSurface surface, Selection selection, ILineLayout layout, EditorLayoutInfo layoutInfo,
        double scrollLeft, float top, FontInfo font)
    {
        int offset = selection.PositionColumn - 1;
        if (offset > layout.Text.Length)
        {
            // The layout of a very long line is cut: the caret is outside of the rendered line.
            return;
        }

        double lineCursorWidth = Math.Min(CursorWidth, font.TypicalHalfwidthCharacterWidth);
        // Monaco: `dom.computeScreenAwareSize`.
        double requested = lineCursorWidth > 0 ? lineCursorWidth : 2;
        double width = Math.Max(1, Math.Floor(requested * font.PixelRatio)) / font.PixelRatio;

        double left = layout.GetCharacterX(offset) - scrollLeft;
        if (width >= 2 && left >= 1)
        {
            // Shift the caret a bit between the characters.
            left -= 1;
        }

        surface.FillRectangle(
            (float)(layoutInfo.ContentLeft + left), top, (float)width, font.LineHeight, Theme.CursorForeground);
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

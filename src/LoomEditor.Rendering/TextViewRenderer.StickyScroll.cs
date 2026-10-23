// Ported from microsoft/vscode: src/vs/editor/contrib/stickyScroll/browser/stickyScrollWidget.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;
using LoomEditor.Core.Text;
using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

// The sticky lines drawn over the top of the text (Monaco: `StickyScrollWidget`, `RenderedStickyLine`, stickyScroll.css).
// Behavior differences: the shadow is a solid 2-DIP band, and relative / interval line numbers are not ported.
internal sealed partial class TextViewRenderer
{
    private const float StickyShadowHeight = 2;
    private const float StickyBorderHeight = 1;

    private StickyLayout? _stickyLayout;

    /// <summary>The index of the sticky line under the pointer (-1 when none): it is drawn with the hover background.</summary>
    public int HoveredStickyLineIndex { get; set; } = -1;

    /// <summary>Whether <paramref name="y"/> (in DIPs from the top of the editor) is over the sticky lines drawn last.</summary>
    public bool IsInStickyScroll(double y) => _stickyLayout is { } layout && y >= 0 && y < layout.Height;

    /// <summary>The index of the sticky line drawn at <paramref name="y"/>, or -1. The lines above the last one win.</summary>
    public int GetStickyLineAt(double y)
    {
        if (_stickyLayout is not { } layout || !IsInStickyScroll(y))
        {
            return -1;
        }

        int last = layout.Tops.Length - 1;
        for (int i = 0; i < last; i++)
        {
            if (y >= layout.Tops[i] && y < layout.Tops[i] + layout.Heights[i])
            {
                return i;
            }
        }

        return y >= layout.Tops[last] && y < layout.Tops[last] + layout.Heights[last] ? last : -1;
    }

    /// <summary>
    /// The view position hit in sticky line <paramref name="index"/> at <paramref name="x"/> (DIPs from the left of the editor),
    /// or <see langword="null"/> when there is no such line.
    /// </summary>
    public Position? GetStickyPositionAt(int index, double x, EditorLayoutInfo layoutInfo)
    {
        if (_stickyLayout is not { } layout || index < 0 || index >= layout.Texts.Length)
        {
            return null;
        }

        double offset = x - layoutInfo.ContentLeft + layout.TextScroll;
        return new Position(layout.ViewLineNumbers[index], layout.Texts[index].HitTest(Math.Max(0, offset)) + 1);
    }

    private sealed record StickyLayout(float[] Tops, int[] Heights, float Height, ILineLayout[] Texts, int[] ViewLineNumbers, double TextScroll);

    private sealed record StickyLine(ILineLayout Text, string? FoldingClassName, bool IsCollapsed);

    // Monaco: `_renderRootNode`, `_updatePosition`.
    private void DrawStickyScroll(IRenderSurface surface, ViewModel viewModel, EditorLayoutInfo layoutInfo, double scrollLeft)
    {
        _stickyLayout = null;
        if (viewModel.StickyScroll is not { LineNumbers.Count: > 0 } data)
        {
            return;
        }

        var lineNumbers = new List<int>();
        var viewLineNumbers = new List<int>();
        var tops = new List<float>();
        var heights = new List<int>();
        int top = 0;
        foreach (int lineNumber in data.LineNumbers)
        {
            if (lineNumber > viewModel.GetLineCount())
            {
                continue;
            }

            int viewLineNumber = viewModel.CoordinatesConverter.ConvertModelPositionToViewPosition(new Position(lineNumber, 1)).LineNumber;
            int height = viewModel.ViewLayout.GetLineHeightForLineNumber(viewLineNumber);
            lineNumbers.Add(lineNumber);
            viewLineNumbers.Add(viewLineNumber);
            tops.Add(top);
            heights.Add(height);
            top += height;
        }

        float totalHeight = top + data.LastLineRelativePosition;
        if (lineNumbers.Count == 0 || totalHeight <= 0)
        {
            return;
        }

        ThemeColor background = GetDecorationColor("editorStickyScroll.background") ?? Theme.Background;
        ThemeColor gutterBackground = GetDecorationColor("editorStickyScrollGutter.background") ?? Theme.Background;
        ThemeColor? hoverBackground = GetDecorationColor("editorStickyScrollHover.background");
        float right = layoutInfo.ContentLeft + layoutInfo.ContentWidth;
        double textScroll = viewModel.LayoutOptions.StickyScroll.ScrollWithEditor ? scrollLeft : 0;

        // The last line is under the others (z-index 0), pushed up by the next scope.
        int lastIndex = lineNumbers.Count - 1;
        var texts = new ILineLayout[lineNumbers.Count];
        foreach (int i in (int[])[lastIndex, .. Enumerable.Range(0, lastIndex)])
        {
            StickyLine line = PrepareStickyLine(viewModel, viewLineNumbers[i]);
            texts[i] = line.Text;
            if (i == lastIndex)
            {
                tops[i] += data.LastLineRelativePosition + (line.IsCollapsed ? 1 : 0);
            }

            float lineTop = tops[i];
            ThemeColor fill = i == HoveredStickyLineIndex && hoverBackground is { } hover ? hover : background;
            surface.FillRectangle(layoutInfo.ContentLeft, lineTop, right - layoutInfo.ContentLeft, heights[i], fill);
            surface.DrawLineLayout(line.Text, (float)(layoutInfo.ContentLeft - textScroll), lineTop, Theme.Foreground);

            // The gutter band hides the text scrolled under it.
            surface.FillRectangle(0, lineTop, layoutInfo.ContentLeft, heights[i], gutterBackground);
            ILineLayout number = _cache.Get(lineNumbers[i].ToString(CultureInfo.InvariantCulture));
            surface.DrawLineLayout(
                number, layoutInfo.LineNumbersLeft + layoutInfo.LineNumbersWidth - (float)number.Width, lineTop, Theme.LineNumberForeground);
            if (line.FoldingClassName is { } className)
            {
                DrawGutterIcon(surface, className, layoutInfo, lineTop, heights[i], HoveredStickyLineIndex >= 0);
            }
        }

        // stickyScroll.css: `border-bottom` and `box-shadow`.
        float bottom = totalHeight;
        if (GetDecorationColor("editorStickyScroll.border") is { } border)
        {
            surface.FillRectangle(0, bottom, right, StickyBorderHeight, border);
            bottom += StickyBorderHeight;
        }

        if (GetDecorationColor("editorStickyScroll.shadow") is { A: > 0 } shadow)
        {
            surface.FillRectangle(0, bottom, right, StickyShadowHeight, shadow);
        }

        _stickyLayout = new StickyLayout([.. tops], [.. heights], totalHeight, texts, [.. viewLineNumbers], textScroll);
    }

    // Monaco: the `RenderedStickyLine` constructor: the first view line of the model line, its inline decorations, its folding icon.
    private StickyLine PrepareStickyLine(ViewModel viewModel, int viewLineNumber)
    {
        int maxColumn = viewModel.GetLineMaxColumn(viewLineNumber);
        var range = new Range(viewLineNumber, 1, viewLineNumber, maxColumn);
        string content = viewModel.GetLineContent(viewLineNumber);
        string text = content.Length > StopRenderingLineAfter ? content[..StopRenderingLineAfter] : content;
        LineStyle style = LineStyle.Create(
            viewModel.GetViewLineTokens(viewLineNumber), text.Length, viewModel.TokenColorMap, LineStyle.ToArgb(Theme.Foreground),
            GetStyleOverrides(viewModel.GetViewportInlineDecorations(range, viewLineNumber), viewLineNumber, maxColumn, text.Length));

        // The folding icon: the lines decoration of a folding region that starts on this line.
        foreach (ViewModelDecoration d in viewModel.GetDecorationsInViewport(range))
        {
            if (d.Range.StartLineNumber != viewLineNumber)
            {
                continue;
            }

            foreach (string? className in (string?[])[d.Options.LinesDecorationsClassName, d.Options.FirstLineDecorationClassName])
            {
                if (DecorationStyles.Get(className) is { GutterIcon: not GutterIcon.None } iconStyle)
                {
                    return new StickyLine(_cache.Get(text, style), className, iconStyle.GutterIcon == GutterIcon.ChevronRight);
                }
            }
        }

        return new StickyLine(_cache.Get(text, style), null, false);
    }
}

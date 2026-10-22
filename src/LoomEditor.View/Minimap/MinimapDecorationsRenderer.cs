// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/minimap/minimap.ts (InnerMinimap.renderDecorations)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Text;
using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Minimap;

/// <summary>Paints the minimap selections and decorations (line highlights, then column highlights) into a transparent RGBA buffer drawn above the lines. Monaco: the decorations part of <c>InnerMinimap</c>.</summary>
/// <remarks>
/// Threading: UI thread only. Behavior differences: <see cref="RenderDecorations"/> returns the buffer to draw instead of
/// drawing on a canvas; the rectangles are filled in a <see cref="MinimapImageData"/> with source-over blending; section
/// headers are not ported (P6-40); this class does not implement <see cref="IMinimapRenderTarget"/>, it provides the
/// selection / decoration members of it.
/// </remarks>
internal sealed class MinimapDecorationsRenderer(IMinimapModel model, EditorTheme theme)
{
    private const int MinimapGutterWidth = 8;

    private EditorTheme _theme = theme;
    private bool _renderDecorations = true;
    private MinimapImageData? _imageData;

    /// <summary>The options changed: the buffer is recreated and repainted (Monaco: <c>onDidChangeOptions</c>).</summary>
    public void OnDidChangeOptions()
    {
        _imageData = null;
        _renderDecorations = true;
    }

    /// <summary>Everything changed (Monaco: <c>onFlushed</c>).</summary>
    public void OnFlushed() => _renderDecorations = true;

    /// <summary>The selections changed (Monaco: <c>onSelectionChanged</c>).</summary>
    public void OnSelectionChanged() => _renderDecorations = true;

    /// <summary>The minimap decorations changed (Monaco: <c>onDecorationsChanged</c>).</summary>
    public void OnDecorationsChanged() => _renderDecorations = true;

    /// <summary>The layout (scroll position) changed.</summary>
    public void OnLayoutChanged() => _renderDecorations = true;

    /// <summary>The theme changed (Monaco: <c>onThemeChanged</c>).</summary>
    public void OnThemeChanged(EditorTheme newTheme)
    {
        _theme = newTheme;
        _renderDecorations = true;
    }

    /// <summary>Repaints when something changed; returns the buffer to draw, or <c>null</c> for a canvas of no pixels. Monaco: <c>renderDecorations</c>.</summary>
    public MinimapImageData? RenderDecorations(MinimapLayout layout)
    {
        MinimapOptions options = model.Options;
        if (options.CanvasInnerWidth <= 0 || options.CanvasInnerHeight <= 0)
        {
            return null;
        }

        if (_imageData is null || _imageData.Width != options.CanvasInnerWidth || _imageData.Height != options.CanvasInnerHeight)
        {
            _imageData = new MinimapImageData(options.CanvasInnerWidth, options.CanvasInnerHeight);
            _renderDecorations = true;
        }

        if (!_renderDecorations)
        {
            return _imageData;
        }

        _renderDecorations = false;
        List<Range> selections = model.GetSelections().Select(s => s.Range).OrderBy(r => r, Comparer<Range>.Create(Range.CompareUsingStarts)).ToList();
        List<ViewModelDecoration> decorations = model.GetMinimapDecorationsInViewport(layout.StartLineNumber, layout.EndLineNumber)
            .OrderBy(d => d.Options.ZIndex).ToList();
        int minimapLineHeight = (int)options.MinimapLineHeight;
        int minimapCharWidth = (int)options.MinimapCharWidth;
        int tabSize = model.GetOptions().TabSize;

        Array.Clear(_imageData.Data);
        Rgba8? selectionColor = ToRgba8(_theme.GetColor("minimap.selectionHighlight"));

        // We first need to render line highlights and then render decorations on top of those.
        // But we need to pick a single color for each line, and use that as a line highlight.
        // This needs to be the color of the decoration with the highest `zIndex`, but priority
        // is given to the selection.
        var highlightedLines = new ContiguousLineMap<bool>(layout.StartLineNumber, layout.EndLineNumber, false);
        RenderSelectionLineHighlights(selectionColor, selections, highlightedLines, layout, minimapLineHeight);
        RenderDecorationsLineHighlights(decorations, highlightedLines, layout, minimapLineHeight);

        var lineOffsetMap = new ContiguousLineMap<int[]?>(layout.StartLineNumber, layout.EndLineNumber, null);
        RenderSelectionsHighlights(selectionColor, selections, lineOffsetMap, layout, minimapLineHeight, tabSize, minimapCharWidth);
        RenderDecorationsHighlights(decorations, lineOffsetMap, layout, minimapLineHeight, tabSize, minimapCharWidth);

        // Monaco calls _renderSectionHeaders(layout) here: section headers are not ported.
        return _imageData;
    }

    private static Rgba8? ToRgba8(ThemeColor? color) =>
        color is { A: > 0 } c ? new Rgba8(c.R, c.G, c.B, c.A) : null;

    private static Rgba8 HalfTransparent(Rgba8 c) => Rgba8.Create(c.R, c.G, c.B, c.A * 0.5);

    private Rgba8? GetDecorationColor(ModelDecorationMinimapOptions minimapOptions) =>
        ToRgba8(DecorationColorResolver.Resolve(minimapOptions.Color, minimapOptions.DarkColor, _theme));

    private void RenderSelectionLineHighlights(Rgba8? selectionColor, List<Range> selections, ContiguousLineMap<bool> highlightedLines, MinimapLayout layout, int minimapLineHeight)
    {
        if (selectionColor is not { } color)
        {
            return;
        }

        Rgba8 fill = HalfTransparent(color);
        double y1 = 0;
        double y2 = 0;

        foreach (Range selection in selections)
        {
            if (layout.IntersectWithViewport(selection) is not var (startLineNumber, endLineNumber))
            {
                // entirely outside minimap's viewport
                continue;
            }

            for (int line = startLineNumber; line <= endLineNumber; line++)
            {
                highlightedLines.Set(line, true);
            }

            double yy1 = layout.GetYForLineNumber(startLineNumber, minimapLineHeight);
            double yy2 = layout.GetYForLineNumber(endLineNumber, minimapLineHeight);

            if (y2 >= yy1)
            {
                // merge into previous
                y2 = yy2;
            }
            else
            {
                if (y2 > y1)
                {
                    // flush
                    FillRect(MinimapGutterWidth, y1, _imageData!.Width, y2 - y1, fill);
                }

                y1 = yy1;
                y2 = yy2;
            }
        }

        if (y2 > y1)
        {
            // flush
            FillRect(MinimapGutterWidth, y1, _imageData!.Width, y2 - y1, fill);
        }
    }

    private void RenderDecorationsLineHighlights(List<ViewModelDecoration> decorations, ContiguousLineMap<bool> highlightedLines, MinimapLayout layout, int minimapLineHeight)
    {
        // Loop backwards to hit first decorations with higher `zIndex`
        for (int i = decorations.Count - 1; i >= 0; i--)
        {
            ViewModelDecoration decoration = decorations[i];
            ModelDecorationMinimapOptions? minimapOptions = decoration.Options.Minimap;
            if (minimapOptions is null || minimapOptions.Position != MinimapPosition.Inline)
            {
                continue;
            }

            if (layout.IntersectWithViewport(decoration.Range) is not var (startLineNumber, endLineNumber))
            {
                continue;
            }

            if (GetDecorationColor(minimapOptions) is not { } decorationColor)
            {
                continue;
            }

            Rgba8 highlightColor = HalfTransparent(decorationColor);
            for (int line = startLineNumber; line <= endLineNumber; line++)
            {
                if (highlightedLines.Has(line))
                {
                    continue;
                }

                highlightedLines.Set(line, true);
                double y = layout.GetYForLineNumber(line, minimapLineHeight);
                FillRect(MinimapGutterWidth, y, _imageData!.Width, minimapLineHeight, highlightColor);
            }
        }
    }

    private void RenderSelectionsHighlights(Rgba8? selectionColor, List<Range> selections, ContiguousLineMap<int[]?> lineOffsetMap, MinimapLayout layout, int lineHeight, int tabSize, int characterWidth)
    {
        if (selectionColor is not { } color)
        {
            return;
        }

        foreach (Range selection in selections)
        {
            if (layout.IntersectWithViewport(selection) is not var (startLineNumber, endLineNumber))
            {
                continue;
            }

            for (int line = startLineNumber; line <= endLineNumber; line++)
            {
                RenderDecorationOnLine(lineOffsetMap, selection, color, layout, line, lineHeight, lineHeight, tabSize, characterWidth);
            }
        }
    }

    private void RenderDecorationsHighlights(List<ViewModelDecoration> decorations, ContiguousLineMap<int[]?> lineOffsetMap, MinimapLayout layout, int minimapLineHeight, int tabSize, int characterWidth)
    {
        // Loop forwards to hit first decorations with lower `zIndex`
        foreach (ViewModelDecoration decoration in decorations)
        {
            ModelDecorationMinimapOptions? minimapOptions = decoration.Options.Minimap;
            if (minimapOptions is null)
            {
                continue;
            }

            if (layout.IntersectWithViewport(decoration.Range) is not var (startLineNumber, endLineNumber))
            {
                continue;
            }

            if (GetDecorationColor(minimapOptions) is not { } decorationColor)
            {
                continue;
            }

            for (int line = startLineNumber; line <= endLineNumber; line++)
            {
                switch (minimapOptions.Position)
                {
                    case MinimapPosition.Inline:
                        RenderDecorationOnLine(lineOffsetMap, decoration.Range, decorationColor, layout, line, minimapLineHeight, minimapLineHeight, tabSize, characterWidth);
                        continue;

                    case MinimapPosition.Gutter:
                        double y = layout.GetYForLineNumber(line, minimapLineHeight);
                        FillRect(2, y, MinimapOptions.GutterDecorationWidth, minimapLineHeight, decorationColor);
                        continue;
                }
            }
        }
    }

    private void RenderDecorationOnLine(ContiguousLineMap<int[]?> lineOffsetMap, Range decorationRange, Rgba8 decorationColor, MinimapLayout layout, int lineNumber, int height, int minimapLineHeight, int tabSize, int charWidth)
    {
        double y = layout.GetYForLineNumber(lineNumber, minimapLineHeight);

        // Skip rendering the line if it's vertically outside our viewport
        if (y + height < 0 || y > model.Options.CanvasInnerHeight)
        {
            return;
        }

        int startColumn = decorationRange.StartLineNumber == lineNumber ? decorationRange.StartColumn : 1;
        int endColumn = decorationRange.EndLineNumber == lineNumber ? decorationRange.EndColumn : model.GetLineMaxColumn(lineNumber);

        int x1 = GetXOffsetForPosition(lineOffsetMap, lineNumber, startColumn, tabSize, charWidth);
        int x2 = GetXOffsetForPosition(lineOffsetMap, lineNumber, endColumn, tabSize, charWidth);

        FillRect(x1, y, x2 - x1, height, decorationColor);
    }

    /// <summary>The x offset of a column, with a per-line offset cache. Monaco: <c>getXOffsetForPosition</c>.</summary>
    internal int GetXOffsetForPosition(ContiguousLineMap<int[]?> lineOffsetMap, int lineNumber, int column, int tabSize, int charWidth)
    {
        int canvasInnerWidth = model.Options.CanvasInnerWidth;
        if (column == 1)
        {
            return MinimapGutterWidth;
        }

        int minimumXOffset = (column - 1) * charWidth;
        if (minimumXOffset >= canvasInnerWidth)
        {
            // there is no need to look at actual characters,
            // as this column is certainly after the minimap width
            return canvasInnerWidth;
        }

        // Cache line offset data so that it is only read once per line
        int[]? lineIndexToXOffset = lineOffsetMap.Get(lineNumber);
        if (lineIndexToXOffset is null)
        {
            string lineData = model.GetLineContent(lineNumber);
            var offsets = new List<int> { MinimapGutterWidth };
            int prevx = MinimapGutterWidth;
            for (int i = 1; i < lineData.Length + 1; i++)
            {
                char charCode = lineData[i - 1];
                int dx = charCode == '\t' ? tabSize * charWidth : Strings.IsFullWidthCharacter(charCode) ? 2 * charWidth : charWidth;

                int x = prevx + dx;
                if (x >= canvasInnerWidth)
                {
                    // no need to keep on going, as we've hit the canvas width
                    offsets.Add(canvasInnerWidth);
                    break;
                }

                offsets.Add(x);
                prevx = x;
            }

            lineIndexToXOffset = [.. offsets];
            lineOffsetMap.Set(lineNumber, lineIndexToXOffset);
        }

        if (column - 1 < lineIndexToXOffset.Length)
        {
            return lineIndexToXOffset[column - 1];
        }

        // goes over the canvas width
        return canvasInnerWidth;
    }

    /// <summary>Source-over blending of a rectangle, truncated to integers and clipped to the buffer, like <c>fillRect</c>.</summary>
    private void FillRect(double x, double y, double width, double height, Rgba8 color)
    {
        MinimapImageData image = _imageData!;
        int left = Math.Max(0, (int)x);
        int top = Math.Max(0, (int)y);
        int right = Math.Min(image.Width, (int)x + (int)width);
        int bottom = Math.Min(image.Height, (int)y + (int)height);
        double sa = color.A / 255.0;
        if (sa == 0)
        {
            return;
        }

        byte[] d = image.Data;
        for (int row = top; row < bottom; row++)
        {
            for (int col = left; col < right; col++)
            {
                int o = ((row * image.Width) + col) * MinimapCharSheet.RgbaChannelsCnt;
                double da = d[o + 3] / 255.0;
                double oa = sa + (da * (1 - sa));
                d[o] = Blend(color.R, d[o], sa, da, oa);
                d[o + 1] = Blend(color.G, d[o + 1], sa, da, oa);
                d[o + 2] = Blend(color.B, d[o + 2], sa, da, oa);
                d[o + 3] = Rgba8.Clamp(Math.Round(oa * 255));
            }
        }
    }

    private static byte Blend(byte src, byte dst, double sa, double da, double oa) =>
        Rgba8.Clamp(Math.Round(((src * sa) + (dst * da * (1 - sa))) / oa));
}

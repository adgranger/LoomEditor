// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/decorations/decorations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

// The backgrounds and borders of the `className` decorations of the viewport (Monaco: `DecorationsOverlay`), drawn
// above the selections and under the text. What a class draws comes from `DecorationStyles` (Monaco: CSS).
internal sealed partial class TextViewRenderer
{
    private readonly Dictionary<string, ThemeColor?> _decorationColors = new(StringComparer.Ordinal);

    private ThemeColor? GetDecorationForeground(DecorationStyle? style) =>
        style?.ForegroundResolver is { } resolver ? resolver(Theme) : GetDecorationColor(style?.ForegroundColorId);

    private ThemeColor? GetDecorationColor(string? colorId)
    {
        if (colorId is null)
        {
            return null;
        }

        if (!_decorationColors.TryGetValue(colorId, out ThemeColor? color))
        {
            color = Theme.GetColor(colorId);
            _decorationColors[colorId] = color;
        }

        return color;
    }

    // Monaco: `prepareRender`.
    private void DrawDecorations(
        IRenderSurface surface, ViewModel viewModel, ViewportData data, ILineLayout[] layouts,
        EditorLayoutInfo layoutInfo, double scrollLeft, int topShift, FontInfo font)
    {
        // Keep only decorations with `className`; sort them for consistent render output (stable sort).
        List<ViewModelDecoration> decorations =
        [
            .. data.GetDecorationsInViewport()
                .Where(d => d.Options.ClassName is not null)
                .OrderBy(d => d.Options.ZIndex)
                .ThenBy(d => d.Options.ClassName, StringComparer.Ordinal)
                .ThenBy(d => d.Range, Comparer<Range>.Create(Range.CompareUsingStarts)),
        ];

        // Render first whole line decorations and then regular decorations.
        DrawWholeLineDecorations(surface, decorations, data, layoutInfo, topShift, font);
        DrawNormalDecorations(surface, viewModel, decorations, data, layouts, layoutInfo, scrollLeft, topShift, font);
    }

    // Monaco: `_renderWholeLineDecorations`.
    private void DrawWholeLineDecorations(
        IRenderSurface surface, List<ViewModelDecoration> decorations, ViewportData data, EditorLayoutInfo layoutInfo,
        int topShift, FontInfo font)
    {
        foreach (ViewModelDecoration d in decorations)
        {
            if (!d.Options.IsWholeLine)
            {
                continue;
            }

            int startLineNumber = Math.Max(d.Range.StartLineNumber, data.StartLineNumber);
            int endLineNumber = Math.Min(d.Range.EndLineNumber, data.EndLineNumber);
            for (int lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
            {
                float top = data.RelativeVerticalOffset[lineNumber - data.StartLineNumber] - topShift;
                DrawDecorationBox(surface, d.Options.ClassName!, layoutInfo.ContentLeft, top, layoutInfo.Width - layoutInfo.ContentLeft, font.LineHeight);
            }
        }
    }

    // Monaco: `_renderNormalDecorations`.
    private void DrawNormalDecorations(
        IRenderSurface surface, ViewModel viewModel, List<ViewModelDecoration> decorations, ViewportData data,
        ILineLayout[] layouts, EditorLayoutInfo layoutInfo, double scrollLeft, int topShift, FontInfo font)
    {
        string? prevClassName = null;
        bool prevShowIfCollapsed = false;
        Range prevRange = default!;
        bool prevShouldFillLineOnLineBreak = false;

        void Flush() => DrawNormalDecoration(
            surface, prevRange, prevClassName!, prevShouldFillLineOnLineBreak, prevShowIfCollapsed, data, layouts,
            layoutInfo, scrollLeft, topShift, font);

        foreach (ViewModelDecoration d in decorations)
        {
            if (d.Options.IsWholeLine)
            {
                continue;
            }

            string className = d.Options.ClassName!;
            bool showIfCollapsed = d.Options.ShowIfCollapsed;

            Range range = d.Range;
            if (showIfCollapsed && range.EndColumn == 1 && range.EndLineNumber != range.StartLineNumber)
            {
                range = new Range(range.StartLineNumber, range.StartColumn, range.EndLineNumber - 1, viewModel.GetLineMaxColumn(range.EndLineNumber - 1));
            }

            if (prevClassName == className && prevShowIfCollapsed == showIfCollapsed && prevRange.IntersectsOrTouches(range))
            {
                // Merge into previous decoration.
                prevRange = prevRange.Plus(range);
                continue;
            }

            // Flush previous decoration.
            if (prevClassName is not null)
            {
                Flush();
            }

            prevClassName = className;
            prevShowIfCollapsed = showIfCollapsed;
            prevRange = range;
            prevShouldFillLineOnLineBreak = d.Options.ShouldFillLineOnLineBreak;
        }

        if (prevClassName is not null)
        {
            Flush();
        }
    }

    // Monaco: `_renderNormalDecoration`, with `ViewLines.linesVisibleRangesForRange` inlined (one range per line).
    private void DrawNormalDecoration(
        IRenderSurface surface, Range range, string className, bool shouldFillLineOnLineBreak, bool showIfCollapsed,
        ViewportData data, ILineLayout[] layouts, EditorLayoutInfo layoutInfo, double scrollLeft, int topShift, FontInfo font)
    {
        // TODO@Alex (Monaco): only find matches include the new lines.
        bool includeNewLines = className == "findMatch";
        double halfwidth = font.TypicalHalfwidthCharacterWidth;
        int startLineNumber = Math.Max(range.StartLineNumber, data.StartLineNumber);
        int endLineNumber = Math.Min(range.EndLineNumber, data.EndLineNumber);
        for (int lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
        {
            int index = lineNumber - data.StartLineNumber;
            ILineLayout layout = layouts[index];
            bool continuesOnNextLine = lineNumber != range.EndLineNumber;

            // Columns are clamped to the laid-out text (very long lines are cut).
            int startColumn = lineNumber == range.StartLineNumber ? range.StartColumn : 1;
            double left = layout.GetCharacterX(Math.Min(startColumn - 1, layout.Text.Length));
            double right = continuesOnNextLine
                ? layout.Width + (includeNewLines ? halfwidth : 0)
                : layout.GetCharacterX(Math.Min(range.EndColumn - 1, layout.Text.Length));
            double width = right - left;

            if (showIfCollapsed && width < halfwidth)
            {
                // Collapsed/very small range case => make the decoration visible by expanding its width
                // on both sides, keeping it centered.
                double center = Math.Round(left + width / 2);
                left = Math.Max(0, Math.Round(center - halfwidth / 2));
                width = halfwidth;
            }

            float x = (float)(layoutInfo.ContentLeft + left - scrollLeft);
            float boxWidth = shouldFillLineOnLineBreak && continuesOnNextLine ? layoutInfo.Width - x : (float)width;
            float top = data.RelativeVerticalOffset[index] - topShift;
            DrawDecorationBox(surface, className, x, top, boxWidth, font.LineHeight);
        }
    }

    // The foreground of the inline decorations of a line, in the order of its list (a later one wins).
    private List<StyleOverride>? GetStyleOverrides(IReadOnlyList<InlineDecoration> inlineDecorations, int lineNumber, int maxColumn, int length)
    {
        List<StyleOverride>? overrides = null;
        List<StyleOverride>? important = null;
        foreach (LineDecoration d in LineDecoration.Filter(inlineDecorations, lineNumber, 1, maxColumn))
        {
            // `WidthOnly` (injected text with no content) is intentionally not drawn: DirectWrite has no inline-block element.
            if (d.Type is not (InlineDecorationType.Regular or InlineDecorationType.RegularAffectingLetterSpacing)
                || DecorationStyles.Get(d.ClassName) is not { } style
                || GetDecorationForeground(style) is not { } color)
            {
                continue;
            }

            int start = Math.Min(d.StartColumn - 1, length);
            int end = Math.Min(d.EndColumn - 1, length);
            var item = new StyleOverride(start, end, LineStyle.ToArgb(color));
            if (style.Important)
            {
                (important ??= []).Add(item);
            }
            else
            {
                (overrides ??= []).Add(item);
            }
        }

        return important is null ? overrides : [.. overrides ?? [], .. important];
    }

    // The CSS `::after` content of `After` decorations (`margin-left: 0.2em`). It takes no room: the text after it is
    // not pushed right. Phase 6 cut point: before content.
    private void DrawAfterContents(
        IRenderSurface surface, ViewportData data, ILineLayout[] layouts, EditorLayoutInfo layoutInfo, double scrollLeft,
        int topShift, FontInfo font)
    {
        for (int i = 0; i < layouts.Length; i++)
        {
            int lineNumber = data.StartLineNumber + i;
            ILineLayout layout = layouts[i];
            foreach (LineDecoration d in LineDecoration.Filter(data.GetInlineDecorations(lineNumber), lineNumber, 1, layout.Text.Length + 1))
            {
                if (d.Type != InlineDecorationType.After
                    || DecorationStyles.Get(d.ClassName) is not { AfterContentText: { } text } style
                    || GetDecorationColor(style.AfterContentColorId) is not { } color)
                {
                    continue;
                }

                double x = layout.GetCharacterX(Math.Min(d.EndColumn - 1, layout.Text.Length)) + 0.2 * font.FontSize;
                surface.DrawLineLayout(
                    _cache.Get(text), (float)(layoutInfo.ContentLeft + x - scrollLeft), data.RelativeVerticalOffset[i] - topShift, color);
            }
        }
    }

    // The background, then the border inside the box (CSS `box-sizing: border-box`, like `DrawLineHighlight`).
    private void DrawDecorationBox(IRenderSurface surface, string className, float left, float top, float width, float height)
    {
        if (DecorationStyles.Get(className) is not { } style)
        {
            return;
        }

        if ((GetDecorationColor(style.BackgroundColorId) ?? GetDecorationColor(style.BackgroundFallbackColorId)) is { } background)
        {
            surface.FillRectangle(left, top, width, height, background);
        }

        if (GetDecorationColor(style.BorderColorId) is { } border)
        {
            if (style.BorderWidth > 0)
            {
                float inset = style.BorderWidth / 2;
                surface.DrawRectangle(left + inset, top + inset, width - style.BorderWidth, height - style.BorderWidth, border, style.BorderWidth);
            }

            if (style.LeftBorderWidth > 0)
            {
                surface.FillRectangle(left, top, style.LeftBorderWidth, height, border);
            }
        }

        if (style.StrikeThrough && Theme.Foreground is var strike)
        {
            surface.FillRectangle(left, top + height / 2, width, 1, strike);
        }

        if (style.Underline != UnderlineStyle.None && GetDecorationColor(style.UnderlineColorId) is { } underline)
        {
            float bottom = top + height;
            if (style.Underline == UnderlineStyle.Squiggle)
            {
                IReadOnlyList<(float X, float Y)> points = UnderlineGeometry.SquigglePoints(left, width, bottom);
                for (int i = 1; i < points.Count; i++)
                {
                    surface.DrawLine(points[i - 1].X, points[i - 1].Y, points[i].X, points[i].Y, underline, 1.2f);
                }
            }
            else
            {
                foreach ((float x, float y, float size) in UnderlineGeometry.DotPositions(left, bottom))
                {
                    surface.FillRectangle(x, y, size, size, underline);
                }
            }
        }
    }
}

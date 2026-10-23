// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/lineNumbers/lineNumbers.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.
using System.Globalization;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

// Also adapted from src/vs/editor/browser/viewParts/currentLineHighlight/currentLineHighlight.ts (same commit).
internal sealed partial class TextViewRenderer
{
    /// <summary>Monaco: the <c>renderLineHighlightOnlyWhenFocus</c> editor option (default <c>false</c>).</summary>
    private const bool RenderLineHighlightOnlyWhenFocus = false;

    /// <summary>Thickness of the line highlight border, like the 2px CSS border of Monaco.</summary>
    private const float LineHighlightBorderWidth = 2;

    /// <summary>Where the current line is highlighted. Monaco: <c>renderLineHighlight</c>, default <c>line</c>.</summary>
    public RenderLineHighlight RenderLineHighlight { get; set; } = RenderLineHighlight.Line;

    // Monaco: `_shouldRenderInContent`.
    private bool ShouldRenderHighlightInContent(bool allSelectionsEmpty, bool hasFocus) =>
        RenderLineHighlight is RenderLineHighlight.Line or RenderLineHighlight.All
        && allSelectionsEmpty
        && (!RenderLineHighlightOnlyWhenFocus || hasFocus);

    // Monaco: `_readFromSelections`: the highlighted lines are the distinct position lines of all selections.
    private static HashSet<int> GetCursorLineNumbers(IReadOnlyList<Selection> selections)
    {
        var lines = new HashSet<int>();
        foreach (Selection selection in selections)
        {
            lines.Add(selection.PositionLineNumber);
        }

        return lines;
    }

    // Monaco: `_shouldRenderInMargin`.
    private bool ShouldRenderHighlightInMargin(bool hasFocus) =>
        RenderLineHighlight is RenderLineHighlight.Gutter or RenderLineHighlight.All
        && (!RenderLineHighlightOnlyWhenFocus || hasFocus);

    // Monaco: the `.current-line` CSS rule is a background, or a border-box border of `editor.lineHighlightBorder`.
    private void DrawLineHighlight(IRenderSurface surface, float left, float width, float top, float height)
    {
        if (Theme.LineHighlightBackground is { } background)
        {
            surface.FillRectangle(left, top, width, height, background);
        }
        else if (Theme.LineHighlightBorder is { } border)
        {
            // The stroke is centered on the rectangle: inset by half of it so that it stays inside the line box.
            float inset = LineHighlightBorderWidth / 2;
            surface.DrawRectangle(
                left + inset, top + inset, width - LineHighlightBorderWidth, height - LineHighlightBorderWidth,
                border, LineHighlightBorderWidth);
        }
    }

    // Monaco: `LineNumbersOverlay.prepareRender` with `RenderLineNumbersType.On` and `renderFinalNewline = on`
    // (every line has its number, including the last empty one).
    private void DrawGutter(
        IRenderSurface surface, ViewModel viewModel, ViewportData data, EditorLayoutInfo layoutInfo, HashSet<int> cursorLines, IReadOnlyList<Selection> selections, bool hasFocus,
        int topShift, FontInfo font)
    {
        // Monaco: `onCursorStateChanged` keeps the model line of the primary cursor.
        int activeLineNumber = viewModel.CoordinatesConverter.ConvertViewPositionToModelPosition(
            new Position(selections[0].PositionLineNumber, selections[0].PositionColumn)).LineNumber;
        bool highlightMargin = ShouldRenderHighlightInMargin(hasFocus);
        int lineCount = data.EndLineNumber - data.StartLineNumber + 1;
        List<string>[] marginDecorations = RenderDedup(data.StartLineNumber, data.EndLineNumber, GetMarginDecorations(data));
        List<string>[] linesDecorations = RenderDedup(data.StartLineNumber, data.EndLineNumber, GetLinesDecorations(data));
        for (int i = 0; i < lineCount; i++)
        {
            int lineNumber = data.StartLineNumber + i;
            float top = data.RelativeVerticalOffset[i] - topShift;
            if (highlightMargin && cursorLines.Contains(lineNumber))
            {
                DrawLineHighlight(surface, 0, layoutInfo.ContentLeft, top, font.LineHeight);
            }

            // Monaco (view.ts): current line, margin decorations, lines decorations, then line numbers.
            foreach (string className in marginDecorations[i])
            {
                DrawDecorationBox(surface, className, 0, top, layoutInfo.ContentLeft, font.LineHeight);
            }

            foreach (string className in linesDecorations[i])
            {
                DrawDecorationBox(surface, className, layoutInfo.DecorationsLeft, top, layoutInfo.DecorationsWidth, font.LineHeight);
                DrawGutterIcon(surface, className, layoutInfo, top, font.LineHeight, IsMarginHovered);
            }

            // Monaco: `_getLineRenderLineNumber`: an empty number for the continuation of a wrapped model line.
            Position modelPosition = viewModel.CoordinatesConverter.ConvertViewPositionToModelPosition(new Position(lineNumber, 1));
            if (modelPosition.Column != 1)
            {
                continue;
            }

            int modelLineNumber = modelPosition.LineNumber;
            ILineLayout number = _cache.Get(modelLineNumber.ToString(CultureInfo.InvariantCulture));
            float x = layoutInfo.LineNumbersLeft + layoutInfo.LineNumbersWidth - (float)number.Width;
            surface.DrawLineLayout(
                number, x, top, modelLineNumber == activeLineNumber ? Theme.LineNumberActiveForeground : Theme.LineNumberForeground);
        }
    }
}

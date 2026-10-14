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
    private bool ShouldRenderHighlightInContent(Selection selection, bool hasFocus) =>
        RenderLineHighlight is RenderLineHighlight.Line or RenderLineHighlight.All
        && selection.IsEmpty
        && (!RenderLineHighlightOnlyWhenFocus || hasFocus);

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
        IRenderSurface surface, ViewportData data, EditorLayoutInfo layoutInfo, Selection selection, bool hasFocus,
        int topShift, FontInfo font)
    {
        int activeLineNumber = selection.PositionLineNumber;
        bool highlightMargin = ShouldRenderHighlightInMargin(hasFocus);
        int lineCount = data.EndLineNumber - data.StartLineNumber + 1;
        for (int i = 0; i < lineCount; i++)
        {
            int lineNumber = data.StartLineNumber + i;
            float top = data.RelativeVerticalOffset[i] - topShift;
            if (highlightMargin && lineNumber == activeLineNumber)
            {
                DrawLineHighlight(surface, 0, layoutInfo.ContentLeft, top, font.LineHeight);
            }

            ILineLayout number = _cache.Get(lineNumber.ToString(CultureInfo.InvariantCulture));
            float x = layoutInfo.LineNumbersLeft + layoutInfo.LineNumbersWidth - (float)number.Width;
            surface.DrawLineLayout(
                number, x, top, lineNumber == activeLineNumber ? Theme.LineNumberActiveForeground : Theme.LineNumberForeground);
        }
    }
}

// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/viewCursors/viewCursor.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

// Behavior difference: the block cursor redraws its character in the background color on top of the filled cell
// (Monaco puts the character in the cursor element).
internal sealed partial class TextViewRenderer
{
    /// <summary>The caret style in insert mode. Monaco: the <c>cursorStyle</c> editor option (default <c>Line</c>).</summary>
    public TextEditorCursorStyle CursorStyle { get; set; } = TextEditorCursorStyle.Line;

    /// <summary>The caret style in overtype mode. Monaco: the <c>overtypeCursorStyle</c> editor option (default <c>Block</c>).</summary>
    public TextEditorCursorStyle OvertypeCursorStyle { get; set; } = TextEditorCursorStyle.Block;

    /// <summary>Whether typing inserts or overwrites; selects between <see cref="CursorStyle"/> and <see cref="OvertypeCursorStyle"/>.</summary>
    public EditorInputMode InputMode { get; set; }

    // Monaco: `ViewCursor._prepareRender`.
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

        // Monaco: `EffectiveCursorStyle.compute`.
        TextEditorCursorStyle style = InputMode == EditorInputMode.Overtype ? OvertypeCursorStyle : CursorStyle;
        double thin = 1 / font.PixelRatio;
        double x = layout.GetCharacterX(offset) - scrollLeft;

        if (style is TextEditorCursorStyle.Line or TextEditorCursorStyle.LineThin)
        {
            double lineCursorWidth = Math.Min(CursorWidth, font.TypicalHalfwidthCharacterWidth);
            // Monaco: `dom.computeScreenAwareSize`.
            double requested = lineCursorWidth > 0 ? lineCursorWidth : 2;
            double width = style == TextEditorCursorStyle.LineThin
                ? thin
                : Math.Max(1, Math.Floor(requested * font.PixelRatio)) / font.PixelRatio;
            if (width >= 2 && x >= 1)
            {
                // Shift the caret a bit between the characters.
                x -= 1;
            }

            surface.FillRectangle((float)(layoutInfo.ContentLeft + x), top, (float)width, font.LineHeight, Theme.CursorForeground);
            return;
        }

        // The other styles cover the next character (one UTF-16 run to the next caret stop).
        int next = offset;
        if (offset < layout.Text.Length)
        {
            next = offset + (char.IsHighSurrogate(layout.Text[offset]) && offset + 1 < layout.Text.Length ? 2 : 1);
        }

        double cell = layout.GetCharacterX(next) - layout.GetCharacterX(offset);
        bool isTab = offset < layout.Text.Length && layout.Text[offset] == '\t';
        if (cell < 1 || isTab)
        {
            cell = font.TypicalHalfwidthCharacterWidth;
        }

        float left = (float)(layoutInfo.ContentLeft + x);
        switch (style)
        {
            case TextEditorCursorStyle.Block:
                surface.FillRectangle(left, top, (float)cell, font.LineHeight, Theme.CursorForeground);
                if (offset < layout.Text.Length && !isTab)
                {
                    surface.DrawLineLayout(_cache.Get(layout.Text[offset..next]), left, top, Theme.Background);
                }

                break;
            case TextEditorCursorStyle.BlockOutline:
                // Inside the cell, like a CSS border.
                surface.DrawRectangle(left + 0.5f, top + 0.5f, (float)cell - 1, font.LineHeight - 1, Theme.CursorForeground, 1);
                break;
            case TextEditorCursorStyle.Underline:
                surface.FillRectangle(left, top + font.LineHeight - 2, (float)cell, 2, Theme.CursorForeground);
                break;
            default:
                surface.FillRectangle(left, top + font.LineHeight - (float)thin, (float)cell, (float)thin, Theme.CursorForeground);
                break;
        }
    }
}

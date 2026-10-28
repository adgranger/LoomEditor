// Ported from microsoft/vscode: src/vs/editor/common/viewLayout/viewLineRenderer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

// Behavior difference: the cell is a rectangle behind the glyph instead of the `.mtkcontrol` CSS class.
internal sealed partial class TextViewRenderer
{
    // Monaco: `.mtkcontrol` of viewLines.css, the same in every theme.
    private static readonly ThemeColor ControlCharacterForeground = ThemeColor.Parse("#FFFFFF");
    private static readonly ThemeColor ControlCharacterBackground = ThemeColor.Parse("#960000");

    /// <summary>Whether control characters are drawn as pictures. Monaco: the <c>renderControlCharacters</c> option (default true).</summary>
    public bool RenderControlCharacters { get; set; } = true;

    // The white foreground of every control picture of the (substituted) line text, and where they are.
    private static List<StyleOverride>? AddControlCharacterOverrides(string text, List<StyleOverride>? overrides, out List<int>? offsets)
    {
        offsets = null;
        uint white = LineStyle.ToArgb(ControlCharacterForeground);
        for (int i = 0; i < text.Length; i++)
        {
            if (ControlCharacters.IsControlPicture(text[i]))
            {
                (offsets ??= []).Add(i);
                (overrides ??= []).Add(new StyleOverride(i, i + 1, white));
            }
        }

        return overrides;
    }

    private static void DrawControlCharacterCells(
        IRenderSurface surface, ViewportData data, List<int>?[] controlCharacters, ILineLayout[] layouts,
        EditorLayoutInfo layoutInfo, double scrollLeft, int topShift, FontInfo font)
    {
        for (int i = 0; i < layouts.Length; i++)
        {
            if (controlCharacters[i] is not { } offsets)
            {
                continue;
            }

            float top = data.RelativeVerticalOffset[i] - topShift;
            double left = layoutInfo.ContentLeft - scrollLeft;
            foreach (int offset in offsets)
            {
                double x0 = layouts[i].GetCharacterX(offset);
                surface.FillRectangle((float)(left + x0), top, (float)(layouts[i].GetCharacterX(offset + 1) - x0), font.LineHeight, ControlCharacterBackground);
            }
        }
    }
}

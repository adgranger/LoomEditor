// Ported from microsoft/vscode: src/vs/editor/common/viewLayout/viewLineRenderer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

// Behavior difference: the dots and arrows are drawn over the text (an overlay) instead of replacing the characters
// with `·` and `→` in the HTML of the line.
internal sealed partial class TextViewRenderer
{
    private const string MiddleDot = "·";
    private const string WordSeparatorMiddleDot = "⸱";
    private const string RightwardsArrow = "→";

    /// <summary>Which whitespace is drawn as dots and arrows. Monaco: the <c>renderWhitespace</c> option (its default is <c>selection</c>).</summary>
    public RenderWhitespace RenderWhitespace { get; set; }

    // Monaco: `RenderLineInput` (renderSpaceCharCode) and `_applyRenderWhitespace`.
    private void DrawWhitespace(
        IRenderSurface surface, ViewModel viewModel, ViewportData data, IReadOnlyList<Selection> selections,
        ILineLayout[] layouts, EditorLayoutInfo layoutInfo, double scrollLeft, int topShift, FontInfo font)
    {
        if (RenderWhitespace == RenderWhitespace.None)
        {
            return;
        }

        ILineLayout dot = _cache.Get(
            Math.Abs(font.WsmiddotWidth - font.SpaceWidth) < Math.Abs(font.MiddotWidth - font.SpaceWidth) ? WordSeparatorMiddleDot : MiddleDot);
        ILineLayout arrow = _cache.Get(RightwardsArrow);
        ThemeColor color = Theme.WhitespaceForeground;

        for (int i = 0; i < layouts.Length; i++)
        {
            ILineLayout layout = layouts[i];
            int lineNumber = data.StartLineNumber + i;
            int minColumn = viewModel.GetLineMinColumn(lineNumber);
            IReadOnlyList<OffsetRange>? selectionsOnLine = RenderWhitespace == RenderWhitespace.Selection
                ? RenderWhitespaceComputer.SelectionsOnLine(selections, lineNumber, minColumn, viewModel.GetLineMaxColumn(lineNumber))
                : null;
            IReadOnlyList<int> offsets = RenderWhitespaceComputer.Compute(
                layout.Text, layout.Text.Length, minColumn - 1, RenderWhitespace,
                viewModel.GetViewLineContinuesWithWrappedLine(lineNumber), selectionsOnLine);

            float top = data.RelativeVerticalOffset[i] - topShift;
            double left = layoutInfo.ContentLeft - scrollLeft;
            foreach (int offset in offsets)
            {
                double x0 = layout.GetCharacterX(offset);
                if (layout.Text[offset] == '\t')
                {
                    surface.DrawLineLayout(arrow, (float)(left + x0), top, color);
                }
                else
                {
                    double x1 = layout.GetCharacterX(offset + 1);
                    surface.DrawLineLayout(dot, (float)(left + x0 + ((x1 - x0 - dot.Width) / 2)), top, color);
                }
            }
        }
    }
}

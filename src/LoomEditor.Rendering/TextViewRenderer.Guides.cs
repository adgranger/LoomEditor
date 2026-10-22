// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/indentGuides/indentGuides.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Guides;
using LoomEditor.Core.Text;
using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

// Behavior difference: the guides are drawn with rectangles instead of CSS box-shadows and borders.
internal sealed partial class TextViewRenderer
{
    // Monaco: `IndentGuidesOverlay.prepareRender` and the rules of its theming participant.
    private void DrawGuides(
        IRenderSurface surface, ViewModel viewModel, ViewportData data, IReadOnlyList<Selection> selections,
        ILineLayout[] layouts, EditorLayoutInfo layoutInfo, double scrollLeft, int topShift, FontInfo font)
    {
        int lineCount = viewModel.GetLineCount();
        Selection primary = selections[0];
        IReadOnlyList<IReadOnlyList<IndentGuide>>? guides = IndentGuidesComputer.GetGuidesByLine(
            viewModel,
            viewModel.LayoutOptions.Guides,
            data.StartLineNumber,
            Math.Min(data.EndLineNumber + 1, lineCount),
            new Position(primary.PositionLineNumber, primary.PositionColumn));
        if (guides is null)
        {
            return;
        }

        double scrollWidth = viewModel.ViewLayout.GetScrollWidth();
        double maxIndentLeft = layoutInfo.WrappingColumn == -1 ? -1 : layoutInfo.WrappingColumn * font.TypicalHalfwidthCharacterWidth;
        double contentLeft = layoutInfo.ContentLeft - scrollLeft;

        for (int i = 0; i < layouts.Length; i++)
        {
            ILineLayout layout = layouts[i];
            float top = data.RelativeVerticalOffset[i] - topShift;
            foreach (IndentGuide guide in guides[i])
            {
                double left = guide.Column == -1
                    ? (guide.VisibleColumn - 1) * font.SpaceWidth
                    : layout.GetCharacterX(Math.Min(guide.Column - 1, layout.Text.Length));
                if (left > scrollWidth || (maxIndentLeft > 0 && left > maxIndentLeft))
                {
                    break;
                }

                ThemeColor? color = GetGuideColor(guide.ClassName);
                if (color is not { } themeColor)
                {
                    continue;
                }

                float x = (float)(contentLeft + left);
                if (guide.HorizontalLine is { } line)
                {
                    double end = line.EndColumn - 1 <= layout.Text.Length ? layout.GetCharacterX(line.EndColumn - 1) : left + font.SpaceWidth;
                    surface.FillRectangle(x, line.Top ? top : top + font.LineHeight - 1, (float)(end - left), 1, themeColor);
                }
                else
                {
                    surface.FillRectangle(x, top, 1, font.LineHeight, themeColor);
                }
            }
        }
    }

    // The class names are built by `IndentGuidesComputer` and `BracketPairGuidesClassNames`:
    // "core-guide-indent lvl-N" or "bracket-indent-guide lvl-N", plus " indent-active".
    private ThemeColor? GetGuideColor(string className)
    {
        bool bracket = className.StartsWith("bracket-indent-guide", StringComparison.Ordinal);
        bool active = className.EndsWith(BracketPairGuidesClassNames.ActiveClassName, StringComparison.Ordinal);
        int levelStart = className.IndexOf("lvl-", StringComparison.Ordinal) + 4;
        int levelEnd = levelStart;
        while (levelEnd < className.Length && char.IsAsciiDigit(className[levelEnd]))
        {
            levelEnd++;
        }

        if (levelStart < 4 || !int.TryParse(className.AsSpan(levelStart, levelEnd - levelStart), out int level))
        {
            return null;
        }

        return bracket
            ? GuideColors.GetBracketGuideColor(Theme, level, active)
            : GuideColors.GetIndentGuideColor(Theme, level, active);
    }
}

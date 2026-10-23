// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/indentGuides/indentGuides.ts (IndentGuidesOverlay.getGuidesByLine)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;
using LoomEditor.Core.Model.Guides;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Layout;

/// <summary>Computes the ordered guides (indent guides merged with bracket guides) of each visible view line.</summary>
/// <remarks>Threading: stateless; the view model must be used from its own thread.</remarks>
internal static class IndentGuidesComputer
{
    /// <summary>
    /// The guides of each view line of a range, or <see langword="null"/> when both guide kinds are off
    /// (Monaco: <c>prepareRender</c> sets its render result to <c>null</c>).
    /// </summary>
    /// <remarks>The caller passes <c>Math.min(visibleEndLineNumber + 1, lineCount)</c> as the end, like Monaco.</remarks>
    public static IReadOnlyList<IReadOnlyList<IndentGuide>>? GetGuidesByLine(
        ViewModel viewModel,
        EditorGuidesOptions options,
        int visibleStartLineNumber,
        int visibleEndLineNumber,
        Position? activeCursorPosition)
    {
        if (!options.Indentation && options.BracketPairs == GuideMode.Off)
        {
            return null;
        }

        List<IndentGuide>[]? bracketGuides = options.BracketPairs != GuideMode.Off
            ? viewModel.GetBracketGuidesInRangeByLine(
                visibleStartLineNumber,
                visibleEndLineNumber,
                activeCursorPosition,
                new BracketGuideOptions(
                    IncludeInactive: options.BracketPairs == GuideMode.On,
                    HorizontalGuides: options.BracketPairsHorizontal switch
                    {
                        GuideMode.On => HorizontalGuidesState.Enabled,
                        GuideMode.Active => HorizontalGuidesState.EnabledForActive,
                        _ => HorizontalGuidesState.Disabled,
                    },
                    HighlightActive: options.HighlightActiveBracketPair))
            : null;

        int[]? indentGuides = options.Indentation
            ? viewModel.GetLinesIndentGuides(visibleStartLineNumber, visibleEndLineNumber)
            : null;

        int activeIndentStartLineNumber = 0;
        int activeIndentEndLineNumber = 0;
        int activeIndentLevel = 0;
        if (options.HighlightActiveIndentation != ActiveIndentationHighlight.Off && activeCursorPosition is { } active)
        {
            ActiveIndentGuideInfo info = viewModel.GetActiveIndentGuide(active.LineNumber, visibleStartLineNumber, visibleEndLineNumber);
            activeIndentStartLineNumber = info.StartLineNumber;
            activeIndentEndLineNumber = info.EndLineNumber;
            activeIndentLevel = info.Indent;
        }

        int indentSize = viewModel.Model.Options.IndentSize;
        List<IReadOnlyList<IndentGuide>> result = [];
        for (int lineNumber = visibleStartLineNumber; lineNumber <= visibleEndLineNumber; lineNumber++)
        {
            List<IndentGuide> lineGuides = [];
            result.Add(lineGuides);

            int lineIndex = lineNumber - visibleStartLineNumber;
            List<IndentGuide> bracketGuidesInLine = bracketGuides?[lineIndex] ?? [];
            int next = 0;
            int indentGuidesInLine = indentGuides?[lineIndex] ?? 0;

            for (int indentLvl = 1; indentLvl <= indentGuidesInLine; indentLvl++)
            {
                int indentGuide = ((indentLvl - 1) * indentSize) + 1;
                bool isActive =
                    // Disable active indent guide if there are bracket guides.
                    (options.HighlightActiveIndentation == ActiveIndentationHighlight.Always || bracketGuidesInLine.Count == 0)
                    && activeIndentStartLineNumber <= lineNumber
                    && lineNumber <= activeIndentEndLineNumber
                    && indentLvl == activeIndentLevel;

                while (next < bracketGuidesInLine.Count && bracketGuidesInLine[next].VisibleColumn < indentGuide)
                {
                    lineGuides.Add(bracketGuidesInLine[next++]);
                }

                IndentGuide? peeked = next < bracketGuidesInLine.Count ? bracketGuidesInLine[next] : null;
                if (peeked is null || peeked.VisibleColumn != indentGuide || peeked.HorizontalLine is not null)
                {
                    lineGuides.Add(new IndentGuide(
                        indentGuide,
                        -1,
                        string.Create(CultureInfo.InvariantCulture, $"core-guide-indent lvl-{(indentLvl - 1) % 30}") + (isActive ? " indent-active" : string.Empty),
                        null,
                        -1,
                        -1));
                }
            }

            while (next < bracketGuidesInLine.Count)
            {
                lineGuides.Add(bracketGuidesInLine[next++]);
            }
        }

        return result;
    }
}

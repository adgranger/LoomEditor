// Ported from microsoft/vscode: src/vs/editor/common/model/guidesTextModelPart.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.BracketPairs;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.Guides;

internal sealed partial class GuidesTextModelPart
{
    /// <summary>
    /// The bracket pair guides of each line from <paramref name="startLineNumber"/> to <paramref name="endLineNumber"/>, sorted by
    /// visible column (Monaco: <c>getLinesBracketGuides</c>).
    /// </summary>
    /// <param name="startLineNumber">The first line.</param>
    /// <param name="endLineNumber">The last line.</param>
    /// <param name="activePosition">The position of the cursor, which selects the active pair; <see langword="null"/> for none.</param>
    /// <param name="options">Which guides to compute.</param>
    public List<IndentGuide>[] GetLinesBracketGuides(int startLineNumber, int endLineNumber, Position? activePosition, BracketGuideOptions options)
    {
        var result = new List<IndentGuide>[Math.Max(0, endLineNumber - startLineNumber + 1)];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = [];
        }

        // If requested, this could be made configurable.
        const bool IncludeSingleLinePairs = true;

        BracketPairWithMinIndentationInfo[] bracketPairs = textModel.BracketPairs
            .GetBracketPairsInRangeWithMinIndentation(new Range(startLineNumber, 1, endLineNumber, textModel.GetLineMaxColumn(endLineNumber)))
            .ToArray();

        Range? activeBracketPairRange = null;
        if (activePosition is { } active && bracketPairs.Length > 0)
        {
            // We don't need to query the brackets again if the cursor is in the viewport
            BracketPairInfo[] candidates = startLineNumber <= active.LineNumber && active.LineNumber <= endLineNumber
                ? bracketPairs
                : textModel.BracketPairs.GetBracketPairsInRange(Range.Collapsed(active)).ToArray();

            for (int i = candidates.Length - 1; i >= 0; i--)
            {
                var pair = candidates[i];
                if (pair.Range.StrictContains(active) && (IncludeSingleLinePairs || pair.Range.StartLineNumber != pair.Range.EndLineNumber))
                {
                    activeBracketPairRange = pair.Range;
                    break;
                }
            }
        }

        bool independentColorPoolPerBracketType = textModel.BracketPairColorizationOptions.IndependentColorPoolPerBracketType;

        foreach (var pair in bracketPairs)
        {
            /*


					{
					|
					}

					{
					|
					----}

				____{
				|test
				----}

				renderHorizontalEndLineAtTheBottom:
					{
					|
					|x}
					--
				renderHorizontalEndLineAtTheBottom:
				____{
				|test
				| x }
				----
			*/

            if (pair.ClosingBracketRange is not { } closingBracketRange)
            {
                continue;
            }

            bool isActive = activeBracketPairRange is { } activeRange && pair.Range == activeRange;

            if (!isActive && !options.IncludeInactive)
            {
                continue;
            }

            string className =
                BracketPairGuidesClassNames.GetInlineClassName(pair.NestingLevel, pair.NestingLevelOfEqualBracketType, independentColorPoolPerBracketType)
                + (options.HighlightActive && isActive ? " " + BracketPairGuidesClassNames.ActiveClassName : string.Empty);

            var start = pair.OpeningBracketRange.Start;
            var end = closingBracketRange.Start;

            bool horizontalGuides = options.HorizontalGuides == HorizontalGuidesState.Enabled
                || (options.HorizontalGuides == HorizontalGuidesState.EnabledForActive && isActive);

            if (pair.Range.StartLineNumber == pair.Range.EndLineNumber)
            {
                if (IncludeSingleLinePairs && horizontalGuides)
                {
                    result[pair.Range.StartLineNumber - startLineNumber].Add(
                        new IndentGuide(-1, pair.OpeningBracketRange.End.Column, className, new IndentGuideHorizontalLine(false, end.Column), -1, -1));
                }

                continue;
            }

            int endVisibleColumn = GetVisibleColumnFromPosition(end);
            int startVisibleColumn = GetVisibleColumnFromPosition(pair.OpeningBracketRange.Start);
            int guideVisibleColumn = Math.Min(startVisibleColumn, Math.Min(endVisibleColumn, pair.MinVisibleColumnIndentation + 1));

            bool renderHorizontalEndLineAtTheBottom = false;

            int firstNonWsIndex = StringClassification.FirstNonWhitespaceIndex(textModel.GetLineContent(closingBracketRange.StartLineNumber));
            bool hasTextBeforeClosingBracket = firstNonWsIndex < closingBracketRange.StartColumn - 1;
            if (hasTextBeforeClosingBracket)
            {
                renderHorizontalEndLineAtTheBottom = true;
            }

            int visibleGuideStartLineNumber = Math.Max(start.LineNumber, startLineNumber);
            int visibleGuideEndLineNumber = Math.Min(end.LineNumber, endLineNumber);

            int offset = renderHorizontalEndLineAtTheBottom ? 1 : 0;

            for (int l = visibleGuideStartLineNumber; l < visibleGuideEndLineNumber + offset; l++)
            {
                result[l - startLineNumber].Add(
                    new IndentGuide(
                        guideVisibleColumn,
                        -1,
                        className,
                        null,
                        l == start.LineNumber ? start.Column : -1,
                        l == end.LineNumber ? end.Column : -1));
            }

            if (horizontalGuides)
            {
                if (start.LineNumber >= startLineNumber && startVisibleColumn > guideVisibleColumn)
                {
                    result[start.LineNumber - startLineNumber].Add(
                        new IndentGuide(guideVisibleColumn, -1, className, new IndentGuideHorizontalLine(false, start.Column), -1, -1));
                }

                if (end.LineNumber <= endLineNumber && endVisibleColumn > guideVisibleColumn)
                {
                    result[end.LineNumber - startLineNumber].Add(
                        new IndentGuide(guideVisibleColumn, -1, className, new IndentGuideHorizontalLine(!renderHorizontalEndLineAtTheBottom, end.Column), -1, -1));
                }
            }
        }

        // JavaScript sort is stable, `List.Sort` is not.
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = [.. result[i].OrderBy(static g => g.VisibleColumn)];
        }

        return result;
    }

    private int GetVisibleColumnFromPosition(Position position) =>
        CursorColumns.VisibleColumnFromColumn(textModel.GetLineContent(position.LineNumber), position.Column, textModel.Options.TabSize) + 1;
}

// Ported from microsoft/vscode: src/vs/editor/contrib/folding/browser/foldingRanges.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Folding;

internal sealed partial class FoldingRegions
{
    public static FoldingRegions FromFoldRanges(IReadOnlyList<FoldRange> ranges)
    {
        ArgumentNullException.ThrowIfNull(ranges);
        int rangesLength = ranges.Count;
        var startIndexes = new uint[rangesLength];
        var endIndexes = new uint[rangesLength];
        var types = new List<string?>(rangesLength);
        bool gotTypes = false;
        for (int i = 0; i < rangesLength; i++)
        {
            FoldRange range = ranges[i];
            startIndexes[i] = (uint)range.StartLineNumber;
            endIndexes[i] = (uint)range.EndLineNumber;
            types.Add(range.Type);
            if (!string.IsNullOrEmpty(range.Type))
            {
                gotTypes = true;
            }
        }

        var regions = new FoldingRegions(startIndexes, endIndexes, gotTypes ? types : null);
        for (int i = 0; i < rangesLength; i++)
        {
            if (ranges[i].IsCollapsed)
            {
                regions.SetCollapsed(i, true);
            }

            regions.SetSource(i, ranges[i].Source);
        }

        return regions;
    }

    /// <summary>
    /// Merges two inputs, each pre-sorted on start line. The first list is assumed to include all regions currently defined by
    /// the range providers; the second only the previously collapsed and the manual ranges. When entries overlap, the second
    /// list's entry wins. Invalid entries (bad lines, out of sequence, overlapping a preceding entry without being contained
    /// by it) are discarded. Monaco: <c>FoldingRegions.sanitizeAndMerge</c>.
    /// </summary>
    public static List<FoldRange> SanitizeAndMerge(FoldingRegions rangesA, IReadOnlyList<FoldRange> rangesB, int? maxLineNumber, ISelectedLines? selection = null)
    {
        ArgumentNullException.ThrowIfNull(rangesA);
        return SanitizeAndMerge(rangesA.ToFoldRange, rangesA.Length, rangesB, maxLineNumber, selection);
    }

    /// <inheritdoc cref="SanitizeAndMerge(FoldingRegions, IReadOnlyList{FoldRange}, int?, ISelectedLines?)"/>
    public static List<FoldRange> SanitizeAndMerge(IReadOnlyList<FoldRange> rangesA, IReadOnlyList<FoldRange> rangesB, int? maxLineNumber, ISelectedLines? selection = null)
    {
        ArgumentNullException.ThrowIfNull(rangesA);
        return SanitizeAndMerge(i => rangesA[i], rangesA.Count, rangesB, maxLineNumber, selection);
    }

    private static List<FoldRange> SanitizeAndMerge(Func<int, FoldRange> itemA, int lengthA, IReadOnlyList<FoldRange> rangesB, int? maxLineNumber, ISelectedLines? selection)
    {
        ArgumentNullException.ThrowIfNull(rangesB);
        int maxLine = maxLineNumber ?? int.MaxValue;
        FoldRange? GetA(int i) => i < lengthA ? itemA(i) : null;
        FoldRange? GetB(int i) => i < rangesB.Count ? rangesB[i] : null;
        int indexA = 0;
        int indexB = 0;
        FoldRange? nextA = GetA(0);
        FoldRange? nextB = GetB(0);

        var stackedRanges = new Stack<FoldRange>();
        FoldRange? topStackedRange = null;
        int prevLineNumber = 0;
        var resultRanges = new List<FoldRange>();

        while (nextA is not null || nextB is not null)
        {
            FoldRange? useRange = null;
            if (nextB is not null && (nextA is null || nextA.StartLineNumber >= nextB.StartLineNumber))
            {
                if (nextA is not null && nextA.StartLineNumber == nextB.StartLineNumber)
                {
                    if (nextB.Source == FoldSource.UserDefined)
                    {
                        // a user defined range (possibly unfolded)
                        useRange = nextB;
                    }
                    else
                    {
                        // a previously folded range or a (possibly unfolded) recovered range
                        useRange = nextA;
                        // stays collapsed if the range still has the same number of lines or the selection is not in the range or after it
                        useRange.IsCollapsed = nextB.IsCollapsed
                            && (nextA.EndLineNumber == nextB.EndLineNumber || !(selection?.StartsInside(nextA.StartLineNumber + 1, nextA.EndLineNumber + 1) ?? false));
                        useRange.Source = FoldSource.Provider;
                    }

                    nextA = GetA(++indexA); // not necessary, just for speed
                }
                else
                {
                    useRange = nextB;
                    if (nextB.IsCollapsed && nextB.Source == FoldSource.Provider)
                    {
                        // a previously collapsed range
                        useRange.Source = FoldSource.Recovered;
                    }
                }

                nextB = GetB(++indexB);
            }
            else
            {
                // nextA is next. The user folded B set takes precedence and we sometimes need to look
                // ahead in it to check for an upcoming conflict.
                int scanIndex = indexB;
                FoldRange? prescanB = nextB;
                while (true)
                {
                    if (prescanB is null || prescanB.StartLineNumber > nextA!.EndLineNumber)
                    {
                        useRange = nextA;
                        break; // no conflict, use this nextA
                    }

                    if (prescanB.Source == FoldSource.UserDefined && prescanB.EndLineNumber > nextA.EndLineNumber)
                    {
                        // we found a user folded range, it wins
                        break; // without setting nextResult, so this nextA gets skipped
                    }

                    prescanB = GetB(++scanIndex);
                }

                nextA = GetA(++indexA);
            }

            if (useRange is not null)
            {
                while (topStackedRange is not null && topStackedRange.EndLineNumber < useRange.StartLineNumber)
                {
                    topStackedRange = stackedRanges.Count > 0 ? stackedRanges.Pop() : null;
                }

                if (useRange.EndLineNumber > useRange.StartLineNumber
                    && useRange.StartLineNumber > prevLineNumber
                    && useRange.EndLineNumber <= maxLine
                    && (topStackedRange is null || topStackedRange.EndLineNumber >= useRange.EndLineNumber))
                {
                    resultRanges.Add(useRange);
                    prevLineNumber = useRange.StartLineNumber;
                    if (topStackedRange is not null)
                    {
                        stackedRanges.Push(topStackedRange);
                    }

                    topStackedRange = useRange;
                }
            }
        }

        return resultRanges;
    }
}


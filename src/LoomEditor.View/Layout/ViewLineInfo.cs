// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelLines.ts (ViewLineInfo, normalizeLineRanges)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>A view line: the model line it belongs to and its index among the view lines of that model line (Monaco: <c>ViewLineInfo</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct ViewLineInfo(int ModelLineNumber, int ModelLineWrappedLineIdx)
{
    public bool IsWrappedLineContinuation => ModelLineWrappedLineIdx > 0;
}

/// <summary>Helpers on ranges that only matter by their lines.</summary>
/// <remarks>Threading: stateless.</remarks>
internal static class LineRanges
{
    /// <summary>
    /// Merges overlapping or touching ranges into sorted, non overlapping ranges that start and end at column 1
    /// (Monaco: <c>normalizeLineRanges</c>). Only line information is considered: columns are ignored.
    /// </summary>
    public static List<Range> Normalize(IReadOnlyList<Range> ranges)
    {
        if (ranges.Count == 0)
        {
            return [];
        }

        // OrderBy is stable, like Array.prototype.sort
        List<Range> sortedRanges = [.. ranges.OrderBy(r => r, Comparer<Range>.Create(Range.CompareUsingStarts))];

        List<Range> result = [];
        int currentRangeStart = sortedRanges[0].StartLineNumber;
        int currentRangeEnd = sortedRanges[0].EndLineNumber;

        for (int i = 1; i < sortedRanges.Count; i++)
        {
            Range range = sortedRanges[i];

            if (range.StartLineNumber > currentRangeEnd + 1)
            {
                result.Add(new Range(currentRangeStart, 1, currentRangeEnd, 1));
                currentRangeStart = range.StartLineNumber;
                currentRangeEnd = range.EndLineNumber;
            }
            else if (range.EndLineNumber > currentRangeEnd)
            {
                currentRangeEnd = range.EndLineNumber;
            }
        }

        result.Add(new Range(currentRangeStart, 1, currentRangeEnd, 1));
        return result;
    }
}

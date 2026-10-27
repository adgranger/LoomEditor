// Ported from microsoft/vscode: src/vs/editor/common/diff/documentDiffProvider.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Diff;

/// <summary>The result of diffing two models (Monaco: <c>IDocumentDiff</c>).</summary>
/// <param name="Identical">True when both models have the same lines (their EOLs may differ).</param>
/// <param name="QuitEarly">True when the computation was cancelled or timed out: the diff may be an approximation.</param>
/// <param name="Changes">The line-level changes with their inner character changes.</param>
/// <param name="Moves">The moved blocks of lines.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record DocumentDiff(bool Identical, bool QuitEarly, IReadOnlyList<DetailedLineRangeMapping> Changes, IReadOnlyList<MovedText> Moves)
{
    /// <summary>Monaco: <c>nullDocumentDiff</c>.</summary>
    public static readonly DocumentDiff Null = new(true, false, [], []);

    /// <summary>
    /// Monaco: <c>normalizeDocumentDiff</c> / <c>normalizeRangeMapping</c> (diffEditorViewModel.ts), written over line counts and max
    /// columns so that it touches no model. An inner change that starts at column 1 and ends at the end of a line, on both sides, is
    /// extended to the start of the next line.
    /// </summary>
    public DocumentDiff Normalize(
        int originalLineCount, Func<int, int> originalLineMaxColumn, int modifiedLineCount, Func<int, int> modifiedLineMaxColumn)
    {
        DetailedLineRangeMapping[] changes = [.. Changes.Select(c => new DetailedLineRangeMapping(
            c.Original,
            c.Modified,
            c.InnerChanges?.Select(i => NormalizeRangeMapping(i, originalLineCount, originalLineMaxColumn, modifiedLineCount, modifiedLineMaxColumn)).ToArray()))];
        return this with { Changes = changes };
    }

    private static RangeMapping NormalizeRangeMapping(
        RangeMapping mapping, int originalLineCount, Func<int, int> originalLineMaxColumn, int modifiedLineCount, Func<int, int> modifiedLineMaxColumn)
    {
        Range originalRange = mapping.OriginalRange;
        Range modifiedRange = mapping.ModifiedRange;
        if (originalRange.StartColumn == 1 && modifiedRange.StartColumn == 1
            && (originalRange.EndColumn != 1 || modifiedRange.EndColumn != 1)
            && originalRange.EndColumn == originalLineMaxColumn(originalRange.EndLineNumber)
            && modifiedRange.EndColumn == modifiedLineMaxColumn(modifiedRange.EndLineNumber)
            && originalRange.EndLineNumber < originalLineCount
            && modifiedRange.EndLineNumber < modifiedLineCount)
        {
            originalRange = originalRange.WithEnd(new(originalRange.EndLineNumber + 1, 1));
            modifiedRange = modifiedRange.WithEnd(new(modifiedRange.EndLineNumber + 1, 1));
        }

        return new RangeMapping(originalRange, modifiedRange);
    }
}

// Ported from microsoft/vscode: src/vs/editor/common/diff/defaultLinesDiffComputer/defaultLinesDiffComputer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diagnostics;
using LoomEditor.Core.Diff.Algorithms;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Diff;

/// <summary>
/// Monaco's default line diff: a line-level alignment (dynamic programming for small inputs, Myers otherwise),
/// refined into character-level inner changes. Moved lines are not computed yet (<see cref="ComputeMoves"/> returns none).
/// </summary>
/// <remarks>Threading: the instance only holds two stateless algorithm objects, so ComputeDiff may be called concurrently.</remarks>
internal sealed class DefaultLinesDiffComputer : ILinesDiffComputer
{
    private readonly DynamicProgrammingDiffing _dynamicProgrammingDiffing = new();
    private readonly MyersDiffAlgorithm _myersDiffingAlgorithm = new();

    /// <inheritdoc />
    public LinesDiff ComputeDiff(string[] originalLines, string[] modifiedLines, LinesDiffComputerOptions options)
    {
        if (originalLines.Length <= 1 && originalLines.AsSpan().SequenceEqual(modifiedLines))
        {
            return new LinesDiff([], [], false);
        }

        if ((originalLines.Length == 1 && originalLines[0].Length == 0) || (modifiedLines.Length == 1 && modifiedLines[0].Length == 0))
        {
            return new LinesDiff(
                [
                    new DetailedLineRangeMapping(
                        new LineRange(1, originalLines.Length + 1),
                        new LineRange(1, modifiedLines.Length + 1),
                        [
                            new RangeMapping(
                                new Range(1, 1, originalLines.Length, originalLines[^1].Length + 1),
                                new Range(1, 1, modifiedLines.Length, modifiedLines[^1].Length + 1)),
                        ]),
                ],
                [],
                false);
        }

        ITimeout timeout = options.MaxComputationTimeMs == 0 ? InfiniteTimeout.Instance : new DateTimeout(options.MaxComputationTimeMs);
        bool considerWhitespaceChanges = !options.IgnoreTrimWhitespace;

        var perfectHashes = new Dictionary<string, int>();
        int GetOrCreateHash(string text)
        {
            if (!perfectHashes.TryGetValue(text, out int hash))
            {
                hash = perfectHashes.Count;
                perfectHashes[text] = hash;
            }

            return hash;
        }

        int[] originalLinesHashes = originalLines.Select(l => GetOrCreateHash(TrimJs(l))).ToArray();
        int[] modifiedLinesHashes = modifiedLines.Select(l => GetOrCreateHash(TrimJs(l))).ToArray();

        var sequence1 = new LineSequence(originalLinesHashes, originalLines);
        var sequence2 = new LineSequence(modifiedLinesHashes, modifiedLines);

        DiffAlgorithmResult lineAlignmentResult = sequence1.Length + sequence2.Length < 1700
            // Use the improved algorithm for small files
            ? DynamicProgrammingDiffing.Compute(
                sequence1,
                sequence2,
                timeout,
                (offset1, offset2) =>
                    originalLines[offset1] == modifiedLines[offset2]
                        ? modifiedLines[offset2].Length == 0 ? 0.1 : 1 + Math.Log(1 + modifiedLines[offset2].Length)
                        : 0.99)
            : _myersDiffingAlgorithm.Compute(sequence1, sequence2, timeout);

        IReadOnlyList<SequenceDiff> lineAlignments = lineAlignmentResult.Diffs;
        bool hitTimeout = lineAlignmentResult.HitTimeout;
        lineAlignments = HeuristicSequenceOptimizations.OptimizeSequenceDiffs(sequence1, sequence2, lineAlignments);
        lineAlignments = HeuristicSequenceOptimizations.RemoveVeryShortMatchingLinesBetweenDiffs(sequence1, sequence2, lineAlignments);

        var alignments = new List<RangeMapping>();
        int seq1LastStart = 0;
        int seq2LastStart = 0;

        void ScanForWhitespaceChanges(int equalLinesCount)
        {
            if (!considerWhitespaceChanges)
            {
                return;
            }

            for (int i = 0; i < equalLinesCount; i++)
            {
                int seq1Offset = seq1LastStart + i;
                int seq2Offset = seq2LastStart + i;
                if (originalLines[seq1Offset] != modifiedLines[seq2Offset])
                {
                    // This is because of whitespace changes, diff these lines
                    (List<RangeMapping> mappings, bool refineTimeout) = RefineDiff(
                        originalLines,
                        modifiedLines,
                        new SequenceDiff(new OffsetRange(seq1Offset, seq1Offset + 1), new OffsetRange(seq2Offset, seq2Offset + 1)),
                        timeout,
                        considerWhitespaceChanges,
                        options);
                    alignments.AddRange(mappings);
                    hitTimeout |= refineTimeout;
                }
            }
        }

        foreach (SequenceDiff diff in lineAlignments)
        {
            Guard.Assert(diff.Seq1Range.Start - seq1LastStart == diff.Seq2Range.Start - seq2LastStart);

            int equalLinesCount = diff.Seq1Range.Start - seq1LastStart;
            ScanForWhitespaceChanges(equalLinesCount);

            seq1LastStart = diff.Seq1Range.EndExclusive;
            seq2LastStart = diff.Seq2Range.EndExclusive;

            (List<RangeMapping> mappings, bool refineTimeout) = RefineDiff(originalLines, modifiedLines, diff, timeout, considerWhitespaceChanges, options);
            hitTimeout |= refineTimeout;
            alignments.AddRange(mappings);
        }

        ScanForWhitespaceChanges(originalLines.Length - seq1LastStart);

        List<DetailedLineRangeMapping> changes = LineRangeMappingConversions.LineRangeMappingFromRangeMappings(
            alignments, new ArrayText(originalLines), new ArrayText(modifiedLines));

        IReadOnlyList<MovedText> moves = options.ComputeMoves ? ComputeMoves() : [];

        Guard.Assert(ValidateChanges(changes, originalLines, modifiedLines));

        return new LinesDiff(changes, moves, hitTimeout);
    }

    /// <summary><c>computeMoves</c>: not ported yet (P9-09 fills it).</summary>
    private static IReadOnlyList<MovedText> ComputeMoves() => [];

    private (List<RangeMapping> Mappings, bool HitTimeout) RefineDiff(
        string[] originalLines, string[] modifiedLines, SequenceDiff diff, ITimeout timeout, bool considerWhitespaceChanges, LinesDiffComputerOptions options)
    {
        LineRangeMapping lineRangeMapping = ToLineRangeMapping(diff);
        RangeMapping rangeMapping = lineRangeMapping.ToRangeMapping2(originalLines, modifiedLines);

        var slice1 = new LinesSliceCharSequence(originalLines, rangeMapping.OriginalRange, considerWhitespaceChanges);
        var slice2 = new LinesSliceCharSequence(modifiedLines, rangeMapping.ModifiedRange, considerWhitespaceChanges);

        DiffAlgorithmResult diffResult = slice1.Length + slice2.Length < 500
            ? _dynamicProgrammingDiffing.Compute(slice1, slice2, timeout)
            : _myersDiffingAlgorithm.Compute(slice1, slice2, timeout);

        IReadOnlyList<SequenceDiff> diffs = diffResult.Diffs;
        diffs = HeuristicSequenceOptimizations.OptimizeSequenceDiffs(slice1, slice2, diffs);
        diffs = HeuristicSequenceOptimizations.ExtendDiffsToEntireWordIfAppropriate(slice1, slice2, diffs, (seq, idx) => seq.FindWordContaining(idx));

        if (options.ExtendToSubwords)
        {
            diffs = HeuristicSequenceOptimizations.ExtendDiffsToEntireWordIfAppropriate(slice1, slice2, diffs, (seq, idx) => seq.FindSubWordContaining(idx), true);
        }

        diffs = HeuristicSequenceOptimizations.RemoveShortMatches(slice1, slice2, diffs);
        diffs = HeuristicSequenceOptimizations.RemoveVeryShortMatchingTextBetweenLongDiffs(slice1, slice2, diffs);

        List<RangeMapping> result = diffs
            .Select(d => new RangeMapping(slice1.TranslateRange(d.Seq1Range), slice2.TranslateRange(d.Seq2Range)))
            .ToList();

        return (result, diffResult.HitTimeout);
    }

    private static LineRangeMapping ToLineRangeMapping(SequenceDiff sequenceDiff) => new(
        new LineRange(sequenceDiff.Seq1Range.Start + 1, sequenceDiff.Seq1Range.EndExclusive + 1),
        new LineRange(sequenceDiff.Seq2Range.Start + 1, sequenceDiff.Seq2Range.EndExclusive + 1));

    // Monaco's assertFn block: make sure all ranges are valid.
    private static bool ValidateChanges(List<DetailedLineRangeMapping> changes, string[] originalLines, string[] modifiedLines)
    {
        foreach (DetailedLineRangeMapping c in changes)
        {
            if (c.InnerChanges is null)
            {
                return false;
            }

            foreach (RangeMapping ic in c.InnerChanges)
            {
                bool valid = ValidatePosition(ic.ModifiedRange.StartLineNumber, ic.ModifiedRange.StartColumn, modifiedLines)
                    && ValidatePosition(ic.ModifiedRange.EndLineNumber, ic.ModifiedRange.EndColumn, modifiedLines)
                    && ValidatePosition(ic.OriginalRange.StartLineNumber, ic.OriginalRange.StartColumn, originalLines)
                    && ValidatePosition(ic.OriginalRange.EndLineNumber, ic.OriginalRange.EndColumn, originalLines);
                if (!valid)
                {
                    return false;
                }
            }

            if (!ValidateRange(c.Modified, modifiedLines) || !ValidateRange(c.Original, originalLines))
            {
                return false;
            }
        }

        return true;

        static bool ValidatePosition(int lineNumber, int column, string[] lines) =>
            lineNumber >= 1 && lineNumber <= lines.Length && column >= 1 && column <= lines[lineNumber - 1].Length + 1;

        static bool ValidateRange(LineRange range, string[] lines) =>
            range.StartLineNumber >= 1 && range.StartLineNumber <= lines.Length + 1
            && range.EndLineNumberExclusive >= 1 && range.EndLineNumberExclusive <= lines.Length + 1;
    }

    // JavaScript's String.prototype.trim and string.Trim() differ (U+0085, U+001C-U+001F vs U+FEFF).
    private static string TrimJs(string s)
    {
        int start = 0;
        int end = s.Length;
        while (start < end && IsJsWhitespace(s[start]))
        {
            start++;
        }

        while (end > start && IsJsWhitespace(s[end - 1]))
        {
            end--;
        }

        return s[start..end];
    }

    private static bool IsJsWhitespace(char c)
    {
        // Code points: tab..CR, space, NBSP, U+1680, U+2000-U+200A, U+2028, U+2029, U+202F, U+205F, U+3000, U+FEFF.
        int i = c;
        return i is (>= 9 and <= 13) or 32 or 160 or 5760 or (>= 8192 and <= 8202) or 8232 or 8233 or 8239 or 8287 or 12288 or 65279;
    }
}

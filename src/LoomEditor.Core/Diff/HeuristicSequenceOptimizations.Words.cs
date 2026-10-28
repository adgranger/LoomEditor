// Ported from microsoft/vscode: src/vs/editor/common/diff/defaultLinesDiffComputer/heuristicSequenceOptimizations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diff.Algorithms;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Diff;

internal static partial class HeuristicSequenceOptimizations
{
    /// <summary>
    /// <c>extendDiffsToEntireWordIfAppropriate</c>: extends a character diff to the whole (sub-)word around it when
    /// most of that word changed. Monaco's <c>force</c> parameter is <paramref name="forceSubWords"/>.
    /// </summary>
    public static IReadOnlyList<SequenceDiff> ExtendDiffsToEntireWordIfAppropriate(
        LinesSliceCharSequence sequence1,
        LinesSliceCharSequence sequence2,
        IReadOnlyList<SequenceDiff> sequenceDiffs,
        Func<LinesSliceCharSequence, int, OffsetRange?> findParent,
        bool forceSubWords = false)
    {
        // Monaco shifts items off this array; the index plays the role of the queue head.
        SequenceDiff[] equalMappings = SequenceDiff.Invert([.. sequenceDiffs], sequence1.Length);
        int head = 0;

        List<SequenceDiff> additional = [];
        OffsetPair lastPoint = OffsetPair.Zero;

        void ScanWord(OffsetPair pair, SequenceDiff equalMapping)
        {
            if (pair.Offset1 < lastPoint.Offset1 || pair.Offset2 < lastPoint.Offset2)
            {
                return;
            }

            OffsetRange? w1 = findParent(sequence1, pair.Offset1);
            OffsetRange? w2 = findParent(sequence2, pair.Offset2);
            if (w1 is null || w2 is null)
            {
                return;
            }

            var w = new SequenceDiff(w1.Value, w2.Value);
            SequenceDiff equalPart = w.Intersect(equalMapping)!;

            int equalChars1 = equalPart.Seq1Range.Length;
            int equalChars2 = equalPart.Seq2Range.Length;

            // The words do not touch previous equals mappings, as we would have processed them already.
            // But they might touch the next ones.
            while (head < equalMappings.Length)
            {
                SequenceDiff next = equalMappings[head];
                bool intersects = next.Seq1Range.Intersects(w.Seq1Range) || next.Seq2Range.Intersects(w.Seq2Range);
                if (!intersects)
                {
                    break;
                }

                OffsetRange? v1 = findParent(sequence1, next.Seq1Range.Start);
                OffsetRange? v2 = findParent(sequence2, next.Seq2Range.Start);
                // Because there is an intersection, we know that the words are not empty.
                var v = new SequenceDiff(v1!.Value, v2!.Value);
                SequenceDiff nextEqualPart = v.Intersect(next)!;

                equalChars1 += nextEqualPart.Seq1Range.Length;
                equalChars2 += nextEqualPart.Seq2Range.Length;

                w = w.Join(v);

                if (w.Seq1Range.EndExclusive >= next.Seq1Range.EndExclusive)
                {
                    // The word extends beyond the next equal mapping.
                    head++;
                }
                else
                {
                    break;
                }
            }

            int changedChars = w.Seq1Range.Length + w.Seq2Range.Length;
            if ((forceSubWords && equalChars1 + equalChars2 < changedChars) || equalChars1 + equalChars2 < changedChars * 2.0 / 3)
            {
                additional.Add(w);
            }

            lastPoint = w.GetEndExclusives();
        }

        while (head < equalMappings.Length)
        {
            SequenceDiff next = equalMappings[head++];
            if (next.Seq1Range.IsEmpty)
            {
                continue;
            }

            ScanWord(next.GetStarts(), next);
            // The equal parts are not empty, so -1 gives us a character that is equal in both parts.
            ScanWord(next.GetEndExclusives().Delta(-1), next);
        }

        return MergeSequenceDiffs(sequenceDiffs, additional);
    }

    private static List<SequenceDiff> MergeSequenceDiffs(IReadOnlyList<SequenceDiff> sequenceDiffs1, List<SequenceDiff> sequenceDiffs2)
    {
        List<SequenceDiff> result = [];
        int i1 = 0;
        int i2 = 0;

        while (i1 < sequenceDiffs1.Count || i2 < sequenceDiffs2.Count)
        {
            SequenceDiff? sd1 = i1 < sequenceDiffs1.Count ? sequenceDiffs1[i1] : null;
            SequenceDiff? sd2 = i2 < sequenceDiffs2.Count ? sequenceDiffs2[i2] : null;

            SequenceDiff next = sd1 is not null && (sd2 is null || sd1.Seq1Range.Start < sd2.Seq1Range.Start)
                ? sequenceDiffs1[i1++]
                : sequenceDiffs2[i2++];

            if (result.Count > 0 && result[^1].Seq1Range.EndExclusive >= next.Seq1Range.Start)
            {
                result[^1] = result[^1].Join(next);
            }
            else
            {
                result.Add(next);
            }
        }

        return result;
    }

    /// <summary>
    /// <c>removeVeryShortMatchingLinesBetweenDiffs</c>. Whitespace is <see cref="char.IsWhiteSpace(char)"/>, which
    /// matches JavaScript's <c>\s</c> for every character but U+FEFF.
    /// </summary>
    public static IReadOnlyList<SequenceDiff> RemoveVeryShortMatchingLinesBetweenDiffs(
        LineSequence sequence1, LineSequence sequence2, IReadOnlyList<SequenceDiff> sequenceDiffs)
    {
        _ = sequence2;
        IReadOnlyList<SequenceDiff> diffs = sequenceDiffs;
        if (diffs.Count == 0)
        {
            return diffs;
        }

        int counter = 0;
        bool shouldRepeat;
        do
        {
            shouldRepeat = false;
            List<SequenceDiff> result = [diffs[0]];

            for (int i = 1; i < diffs.Count; i++)
            {
                SequenceDiff cur = diffs[i];
                SequenceDiff lastResult = result[^1];

                if (ShouldJoinLines(sequence1, lastResult, cur))
                {
                    shouldRepeat = true;
                    result[^1] = lastResult.Join(cur);
                }
                else
                {
                    result.Add(cur);
                }
            }

            diffs = result;
        }
        while (counter++ < 10 && shouldRepeat);

        return diffs;
    }

    private static bool ShouldJoinLines(LineSequence sequence1, SequenceDiff before, SequenceDiff after)
    {
        var unchangedRange = new OffsetRange(before.Seq1Range.EndExclusive, after.Seq1Range.Start);
        string unchangedText = sequence1.GetText(unchangedRange);
        int nonWhitespace = unchangedText.Count(c => !char.IsWhiteSpace(c));
        return nonWhitespace <= 4
            && (before.Seq1Range.Length + before.Seq2Range.Length > 5 || after.Seq1Range.Length + after.Seq2Range.Length > 5);
    }

    /// <summary>
    /// <c>removeVeryShortMatchingTextBetweenLongDiffs</c>. <c>trim()</c> is <see cref="string.Trim()"/> (Monaco's
    /// JavaScript <c>trim</c> and <c>\s</c> are the Unicode white space, as is <see cref="char.IsWhiteSpace(char)"/>).
    /// </summary>
    public static IReadOnlyList<SequenceDiff> RemoveVeryShortMatchingTextBetweenLongDiffs(
        LinesSliceCharSequence sequence1, LinesSliceCharSequence sequence2, IReadOnlyList<SequenceDiff> sequenceDiffs)
    {
        IReadOnlyList<SequenceDiff> diffs = sequenceDiffs;
        if (diffs.Count == 0)
        {
            return diffs;
        }

        int counter = 0;
        bool shouldRepeat;
        do
        {
            shouldRepeat = false;
            List<SequenceDiff> result = [diffs[0]];

            for (int i = 1; i < diffs.Count; i++)
            {
                SequenceDiff cur = diffs[i];
                SequenceDiff lastResult = result[^1];

                if (ShouldJoinText(sequence1, sequence2, lastResult, cur))
                {
                    shouldRepeat = true;
                    result[^1] = lastResult.Join(cur);
                }
                else
                {
                    result.Add(cur);
                }
            }

            diffs = result;
        }
        while (counter++ < 10 && shouldRepeat);

        List<SequenceDiff> newDiffs = [];

        // Remove short suffixes/prefixes
        for (int i = 0; i < diffs.Count; i++)
        {
            SequenceDiff? prev = i > 0 ? diffs[i - 1] : null;
            SequenceDiff cur = diffs[i];
            SequenceDiff? next = i + 1 < diffs.Count ? diffs[i + 1] : null;
            SequenceDiff newDiff = cur;

            bool ShouldMarkAsChanged(string text) =>
                text.Length > 0 && text.Trim().Length <= 3 && cur.Seq1Range.Length + cur.Seq2Range.Length > 100;

            OffsetRange fullRange1 = sequence1.ExtendToFullLines(cur.Seq1Range);
            string prefix = sequence1.GetText(new OffsetRange(fullRange1.Start, cur.Seq1Range.Start));
            if (ShouldMarkAsChanged(prefix))
            {
                newDiff = newDiff.DeltaStart(-prefix.Length);
            }

            string suffix = sequence1.GetText(new OffsetRange(cur.Seq1Range.EndExclusive, fullRange1.EndExclusive));
            if (ShouldMarkAsChanged(suffix))
            {
                newDiff = newDiff.DeltaEnd(suffix.Length);
            }

            SequenceDiff availableSpace = SequenceDiff.FromOffsetPairs(
                prev is not null ? prev.GetEndExclusives() : OffsetPair.Zero,
                next is not null ? next.GetStarts() : OffsetPair.Max);
            SequenceDiff joined = newDiff.Intersect(availableSpace)!;
            if (newDiffs.Count > 0 && joined.GetStarts().Equals(newDiffs[^1].GetEndExclusives()))
            {
                newDiffs[^1] = newDiffs[^1].Join(joined);
            }
            else
            {
                newDiffs.Add(joined);
            }
        }

        return newDiffs;
    }

    private static bool ShouldJoinText(LinesSliceCharSequence sequence1, LinesSliceCharSequence sequence2, SequenceDiff before, SequenceDiff after)
    {
        var unchangedRange = new OffsetRange(before.Seq1Range.EndExclusive, after.Seq1Range.Start);

        int unchangedLineCount = sequence1.CountLinesIn(unchangedRange);
        if (unchangedLineCount > 5 || unchangedRange.Length > 500)
        {
            return false;
        }

        string unchangedText = sequence1.GetText(unchangedRange).Trim();
        if (unchangedText.Length > 20 || unchangedText.AsSpan().IndexOfAny('\r', '\n') >= 0)
        {
            return false;
        }

        int beforeLineCount1 = sequence1.CountLinesIn(before.Seq1Range);
        int beforeSeq1Length = before.Seq1Range.Length;
        int beforeLineCount2 = sequence2.CountLinesIn(before.Seq2Range);
        int beforeSeq2Length = before.Seq2Range.Length;

        int afterLineCount1 = sequence1.CountLinesIn(after.Seq1Range);
        int afterSeq1Length = after.Seq1Range.Length;
        int afterLineCount2 = sequence2.CountLinesIn(after.Seq2Range);
        int afterSeq2Length = after.Seq2Range.Length;

        const int Max = 2 * 40 + 50;
        static double Cap(int v) => Math.Min(v, Max);

        return Math.Pow(Math.Pow(Cap(beforeLineCount1 * 40 + beforeSeq1Length), 1.5) + Math.Pow(Cap(beforeLineCount2 * 40 + beforeSeq2Length), 1.5), 1.5)
            + Math.Pow(Math.Pow(Cap(afterLineCount1 * 40 + afterSeq1Length), 1.5) + Math.Pow(Cap(afterLineCount2 * 40 + afterSeq2Length), 1.5), 1.5)
            > Math.Pow(Math.Pow(Max, 1.5), 1.5) * 1.3;
    }
}

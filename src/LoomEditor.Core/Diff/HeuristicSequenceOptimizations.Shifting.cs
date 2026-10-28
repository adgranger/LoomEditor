// Ported from microsoft/vscode: src/vs/editor/common/diff/defaultLinesDiffComputer/heuristicSequenceOptimizations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diff.Algorithms;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Diff;

/// <summary>Post-processing of a raw sequence diff: joining, shifting to better boundaries, removing short matches.</summary>
/// <remarks>Threading: stateless.</remarks>
internal static partial class HeuristicSequenceOptimizations
{
    /// <summary><c>optimizeSequenceDiffs</c>.</summary>
    public static IReadOnlyList<SequenceDiff> OptimizeSequenceDiffs(ISequence sequence1, ISequence sequence2, IReadOnlyList<SequenceDiff> sequenceDiffs)
    {
        IReadOnlyList<SequenceDiff> result = JoinSequenceDiffsByShifting(sequence1, sequence2, sequenceDiffs);
        // Sometimes, calling this function twice improves the result.
        result = JoinSequenceDiffsByShifting(sequence1, sequence2, result);
        return ShiftSequenceDiffs(sequence1, sequence2, result);
    }

    /// <summary><c>removeShortMatches</c>.</summary>
    public static IReadOnlyList<SequenceDiff> RemoveShortMatches(ISequence sequence1, ISequence sequence2, IReadOnlyList<SequenceDiff> sequenceDiffs)
    {
        List<SequenceDiff> result = [];
        foreach (SequenceDiff s in sequenceDiffs)
        {
            if (result.Count == 0)
            {
                result.Add(s);
                continue;
            }

            SequenceDiff last = result[^1];
            if (s.Seq1Range.Start - last.Seq1Range.EndExclusive <= 2 || s.Seq2Range.Start - last.Seq2Range.EndExclusive <= 2)
            {
                result[^1] = new SequenceDiff(last.Seq1Range.Join(s.Seq1Range), last.Seq2Range.Join(s.Seq2Range));
            }
            else
            {
                result.Add(s);
            }
        }

        return result;
    }

    // Fixes `{ Baz, Bar }` -> `{ Baz, Bar, Foo }` computed as [add "," after Bar] + [add "Foo " after space]
    // into the single diff [add ", Foo" after Bar].
    private static IReadOnlyList<SequenceDiff> JoinSequenceDiffsByShifting(ISequence sequence1, ISequence sequence2, IReadOnlyList<SequenceDiff> sequenceDiffs)
    {
        if (sequenceDiffs.Count == 0)
        {
            return sequenceDiffs;
        }

        List<SequenceDiff> result = [sequenceDiffs[0]];

        // First move them all to the left as much as possible and join them if possible
        for (int i = 1; i < sequenceDiffs.Count; i++)
        {
            SequenceDiff prevResult = result[^1];
            SequenceDiff cur = sequenceDiffs[i];

            if (cur.Seq1Range.IsEmpty || cur.Seq2Range.IsEmpty)
            {
                int length = cur.Seq1Range.Start - prevResult.Seq1Range.EndExclusive;
                int d;
                for (d = 1; d <= length; d++)
                {
                    if (sequence1.GetElement(cur.Seq1Range.Start - d) != sequence1.GetElement(cur.Seq1Range.EndExclusive - d) ||
                        sequence2.GetElement(cur.Seq2Range.Start - d) != sequence2.GetElement(cur.Seq2Range.EndExclusive - d))
                    {
                        break;
                    }
                }

                d--;

                if (d == length)
                {
                    // Merge previous and current diff
                    result[^1] = new SequenceDiff(
                        new OffsetRange(prevResult.Seq1Range.Start, cur.Seq1Range.EndExclusive - length),
                        new OffsetRange(prevResult.Seq2Range.Start, cur.Seq2Range.EndExclusive - length));
                    continue;
                }

                cur = cur.Delta(-d);
            }

            result.Add(cur);
        }

        List<SequenceDiff> result2 = [];
        // Then move them all to the right and join them again if possible
        for (int i = 0; i < result.Count - 1; i++)
        {
            SequenceDiff nextResult = result[i + 1];
            SequenceDiff cur = result[i];

            if (cur.Seq1Range.IsEmpty || cur.Seq2Range.IsEmpty)
            {
                int length = nextResult.Seq1Range.Start - cur.Seq1Range.EndExclusive;
                int d;
                for (d = 0; d < length; d++)
                {
                    if (!sequence1.IsStronglyEqual(cur.Seq1Range.Start + d, cur.Seq1Range.EndExclusive + d) ||
                        !sequence2.IsStronglyEqual(cur.Seq2Range.Start + d, cur.Seq2Range.EndExclusive + d))
                    {
                        break;
                    }
                }

                if (d == length)
                {
                    // Merge previous and current diff, write to result!
                    result[i + 1] = new SequenceDiff(
                        new OffsetRange(cur.Seq1Range.Start + length, nextResult.Seq1Range.EndExclusive),
                        new OffsetRange(cur.Seq2Range.Start + length, nextResult.Seq2Range.EndExclusive));
                    continue;
                }

                if (d > 0)
                {
                    cur = cur.Delta(d);
                }
            }

            result2.Add(cur);
        }

        if (result.Count > 0)
        {
            result2.Add(result[^1]);
        }

        return result2;
    }

    // Aligns character level diffs at whitespace characters:
    // `import { I[Arr, I]Bar }` -> `import { [IArr, ]IBar }`.
    private static SequenceDiff[] ShiftSequenceDiffs(ISequence sequence1, ISequence sequence2, IReadOnlyList<SequenceDiff> sequenceDiffs)
    {
        // Monaco returns early when a sequence has no getBoundaryScore; ours always have one (the default returns 0).
        SequenceDiff[] diffs = [.. sequenceDiffs];
        for (int i = 0; i < diffs.Length; i++)
        {
            SequenceDiff? prevDiff = i > 0 ? diffs[i - 1] : null;
            SequenceDiff diff = diffs[i];
            SequenceDiff? nextDiff = i + 1 < diffs.Length ? diffs[i + 1] : null;

            OffsetRange seq1ValidRange = new(
                prevDiff is not null ? prevDiff.Seq1Range.EndExclusive + 1 : 0,
                nextDiff is not null ? nextDiff.Seq1Range.Start - 1 : sequence1.Length);
            OffsetRange seq2ValidRange = new(
                prevDiff is not null ? prevDiff.Seq2Range.EndExclusive + 1 : 0,
                nextDiff is not null ? nextDiff.Seq2Range.Start - 1 : sequence2.Length);

            if (diff.Seq1Range.IsEmpty)
            {
                diffs[i] = ShiftDiffToBetterPosition(diff, sequence1, sequence2, seq1ValidRange, seq2ValidRange);
            }
            else if (diff.Seq2Range.IsEmpty)
            {
                diffs[i] = ShiftDiffToBetterPosition(diff.Swap(), sequence2, sequence1, seq2ValidRange, seq1ValidRange).Swap();
            }
        }

        return diffs;
    }

    private static SequenceDiff ShiftDiffToBetterPosition(SequenceDiff diff, ISequence sequence1, ISequence sequence2, OffsetRange seq1ValidRange, OffsetRange seq2ValidRange)
    {
        const int MaxShiftLimit = 100; // To prevent performance issues

        // don't touch previous or next!
        int deltaBefore = 1;
        while (diff.Seq1Range.Start - deltaBefore >= seq1ValidRange.Start &&
               diff.Seq2Range.Start - deltaBefore >= seq2ValidRange.Start &&
               sequence2.IsStronglyEqual(diff.Seq2Range.Start - deltaBefore, diff.Seq2Range.EndExclusive - deltaBefore) &&
               deltaBefore < MaxShiftLimit)
        {
            deltaBefore++;
        }

        deltaBefore--;

        int deltaAfter = 0;
        while (diff.Seq1Range.Start + deltaAfter < seq1ValidRange.EndExclusive &&
               diff.Seq2Range.EndExclusive + deltaAfter < seq2ValidRange.EndExclusive &&
               sequence2.IsStronglyEqual(diff.Seq2Range.Start + deltaAfter, diff.Seq2Range.EndExclusive + deltaAfter) &&
               deltaAfter < MaxShiftLimit)
        {
            deltaAfter++;
        }

        if (deltaBefore == 0 && deltaAfter == 0)
        {
            return diff;
        }

        int bestDelta = 0;
        int bestScore = -1;
        for (int delta = -deltaBefore; delta <= deltaAfter; delta++)
        {
            int seq2OffsetStart = diff.Seq2Range.Start + delta;
            int seq2OffsetEndExclusive = diff.Seq2Range.EndExclusive + delta;
            int seq1Offset = diff.Seq1Range.Start + delta;

            int score = sequence1.GetBoundaryScore(seq1Offset) + sequence2.GetBoundaryScore(seq2OffsetStart) + sequence2.GetBoundaryScore(seq2OffsetEndExclusive);
            if (score > bestScore)
            {
                bestScore = score;
                bestDelta = delta;
            }
        }

        return diff.Delta(bestDelta);
    }
}

// Ported from microsoft/vscode: src/vs/editor/common/diff/defaultLinesDiffComputer/algorithms/dynamicProgrammingDiffing.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Diff.Algorithms;

/// <summary>
/// An O(MN) diffing algorithm that supports a score function (Monaco: <c>DynamicProgrammingDiffing</c>).
/// The algorithm can be improved by processing the 2d array diagonally.
/// </summary>
/// <remarks>Threading: stateless; safe to call from any thread, one call per instance at a time is not required.</remarks>
internal sealed class DynamicProgrammingDiffing : IDiffAlgorithm
{
    /// <inheritdoc />
    public DiffAlgorithmResult Compute(ISequence sequence1, ISequence sequence2, ITimeout? timeout = null) =>
        Compute(sequence1, sequence2, timeout, null);

    /// <summary>Computes the diffs; <paramref name="equalityScore"/> scores a match of two offsets (default 1).</summary>
    public static DiffAlgorithmResult Compute(ISequence sequence1, ISequence sequence2, ITimeout? timeout, Func<int, int, double>? equalityScore)
    {
        timeout ??= InfiniteTimeout.Instance;
        if (sequence1.Length == 0 || sequence2.Length == 0)
        {
            return DiffAlgorithmResult.Trivial(sequence1, sequence2);
        }

        // lcsLengths.Get(i, j): Length of the longest common subsequence of sequence1[0..i] and sequence2[0..j].
        var lcsLengths = new Array2D<double>(sequence1.Length, sequence2.Length);
        var directions = new Array2D<int>(sequence1.Length, sequence2.Length);
        var lengths = new Array2D<int>(sequence1.Length, sequence2.Length);

        // ==== Initializing lcsLengths ====
        for (int s1 = 0; s1 < sequence1.Length; s1++)
        {
            for (int s2 = 0; s2 < sequence2.Length; s2++)
            {
                if (!timeout.IsValid())
                {
                    return DiffAlgorithmResult.TrivialTimedOut(sequence1, sequence2);
                }

                double horizontalLen = s1 == 0 ? 0 : lcsLengths.Get(s1 - 1, s2);
                double verticalLen = s2 == 0 ? 0 : lcsLengths.Get(s1, s2 - 1);

                double extendedSeqScore;
                if (sequence1.GetElement(s1) == sequence2.GetElement(s2))
                {
                    extendedSeqScore = s1 == 0 || s2 == 0 ? 0 : lcsLengths.Get(s1 - 1, s2 - 1);
                    if (s1 > 0 && s2 > 0 && directions.Get(s1 - 1, s2 - 1) == 3)
                    {
                        // Prefer consecutive diagonals
                        extendedSeqScore += lengths.Get(s1 - 1, s2 - 1);
                    }

                    extendedSeqScore += equalityScore is not null ? equalityScore(s1, s2) : 1;
                }
                else
                {
                    extendedSeqScore = -1;
                }

                double newValue = Math.Max(Math.Max(horizontalLen, verticalLen), extendedSeqScore);

                if (newValue == extendedSeqScore)
                {
                    // Prefer diagonals
                    int prevLen = s1 > 0 && s2 > 0 ? lengths.Get(s1 - 1, s2 - 1) : 0;
                    lengths.Set(s1, s2, prevLen + 1);
                    directions.Set(s1, s2, 3);
                }
                else if (newValue == horizontalLen)
                {
                    lengths.Set(s1, s2, 0);
                    directions.Set(s1, s2, 1);
                }
                else if (newValue == verticalLen)
                {
                    lengths.Set(s1, s2, 0);
                    directions.Set(s1, s2, 2);
                }

                lcsLengths.Set(s1, s2, newValue);
            }
        }

        // ==== Backtracking ====
        var result = new List<SequenceDiff>();
        int lastAligningPosS1 = sequence1.Length;
        int lastAligningPosS2 = sequence2.Length;

        void ReportDecreasingAligningPositions(int s1, int s2)
        {
            if (s1 + 1 != lastAligningPosS1 || s2 + 1 != lastAligningPosS2)
            {
                result.Add(new SequenceDiff(
                    new OffsetRange(s1 + 1, lastAligningPosS1),
                    new OffsetRange(s2 + 1, lastAligningPosS2)));
            }

            lastAligningPosS1 = s1;
            lastAligningPosS2 = s2;
        }

        int b1 = sequence1.Length - 1;
        int b2 = sequence2.Length - 1;
        while (b1 >= 0 && b2 >= 0)
        {
            if (directions.Get(b1, b2) == 3)
            {
                ReportDecreasingAligningPositions(b1, b2);
                b1--;
                b2--;
            }
            else if (directions.Get(b1, b2) == 1)
            {
                b1--;
            }
            else
            {
                b2--;
            }
        }

        ReportDecreasingAligningPositions(-1, -1);
        result.Reverse();
        return new DiffAlgorithmResult([.. result], false);
    }
}

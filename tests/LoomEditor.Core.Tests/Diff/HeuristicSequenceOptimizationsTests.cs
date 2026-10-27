using LoomEditor.Core.Diff;
using LoomEditor.Core.Diff.Algorithms;
using LoomEditor.Core.Tests.Diff.TestHelpers;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Diff;

public class HeuristicSequenceOptimizationsTests
{
    private static SequenceDiff Diff(int s1, int e1, int s2, int e2) => new(new OffsetRange(s1, e1), new OffsetRange(s2, e2));

    [Fact]
    public void Optimize_JoinsDiffsSeparatedByEqualElements()
    {
        // "a b" -> "a, c b": [add ","] and [add "c "] around the shared space become one diff.
        var seq1 = new ArraySequence("a b");
        var seq2 = new ArraySequence("a, c b");
        IReadOnlyList<SequenceDiff> result = HeuristicSequenceOptimizations.OptimizeSequenceDiffs(
            seq1, seq2, [Diff(1, 1, 1, 2), Diff(2, 2, 3, 5)]);

        SequenceDiff single = Assert.Single(result);
        Assert.Equal("[1, 1) <-> [1, 4)", single.ToString());
    }

    [Fact]
    public void Optimize_ShiftsInsertionToBoundary()
    {
        var seq1 = new ScoredSequence("xxxx", offset => offset == 3 ? 10 : 0, strong: true);
        var seq2 = new ScoredSequence("xxxxx", _ => 0, strong: true);
        IReadOnlyList<SequenceDiff> result = HeuristicSequenceOptimizations.OptimizeSequenceDiffs(seq1, seq2, [Diff(1, 1, 1, 2)]);

        Assert.Equal("[3, 3) <-> [3, 4)", Assert.Single(result).ToString());
    }

    [Fact]
    public void Optimize_NoDiffs_Empty()
    {
        var seq = new ArraySequence("abc");
        Assert.Empty(HeuristicSequenceOptimizations.OptimizeSequenceDiffs(seq, seq, []));
    }

    [Fact]
    public void Optimize_ShiftIsBoundedByStronglyEqual()
    {
        var seq1 = new ScoredSequence("xxxx", offset => offset == 3 ? 10 : 0, strong: false);
        var seq2 = new ScoredSequence("xxxxx", _ => 0, strong: false);
        IReadOnlyList<SequenceDiff> result = HeuristicSequenceOptimizations.OptimizeSequenceDiffs(seq1, seq2, [Diff(1, 1, 1, 2)]);

        Assert.Equal("[1, 1) <-> [1, 2)", Assert.Single(result).ToString());
    }

    [Fact]
    public void RemoveShortMatches_MergesDiffsWithTinyEqualGap()
    {
        var seq = new ArraySequence(new string('a', 20));

        // Gap of 2 in the first sequence.
        Assert.Equal("[0, 4) <-> [0, 11)", Assert.Single(HeuristicSequenceOptimizations.RemoveShortMatches(
            seq, seq, [Diff(0, 1, 0, 1), Diff(3, 4, 10, 11)])).ToString());

        // Gap of 2 in the second sequence only.
        Assert.Equal("[0, 11) <-> [0, 4)", Assert.Single(HeuristicSequenceOptimizations.RemoveShortMatches(
            seq, seq, [Diff(0, 1, 0, 1), Diff(10, 11, 3, 4)])).ToString());
    }

    [Fact]
    public void RemoveShortMatches_KeepsLongGap()
    {
        var seq = new ArraySequence(new string('a', 20));
        IReadOnlyList<SequenceDiff> result = HeuristicSequenceOptimizations.RemoveShortMatches(
            seq, seq, [Diff(0, 1, 0, 1), Diff(5, 6, 5, 6)]);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Optimize_Result_IsSortedAndNonOverlapping()
    {
        var algorithm = new MyersDiffAlgorithm();
        for (int seed = 0; seed < 200; seed++)
        {
            var random = new Random(seed);
            var seq1 = new ArraySequence(RandomText(random));
            var seq2 = new ArraySequence(RandomText(random));
            IReadOnlyList<SequenceDiff> result = HeuristicSequenceOptimizations.OptimizeSequenceDiffs(seq1, seq2, algorithm.Compute(seq1, seq2).Diffs);

            for (int i = 0; i < result.Count; i++)
            {
                Assert.True(result[i].Seq1Range.EndExclusive <= seq1.Length && result[i].Seq2Range.EndExclusive <= seq2.Length, $"seed {seed}: out of bounds");
                if (i > 0)
                {
                    Assert.True(
                        result[i - 1].Seq1Range.EndExclusive <= result[i].Seq1Range.Start && result[i - 1].Seq2Range.EndExclusive <= result[i].Seq2Range.Start,
                        $"seed {seed}: diffs {i - 1} and {i} are unsorted or overlap");
                }
            }
        }
    }

    private static string RandomText(Random random)
    {
        char[] chars = new char[random.Next(0, 30)];
        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = "ab ,"[random.Next(4)];
        }

        return new string(chars);
    }

    private sealed class ScoredSequence(string text, Func<int, int> score, bool strong) : ISequence
    {
        public int Length => text.Length;

        public int GetElement(int offset) => text[offset];

        public int GetBoundaryScore(int length) => score(length);

        public bool IsStronglyEqual(int offset1, int offset2) => strong && text[offset1] == text[offset2];
    }
}

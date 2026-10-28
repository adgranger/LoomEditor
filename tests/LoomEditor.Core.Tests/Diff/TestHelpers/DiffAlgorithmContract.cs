using LoomEditor.Core.Diff.Algorithms;

namespace LoomEditor.Core.Tests.Diff.TestHelpers;

/// <summary>Checks shared by every <see cref="IDiffAlgorithm"/>.</summary>
internal static class DiffAlgorithmContract
{
    public static string Diff(IDiffAlgorithm algorithm, string a, string b) =>
        string.Join(", ", algorithm.Compute(new ArraySequence(a), new ArraySequence(b)).Diffs.Select(d => d.ToString()));

    public static void Table(IDiffAlgorithm algorithm)
    {
        Assert.Equal(string.Empty, Diff(algorithm, "abc", "abc"));
        Assert.Equal("[0, 3) <-> [0, 0)", Diff(algorithm, "abc", string.Empty));
        Assert.Equal("[0, 0) <-> [0, 3)", Diff(algorithm, string.Empty, "abc"));
        Assert.Equal(string.Empty, Diff(algorithm, string.Empty, string.Empty).Replace("[0, 0) <-> [0, 0)", string.Empty));
        Assert.Equal("[2, 2) <-> [2, 3)", Diff(algorithm, "abde", "abcde"));
        Assert.Equal("[0, 1) <-> [0, 0)", Diff(algorithm, "xabc", "abc"));
        Assert.Equal("[1, 2) <-> [1, 2)", Diff(algorithm, "abc", "axc"));
    }

    public static void SortedAndApplicable(IDiffAlgorithm algorithm)
    {
        for (int seed = 0; seed < 200; seed++)
        {
            var random = new Random(seed);
            string a = RandomString(random);
            string b = RandomString(random);
            SequenceDiff[] diffs = algorithm.Compute(new ArraySequence(a), new ArraySequence(b)).Diffs;

            var rebuilt = new System.Text.StringBuilder();
            int pos1 = 0;
            foreach (SequenceDiff diff in diffs)
            {
                Assert.True(diff.Seq1Range.Start >= pos1, $"seed {seed}: overlapping diffs");
                rebuilt.Append(a, pos1, diff.Seq1Range.Start - pos1);
                rebuilt.Append(b, diff.Seq2Range.Start, diff.Seq2Range.Length);
                pos1 = diff.Seq1Range.EndExclusive;
            }

            rebuilt.Append(a, pos1, a.Length - pos1);
            Assert.True(b == rebuilt.ToString(), $"seed {seed}: '{a}' -> '{b}' gave '{rebuilt}'");
        }
    }

    public static void TimeoutInvalid(IDiffAlgorithm algorithm)
    {
        DiffAlgorithmResult result = algorithm.Compute(new ArraySequence("abcdef"), new ArraySequence("fedcba"), new FakeTimeout(0));
        Assert.True(result.HitTimeout);
    }

    private static string RandomString(Random random) =>
        new([.. Enumerable.Range(0, random.Next(0, 12)).Select(_ => (char)('a' + random.Next(4)))]);
}

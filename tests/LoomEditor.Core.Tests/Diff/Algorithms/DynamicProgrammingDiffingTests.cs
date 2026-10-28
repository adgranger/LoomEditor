using LoomEditor.Core.Diff.Algorithms;
using LoomEditor.Core.Tests.Diff.TestHelpers;

namespace LoomEditor.Core.Tests.Diff.Algorithms;

public class DynamicProgrammingDiffingTests
{
    private readonly DynamicProgrammingDiffing _algorithm = new();

    [Fact]
    public void Compute_Table_GivesExpectedDiffs() => DiffAlgorithmContract.Table(_algorithm);

    [Fact]
    public void Compute_ResultsAreSortedAndNonOverlapping() => DiffAlgorithmContract.SortedAndApplicable(_algorithm);

    [Fact]
    public void Compute_TimeoutInvalid_HitTimeoutTrue() => DiffAlgorithmContract.TimeoutInvalid(_algorithm);

    [Fact]
    public void DynamicProgramming_EqualityScore_PrefersHigherScore()
    {
        // "a" can match offset 0 or 2 of "aba"; the score decides which one.
        var seq1 = new ArraySequence("a");
        var seq2 = new ArraySequence("aba");

        string Run(Func<int, int, double> score) =>
            string.Join(", ", DynamicProgrammingDiffing.Compute(seq1, seq2, null, score).Diffs.Select(d => d.ToString()));

        Assert.Equal("[1, 1) <-> [1, 3)", Run((_, s2) => s2 == 0 ? 10 : 1));
        Assert.Equal("[0, 0) <-> [0, 2)", Run((_, s2) => s2 == 2 ? 10 : 1));
    }

    [Fact]
    public void Array2D_GetSet()
    {
        var array = new Array2D<int>(3, 2);
        array.Set(2, 1, 7);
        array.Set(0, 0, 4);
        Assert.Equal(7, array.Get(2, 1));
        Assert.Equal(4, array.Get(0, 0));
        Assert.Equal(0, array.Get(1, 1));
    }
}

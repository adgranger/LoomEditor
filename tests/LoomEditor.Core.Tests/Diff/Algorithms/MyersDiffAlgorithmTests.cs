using LoomEditor.Core.Diff;
using LoomEditor.Core.Diff.Algorithms;
using LoomEditor.Core.Tests.Diff.TestHelpers;

namespace LoomEditor.Core.Tests.Diff.Algorithms;

public class MyersDiffAlgorithmTests
{
    private readonly MyersDiffAlgorithm _algorithm = new();

    // Monaco: 'myers' > '1'
    [Fact]
    public void Compute_HelloWorldToHalloWelt_DoesNotThrow()
    {
        var seq1 = new LinesSliceCharSequence(["hello world"], new Range(1, 1, 1, int.MaxValue), true);
        var seq2 = new LinesSliceCharSequence(["hallo welt"], new Range(1, 1, 1, int.MaxValue), true);
        DiffAlgorithmResult result = _algorithm.Compute(seq1, seq2);
        Assert.False(result.HitTimeout);
    }

    [Fact]
    public void Compute_Table_GivesExpectedDiffs() => DiffAlgorithmContract.Table(_algorithm);

    [Fact]
    public void Compute_ResultsAreSortedAndNonOverlapping() => DiffAlgorithmContract.SortedAndApplicable(_algorithm);

    [Fact]
    public void Compute_TimeoutInvalid_HitTimeoutTrue() => DiffAlgorithmContract.TimeoutInvalid(_algorithm);
}

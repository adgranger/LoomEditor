using LoomEditor.Core.Diff.Algorithms;
using LoomEditor.Core.Tests.Diff.TestHelpers;

namespace LoomEditor.Core.Tests.Diff.Algorithms;

public class MyersDiffAlgorithmTests
{
    private readonly MyersDiffAlgorithm _algorithm = new();

    // Monaco: 'myers' > '1' (re-added with LinesSliceCharSequence in P9-04)
    [Fact]
    public void Compute_HelloWorldToHalloWelt_DoesNotThrow()
    {
        DiffAlgorithmResult result = _algorithm.Compute(new ArraySequence("hello world"), new ArraySequence("hallo welt"));
        Assert.False(result.HitTimeout);
    }

    [Fact]
    public void Compute_Table_GivesExpectedDiffs() => DiffAlgorithmContract.Table(_algorithm);

    [Fact]
    public void Compute_ResultsAreSortedAndNonOverlapping() => DiffAlgorithmContract.SortedAndApplicable(_algorithm);

    [Fact]
    public void Compute_TimeoutInvalid_HitTimeoutTrue() => DiffAlgorithmContract.TimeoutInvalid(_algorithm);
}

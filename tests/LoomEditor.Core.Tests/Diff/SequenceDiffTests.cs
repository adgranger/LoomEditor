using LoomEditor.Core.Diff.Algorithms;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Diff;

public class SequenceDiffTests
{
    private static SequenceDiff D(int s1, int e1, int s2, int e2) => new(new OffsetRange(s1, e1), new OffsetRange(s2, e2));

    // Monaco: SequenceDiff.join
    [Fact]
    public void Join_TwoDiffs_SmallestEnclosingDiff()
    {
        Assert.Equal("[1, 9) <-> [2, 12)", D(1, 3, 2, 4).Join(D(5, 9, 10, 12)).ToString());
    }

    // Monaco: SequenceDiff.intersect
    [Fact]
    public void Intersect_Overlapping_ReturnsIntersection()
    {
        Assert.Equal("[2, 3) <-> [3, 4)", D(1, 3, 2, 4).Intersect(D(2, 9, 3, 12))!.ToString());
    }

    [Fact]
    public void Intersect_Disjoint_ReturnsNull()
    {
        Assert.Null(D(1, 3, 2, 4).Intersect(D(5, 9, 3, 12)));
    }

    // Monaco: SequenceDiff.intersectsOrTouches
    [Fact]
    public void IntersectsOrTouches_Touching_True()
    {
        Assert.True(D(1, 3, 10, 11).IntersectsOrTouches(D(3, 5, 20, 21)));
        Assert.False(D(1, 3, 10, 11).IntersectsOrTouches(D(4, 5, 20, 21)));
    }

    // Monaco: SequenceDiff.delta / deltaStart / deltaEnd / swap
    [Fact]
    public void Delta_DeltaStart_DeltaEnd_Swap_MoveTheRanges()
    {
        SequenceDiff d = D(1, 3, 5, 9);
        Assert.Same(d, d.Delta(0));
        Assert.Equal("[3, 5) <-> [7, 11)", d.Delta(2).ToString());
        Assert.Equal("[2, 3) <-> [6, 9)", d.DeltaStart(1).ToString());
        Assert.Equal("[1, 4) <-> [5, 10)", d.DeltaEnd(1).ToString());
        Assert.Equal("[5, 9) <-> [1, 3)", d.Swap().ToString());
    }

    // Monaco: SequenceDiff.invert
    [Fact]
    public void Invert_TwoDiffs_ThreeUnchangedRanges()
    {
        SequenceDiff[] inverted = SequenceDiff.Invert([D(2, 4, 2, 3), D(6, 7, 5, 8)], 10);
        Assert.Equal(["[0, 2) <-> [0, 2)", "[4, 6) <-> [3, 5)", "[7, 10) <-> [8, 11)"], inverted.Select(x => x.ToString()));
    }

    [Fact]
    public void Invert_NoDiffs_WholeDocumentIsUnchanged()
    {
        SequenceDiff[] inverted = SequenceDiff.Invert([], 4);
        Assert.Equal(["[0, 4) <-> [0, 4)"], inverted.Select(x => x.ToString()));
    }

    // Monaco: SequenceDiff.fromOffsetPairs
    [Fact]
    public void FromOffsetPairs_StartAndEnd_BuildsBothRanges()
    {
        Assert.Equal("[1, 4) <-> [2, 8)", SequenceDiff.FromOffsetPairs(new OffsetPair(1, 2), new OffsetPair(4, 8)).ToString());
    }

    // Monaco: SequenceDiff.assertSorted
    [Fact]
    public void AssertSorted_Sorted_DoesNotThrow()
    {
        SequenceDiff.AssertSorted([D(0, 1, 0, 1), D(1, 2, 3, 4)]);
    }

    // Monaco: SequenceDiff.getStarts / getEndExclusives
    [Fact]
    public void GetStarts_GetEndExclusives_ReturnOffsetPairs()
    {
        SequenceDiff d = D(1, 3, 5, 9);
        Assert.Equal(new OffsetPair(1, 5), d.GetStarts());
        Assert.Equal(new OffsetPair(3, 9), d.GetEndExclusives());
    }

    // Monaco: OffsetPair.delta / equals / toString
    [Fact]
    public void OffsetPair_Delta_Equals()
    {
        OffsetPair p = new(1, 2);
        Assert.Same(p, p.Delta(0));
        Assert.Equal(new OffsetPair(4, 5), p.Delta(3));
        Assert.NotEqual(p, new OffsetPair(1, 3));
        Assert.Equal("1 <-> 2", p.ToString());
        Assert.Equal(new OffsetPair(0, 0), OffsetPair.Zero);
    }

    // Monaco: DiffAlgorithmResult.trivial / trivialTimedOut
    [Fact]
    public void Trivial_TwoSequences_OneDiffCoveringBoth()
    {
        LoomEditor.Core.Diff.LineSequence a = new([1, 2, 3], ["a", "b", "c"]);
        LoomEditor.Core.Diff.LineSequence b = new([1], ["a"]);
        DiffAlgorithmResult r = DiffAlgorithmResult.Trivial(a, b);
        Assert.False(r.HitTimeout);
        Assert.Equal("[0, 3) <-> [0, 1)", Assert.Single(r.Diffs).ToString());
        Assert.True(DiffAlgorithmResult.TrivialTimedOut(a, b).HitTimeout);
    }
}

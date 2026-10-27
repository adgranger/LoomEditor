using LoomEditor.Core.Diff;
using LoomEditor.Core.Text;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.View.Tests.DiffEditor;

public class UnchangedRegionTests
{
    private static DetailedLineRangeMapping Change(int start, int end) => new(new LineRange(start, end), new LineRange(start, end), null);

    // Original 10..39, modified 20..49.
    private static UnchangedRegion NewRegion(int top = 0, int bottom = 0) => new(10, 20, 30, top, bottom);

    private static void AssertRegion(UnchangedRegion r, int original, int modified, int count)
    {
        Assert.Equal((original, modified, count), (r.OriginalLineNumber, r.ModifiedLineNumber, r.LineCount));
    }

    [Fact]
    public void FromDiffs_NoChanges_WholeFileOneRegionHidden()
    {
        var regions = UnchangedRegion.FromDiffs([], 30, 30, 3, 3);

        var r = Assert.Single(regions);
        AssertRegion(r, 1, 1, 30);
    }

    [Fact]
    public void FromDiffs_ChangeInTheMiddle_TwoRegionsWithContext()
    {
        var regions = UnchangedRegion.FromDiffs([Change(15, 16)], 30, 30, 3, 3);

        Assert.Equal(2, regions.Count);
        AssertRegion(regions[0], 1, 1, 11);
        AssertRegion(regions[1], 19, 19, 12);
    }

    [Fact]
    public void FromDiffs_SmallGap_NoRegion()
    {
        // The gap [2, 5) has 3 lines, less than 2 * 1 + 3.
        var regions = UnchangedRegion.FromDiffs([Change(1, 2), Change(5, 6)], 30, 30, 3, 1);

        var r = Assert.Single(regions);
        AssertRegion(r, 7, 7, 24);
    }

    [Fact]
    public void FromDiffs_AtStartAtEnd_OneSidedContext()
    {
        var regions = UnchangedRegion.FromDiffs([Change(10, 11)], 20, 20, 2, 2);

        Assert.Equal(2, regions.Count);
        AssertRegion(regions[0], 1, 1, 7);
        AssertRegion(regions[1], 13, 13, 8);
    }

    [Fact]
    public void SetState_ClampsVisibleCounts()
    {
        var r = NewRegion();

        r.SetState(100, 100);

        Assert.Equal((30, 0), (r.VisibleLineCountTop, r.VisibleLineCountBottom));
    }

    [Fact]
    public void Constructor_InRangeCounts_AreKept()
    {
        var r = NewRegion(4, 6);

        Assert.Equal((4, 6), (r.VisibleLineCountTop, r.VisibleLineCountBottom));
    }

    [Fact]
    public void GetHiddenRanges_AfterShowMoreAbove()
    {
        var r = NewRegion();

        r.ShowMoreAbove(5);

        Assert.Equal(new LineRange(15, 40), r.GetHiddenOriginalRange());
        Assert.Equal(new LineRange(25, 50), r.GetHiddenModifiedRange());
    }

    [Fact]
    public void ShowMoreAbove_Capped()
    {
        var r = NewRegion(0, 5);

        r.ShowMoreAbove(100);

        Assert.Equal(25, r.VisibleLineCountTop);
    }

    [Fact]
    public void ShowMoreBelow_Capped()
    {
        var r = NewRegion(5, 0);

        r.ShowMoreBelow(100);

        Assert.Equal(25, r.VisibleLineCountBottom);
    }

    [Fact]
    public void ShowAll_NoHiddenLines_ControlsHidden()
    {
        var r = NewRegion(4, 0);

        r.ShowAll();

        Assert.Equal(26, r.VisibleLineCountBottom);
        Assert.True(r.GetHiddenModifiedRange().IsEmpty);
        Assert.True(r.ShouldHideControls);
    }

    [Fact]
    public void CollapseAll_ResetsCounts()
    {
        var r = NewRegion(5, 5);

        r.CollapseAll();

        Assert.Equal((0, 0), (r.VisibleLineCountTop, r.VisibleLineCountBottom));
        Assert.False(r.ShouldHideControls);
    }

    [Fact]
    public void ShowModifiedLine_FromCloserSide_Top()
    {
        var r = NewRegion();

        r.ShowModifiedLine(22, RevealPreference.FromCloserSide);

        Assert.Equal((3, 0), (r.VisibleLineCountTop, r.VisibleLineCountBottom));
    }

    [Fact]
    public void ShowModifiedLine_FromBottom()
    {
        var r = NewRegion();

        r.ShowModifiedLine(22, RevealPreference.FromBottom);

        Assert.Equal((0, 28), (r.VisibleLineCountTop, r.VisibleLineCountBottom));
    }

    [Fact]
    public void ShowOriginalLine_FromTop()
    {
        var r = NewRegion();

        r.ShowOriginalLine(13, RevealPreference.FromTop);

        Assert.Equal((24, 0), (r.VisibleLineCountTop, r.VisibleLineCountBottom));
    }

    [Fact]
    public void ShowOriginalLine_FromCloserSide_Bottom()
    {
        var r = NewRegion();

        r.ShowOriginalLine(35, RevealPreference.FromCloserSide);

        Assert.Equal((0, 20), (r.VisibleLineCountTop, r.VisibleLineCountBottom));
    }

    [Fact]
    public void SetHiddenModifiedRange_ShowsTheRest()
    {
        var r = NewRegion();

        r.SetHiddenModifiedRange(new LineRange(25, 40));

        Assert.Equal((5, 10), (r.VisibleLineCountTop, r.VisibleLineCountBottom));
    }

    [Fact]
    public void SetHiddenModifiedRange_OutsideRegion_Clamped()
    {
        var r = NewRegion();

        r.SetHiddenModifiedRange(new LineRange(0, 100));

        Assert.Equal((0, 0), (r.VisibleLineCountTop, r.VisibleLineCountBottom));
    }

    [Fact]
    public void SetVisibleRanges_SplitsInTwoRegions()
    {
        var r = NewRegion();

        var result = r.SetVisibleRanges([new LineRangeMapping(new LineRange(30, 35), new LineRange(30, 35))]);

        Assert.Equal(2, result.Count);
        AssertRegion(result[0], 10, 20, 10);
        Assert.Equal((0, 0), (result[0].VisibleLineCountTop, result[0].VisibleLineCountBottom));
        AssertRegion(result[1], 20, 30, 20);
        Assert.Equal((5, 0), (result[1].VisibleLineCountTop, result[1].VisibleLineCountBottom));
    }

    [Fact]
    public void SetVisibleRanges_AllVisible_ShowsAll()
    {
        var r = NewRegion();

        var result = r.SetVisibleRanges([new LineRangeMapping(new LineRange(20, 50), new LineRange(20, 50))]);

        Assert.Same(r, Assert.Single(result));
        Assert.Equal(30, r.VisibleLineCountBottom);
    }

    [Fact]
    public void Changed_RaisedOncePerMutation()
    {
        var r = NewRegion();
        var count = 0;
        r.Changed += (_, _) => count++;

        r.SetState(5, 5);
        r.IsDragged = UnchangedRegionDragSide.Top;

        Assert.Equal(2, count);
    }

    [Fact]
    public void Changed_NotRaisedWhenNothingChanges()
    {
        var r = NewRegion(5, 5);
        var count = 0;
        r.Changed += (_, _) => count++;

        r.SetState(5, 5);
        r.ShowMoreAbove(0);
        r.IsDragged = null;

        Assert.Equal(0, count);
    }
}

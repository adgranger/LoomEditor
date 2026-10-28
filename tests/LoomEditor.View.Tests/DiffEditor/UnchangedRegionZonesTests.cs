using LoomEditor.View.DiffEditor;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.DiffEditor;

// Monaco has no test for hideUnchangedRegionsFeature (it needs a DOM): all tests are new.
public class UnchangedRegionZonesTests
{
    // Original 10..39, modified 20..49 (a region in the middle of a 100 line file).
    private static UnchangedRegion Region(int top = 0, int bottom = 0) => new(10, 20, 30, top, bottom);

    [Fact]
    public void Compute_NoRegions_Empty()
    {
        UnchangedRegionLayout layout = UnchangedRegionZones.Compute([], 100);

        Assert.Empty(layout.OriginalZones);
        Assert.Empty(layout.ModifiedZones);
        Assert.Empty(layout.OriginalHiddenAreas);
        Assert.Empty(layout.ModifiedDecorations);
    }

    [Fact]
    public void Compute_OneRegion_ZonesAfterLineBeforeHiddenRange()
    {
        UnchangedRegionLayout layout = UnchangedRegionZones.Compute([Region()], 100);

        ZoneSpec original = Assert.Single(layout.OriginalZones);
        ZoneSpec modified = Assert.Single(layout.ModifiedZones);
        Assert.Equal(9, original.AfterLineNumber);
        Assert.Equal(19, modified.AfterLineNumber);
        Assert.Equal(ViewZoneKind.Collapsed, modified.Kind);
        Assert.True(modified.SuppressMouseDown);
        Assert.True(((CollapsedZoneData)original.Data!).IsOriginalSide);
        Assert.False(((CollapsedZoneData)modified.Data!).IsOriginalSide);
    }

    [Fact]
    public void Compute_RegionWithTopVisibleLines_ZoneAfterTheVisibleOnes()
    {
        UnchangedRegionLayout layout = UnchangedRegionZones.Compute([Region(top: 5, bottom: 2)], 100);

        Assert.Equal(24, Assert.Single(layout.ModifiedZones).AfterLineNumber);
        Assert.Equal(14, Assert.Single(layout.OriginalZones).AfterLineNumber);
    }

    [Fact]
    public void Compute_ShouldHideControls_NoZoneButDecoration()
    {
        UnchangedRegionLayout layout = UnchangedRegionZones.Compute([Region(top: 30)], 100);

        Assert.Empty(layout.OriginalZones);
        Assert.Empty(layout.ModifiedZones);
        Assert.Empty(layout.ModifiedHiddenAreas);
        Assert.Single(layout.ModifiedDecorations);
    }

    [Fact]
    public void Compute_SingleRegionCoveringWholeFile_Empty()
    {
        UnchangedRegionLayout layout = UnchangedRegionZones.Compute([new UnchangedRegion(1, 1, 30, 0, 0)], 30);

        Assert.Empty(layout.ModifiedZones);
        Assert.Empty(layout.ModifiedHiddenAreas);
        Assert.Empty(layout.OriginalDecorations);
    }

    [Fact]
    public void Compute_HiddenAreas_AreTheHiddenRanges()
    {
        UnchangedRegionLayout layout = UnchangedRegionZones.Compute([Region(top: 3, bottom: 4)], 100);

        Assert.Equal((13, 35), (layout.OriginalHiddenAreas[0].StartLineNumber, layout.OriginalHiddenAreas[0].EndLineNumber));
        Assert.Equal((23, 45), (layout.ModifiedHiddenAreas[0].StartLineNumber, layout.ModifiedHiddenAreas[0].EndLineNumber));
    }

    [Fact]
    public void Compute_Decorations_CoverWholeUnchangedRange()
    {
        UnchangedRegionLayout layout = UnchangedRegionZones.Compute([Region(top: 3)], 100);

        var decoration = Assert.Single(layout.ModifiedDecorations);
        Assert.Equal((20, 49), (decoration.Range.StartLineNumber, decoration.Range.EndLineNumber));
        Assert.True(decoration.Options.IsWholeLine);
        Assert.Equal("diff-unchanged-lines", decoration.Options.ClassName);
    }

    [Fact]
    public void Compute_BarHeightIs24() =>
        Assert.Equal(24, Assert.Single(UnchangedRegionZones.Compute([Region()], 100).ModifiedZones).HeightInPx);
}

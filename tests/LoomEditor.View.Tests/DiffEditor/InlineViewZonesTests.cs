using LoomEditor.Core.Diff;
using LoomEditor.Core.Text;
using LoomEditor.View.DiffEditor;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.DiffEditor;

// Monaco has no unit test for the zones of the inline view (DOM): all tests are new.
public class InlineViewZonesTests
{
    private const int Lh = 10;

    private static DiffMapping Diff(int os, int oe, int ms, int me, params Range[] originalInner) =>
        new(new DetailedLineRangeMapping(
            new LineRange(os, oe),
            new LineRange(ms, me),
            originalInner.Length == 0 ? null : [.. originalInner.Select(r => new RangeMapping(r, new Range(ms, 1, ms, 1)))]));

    private static IReadOnlyList<ZoneSpec> Compute(DiffMapping[] diffs, AdditionalLineHeight[]? originalExtra = null, AdditionalLineHeight[]? modifiedExtra = null) =>
        InlineViewZones.Compute(
            LineRangeAlignments.Compute(diffs, originalExtra ?? [], modifiedExtra ?? [], Lh, Lh, _ => 10, 100, innerHunkAlignment: false),
            Lh,
            renderIndicators: true,
            _ => throw new InvalidOperationException());

    [Fact]
    public void Compute_NoAlignments_Empty() => Assert.Empty(Compute([]));

    [Fact]
    public void Compute_Deletion_OneDeletedLinesZoneBeforeFollowingLine()
    {
        ZoneSpec zone = Assert.Single(Compute([Diff(3, 5, 3, 3)]));

        Assert.Equal(ViewZoneKind.DeletedLines, zone.Kind);
        Assert.Equal(2, zone.AfterLineNumber);
        Assert.Equal(2 * Lh, zone.HeightInPx);
        Assert.Equal(new LineRange(3, 5), ((DeletedLinesZoneData)zone.Data!).OriginalLines);
        Assert.True(((DeletedLinesZoneData)zone.Data!).RenderIndicators);
    }

    [Fact]
    public void Compute_Replacement_DeletedZoneAboveModifiedRange()
    {
        ZoneSpec zone = Assert.Single(Compute([Diff(2, 4, 5, 8)]));

        Assert.Equal(4, zone.AfterLineNumber);
        Assert.Equal(2 * Lh, zone.HeightInPx);
    }

    [Fact]
    public void Compute_Insertion_NoZone() => Assert.Empty(Compute([Diff(3, 3, 3, 5)]));

    [Fact]
    public void Compute_InnerChanges_BecomeDeletedRangesClippedToTheBlock()
    {
        // The second inner range ends at column 1 of the line after the block: its part outside the block is cut away.
        var inner1 = new Range(2, 3, 2, 6);
        var inner2 = new Range(3, 2, 4, 1);

        ZoneSpec zone = Assert.Single(Compute([Diff(2, 4, 2, 3, inner1, inner2)]));

        Assert.Equal([inner1, inner2], ((DeletedLinesZoneData)zone.Data!).DeletedRanges);
    }

    [Fact]
    public void Compute_InnerChangeOutsideTheBlock_Dropped()
    {
        ZoneSpec zone = Assert.Single(Compute([Diff(2, 4, 2, 3, new Range(4, 1, 4, 5))]));

        Assert.Empty(((DeletedLinesZoneData)zone.Data!).DeletedRanges);
    }

    [Fact]
    public void Compute_NoDiffAlignment_NegativeDelta_EmptyZone()
    {
        // The modified side has 20 px of extra space on line 3: the zone of the other editor is not there.
        ZoneSpec zone = Assert.Single(Compute([], modifiedExtra: [new AdditionalLineHeight(3, 20)]));

        Assert.Equal(ViewZoneKind.Empty, zone.Kind);
        Assert.Equal(3, zone.AfterLineNumber);
        Assert.Equal(20, zone.HeightInPx);
        Assert.Null(zone.Data);
    }

    [Fact]
    public void Compute_NoDiffAlignment_PositiveDelta_Nothing() =>
        Assert.Empty(Compute([], originalExtra: [new AdditionalLineHeight(3, 20)]));

    [Fact]
    public void Compute_ZoneSuppressesMouseDown() =>
        Assert.All(Compute([Diff(3, 5, 3, 3)]), zone => Assert.True(zone.SuppressMouseDown));

    [Fact]
    public void DeletedLinesZoneData_Equals_IgnoresTheDelegate()
    {
        var a = new DeletedLinesZoneData(new LineRange(1, 3), _ => null!, [new Range(1, 1, 1, 2)], true);
        var b = new DeletedLinesZoneData(new LineRange(1, 3), _ => throw new InvalidOperationException(), [new Range(1, 1, 1, 2)], true);

        Assert.Equal(a, b);
        Assert.NotEqual(a, b with { RenderIndicators = false });
    }
}

using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

// New tests: Monaco has no test of OverviewZoneManager. The expected numbers come from the formulas, by hand.
public class OverviewZoneManagerTests
{
    private static readonly ThemeColor Red = ThemeColor.FromRgb(255, 0, 0);
    private static readonly ThemeColor Green = ThemeColor.FromRgb(0, 255, 0);

    private int _offsetCalls;

    // Line height 10, outer height 1000, canvas 100: the ratio is 0.1; the minimum half height is 2.
    private OverviewZoneManager NewManager()
    {
        var manager = new OverviewZoneManager(line =>
        {
            _offsetCalls++;
            return (line - 1) * 10;
        });
        manager.SetLineHeight(10);
        manager.SetOuterHeight(1000);
        manager.SetDomHeight(100);
        return manager;
    }

    private static (int From, int To) Single(OverviewZoneManager manager, OverviewRulerZone zone)
    {
        manager.SetZones([zone]);
        ColorZone resolved = Assert.Single(manager.ResolveColorZones());
        return (resolved.From, resolved.To);
    }

    [Fact]
    public void ResolveColorZones_SingleZone_ProportionalPosition() =>
        Assert.Equal((10, 20), Single(NewManager(), new OverviewRulerZone(11, 20, 10, Red)));

    [Fact]
    public void ResolveColorZones_ZoneShorterThanMinimum_GrownToFourPixels() =>
        Assert.Equal((18, 22), Single(NewManager(), new OverviewRulerZone(21, 21, 1, Red)));

    [Fact]
    public void ResolveColorZones_ZoneAtTopEdge_ClampedInsideCanvas() =>
        Assert.Equal((0, 4), Single(NewManager(), new OverviewRulerZone(1, 1, 1, Red)));

    [Fact]
    public void ResolveColorZones_ZoneAtBottomEdge_ClampedInsideCanvas() =>
        Assert.Equal((96, 100), Single(NewManager(), new OverviewRulerZone(100, 100, 1, Red)));

    [Fact]
    public void ResolveColorZones_HeightInLinesZero_UsesEndLine() =>
        Assert.Equal((9, 13), Single(NewManager(), new OverviewRulerZone(11, 13, 0, Red)));

    [Fact]
    public void ResolveColorZones_SameColorsSortedByFrom()
    {
        OverviewZoneManager manager = NewManager();
        manager.SetZones([new OverviewRulerZone(51, 60, 10, Red), new OverviewRulerZone(11, 20, 10, Red)]);

        IReadOnlyList<ColorZone> zones = manager.ResolveColorZones();

        Assert.Equal([10, 50], zones.Select(z => z.From));
    }

    [Fact]
    public void ResolveColorZones_ColorIdsAssignedOncePerColor()
    {
        OverviewZoneManager manager = NewManager();
        manager.SetZones([new OverviewRulerZone(1, 1, 1, Red), new OverviewRulerZone(11, 11, 1, Green), new OverviewRulerZone(21, 21, 1, Red)]);

        IReadOnlyList<ColorZone> zones = manager.ResolveColorZones();

        Assert.Equal([1, 2, 2], zones.Select(z => z.ColorId));
        Assert.Equal(3, manager.GetId2Color().Count);
        // Zones are sorted by color before ids are assigned: Green (0xFF00FF00) comes first.
        Assert.Equal(Green, manager.GetId2Color()[1]);
        Assert.Equal(Red, manager.GetId2Color()[2]);
    }

    [Fact]
    public void ResolveColorZones_CachedUntilInvalidated()
    {
        OverviewZoneManager manager = NewManager();
        manager.SetZones([new OverviewRulerZone(11, 20, 10, Red)]);
        manager.ResolveColorZones();
        int calls = _offsetCalls;

        manager.ResolveColorZones();
        Assert.Equal(calls, _offsetCalls);

        manager.SetLineHeight(12);
        manager.ResolveColorZones();
        Assert.True(_offsetCalls > calls);
    }

    [Fact]
    public void SetLineHeight_Same_ReturnsFalse()
    {
        var manager = new OverviewZoneManager(_ => 0);

        Assert.True(manager.SetLineHeight(10));
        Assert.False(manager.SetLineHeight(10));
    }

    [Fact]
    public void SetDomHeight_Change_InvalidatesCache()
    {
        OverviewZoneManager manager = NewManager();
        var zone = new OverviewRulerZone(11, 20, 10, Red);
        Assert.Equal((10, 20), Single(manager, zone));

        Assert.True(manager.SetDomHeight(200));

        ColorZone resolved = Assert.Single(manager.ResolveColorZones());
        Assert.Equal((20, 40), (resolved.From, resolved.To));
    }

    [Fact]
    public void SetPixelRatio_ScalesCanvas()
    {
        OverviewZoneManager manager = NewManager();
        manager.SetDomWidth(30);

        manager.SetPixelRatio(2);

        Assert.Equal(200, manager.CanvasHeight);
        Assert.Equal(60, manager.CanvasWidth);
    }

    [Fact]
    public void ColorZone_Compare_ByColorThenFromThenTo()
    {
        Assert.True(ColorZone.Compare(new ColorZone(5, 6, 1), new ColorZone(0, 1, 2)) < 0);
        Assert.True(ColorZone.Compare(new ColorZone(1, 9, 1), new ColorZone(2, 3, 1)) < 0);
        Assert.True(ColorZone.Compare(new ColorZone(1, 3, 1), new ColorZone(1, 9, 1)) < 0);
        Assert.Equal(0, ColorZone.Compare(new ColorZone(1, 3, 1), new ColorZone(1, 3, 1)));
    }

    [Fact]
    public void OverviewRulerZone_Compare_Order()
    {
        var a = new OverviewRulerZone(1, 5, 2, Red);

        Assert.Equal(0, OverviewRulerZone.Compare(a, new OverviewRulerZone(1, 5, 2, Red)));
        Assert.True(OverviewRulerZone.Compare(a, new OverviewRulerZone(2, 5, 2, Red)) < 0);
        Assert.True(OverviewRulerZone.Compare(a, new OverviewRulerZone(1, 5, 3, Red)) < 0);
        Assert.True(OverviewRulerZone.Compare(a, new OverviewRulerZone(1, 6, 2, Red)) < 0);
        Assert.NotEqual(0, OverviewRulerZone.Compare(new OverviewRulerZone(9, 9, 9, Red), new OverviewRulerZone(1, 1, 1, Green)));
    }
}

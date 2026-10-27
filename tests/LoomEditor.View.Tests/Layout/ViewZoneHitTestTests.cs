using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

public class ViewZoneHitTestTests
{
    private static readonly ViewZone First = new() { Id = "a" };
    private static readonly ViewZone Second = new() { Id = "b" };

    private static ViewZoneViewportData[] Zones() =>
    [
        new(First, 38, 40),
        new(Second, 100, 20),
    ];

    [Fact]
    public void HitTest_InsideZone_ReturnsZoneAndOffset()
    {
        (ViewZone Zone, double YInZone)? hit = ViewZoneViewportData.HitTest(Zones(), 60, 50, 40, 500);

        Assert.NotNull(hit);
        Assert.Same(First, hit.Value.Zone);
        Assert.Equal(12, hit.Value.YInZone);
    }

    [Fact]
    public void HitTest_BetweenLines_Null() =>
        Assert.Null(ViewZoneViewportData.HitTest(Zones(), 60, 90, 40, 500));

    [Fact]
    public void HitTest_LeftOfContent_Null() =>
        Assert.Null(ViewZoneViewportData.HitTest(Zones(), 10, 50, 40, 500));

    [Fact]
    public void HitTest_ZoneWithZeroHeight_Null() =>
        Assert.Null(ViewZoneViewportData.HitTest([new ViewZoneViewportData(First, 38, 0)], 60, 38, 40, 500));

    [Fact]
    public void HitTest_TwoZones_ReturnsTheOneUnderThePoint()
    {
        (ViewZone Zone, double YInZone)? hit = ViewZoneViewportData.HitTest(Zones(), 60, 110, 40, 500);

        Assert.NotNull(hit);
        Assert.Same(Second, hit.Value.Zone);
        Assert.Equal(10, hit.Value.YInZone);
    }
}

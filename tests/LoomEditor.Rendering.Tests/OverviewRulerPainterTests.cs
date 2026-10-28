using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test of the canvas drawing: all tests are new. Line height 10, outer height 1000, canvas height `domHeight`.
public class OverviewRulerPainterTests
{
    private static readonly ThemeColor Red = ThemeColor.FromRgb(255, 0, 0);
    private static readonly ThemeColor Green = ThemeColor.FromRgb(0, 255, 0);

    private static RecordingRenderSurface Draw(int domHeight, params OverviewRulerZone[] zones)
    {
        var manager = new OverviewZoneManager(line => (line - 1) * 10);
        manager.SetLineHeight(10);
        manager.SetOuterHeight(1000);
        manager.SetDomWidth(15);
        manager.SetDomHeight(domHeight);
        manager.SetZones(zones);
        var surface = new RecordingRenderSurface();
        new OverviewRulerPainter(manager).Draw(surface, 5, 2, 15, domHeight);
        return surface;
    }

    [Fact]
    public void Draw_NoZones_NoRectangles() => Assert.Empty(Draw(100).Calls);

    [Fact]
    public void Draw_TwoZonesSameColorAdjacent_OneRectangle()
    {
        RecordingRenderSurface surface = Draw(100, new OverviewRulerZone(11, 20, 10, Red), new OverviewRulerZone(21, 30, 10, Red));

        // Bands 10..20 and 20..30 touch: one rectangle from 10 to 30, offset by the top of the strip.
        Assert.Equal(new FillRectangleCall(5, 12, 15, 20, Red), Assert.Single(surface.Calls));
    }

    [Fact]
    public void Draw_TwoColors_TwoRectangles()
    {
        RecordingRenderSurface surface = Draw(100, new OverviewRulerZone(11, 20, 10, Red), new OverviewRulerZone(51, 60, 10, Green));

        Assert.Equal(2, surface.Calls.Count);
        Assert.Contains(new FillRectangleCall(5, 12, 15, 10, Red), surface.Calls);
        Assert.Contains(new FillRectangleCall(5, 52, 15, 10, Green), surface.Calls);
    }

    [Fact]
    public void Draw_ScaledByHeightRatio()
    {
        // The strip is 200 high for 1000 of content: the ratio is 0.2.
        RecordingRenderSurface surface = Draw(200, new OverviewRulerZone(11, 20, 10, Red));

        Assert.Equal(new FillRectangleCall(5, 22, 15, 20, Red), Assert.Single(surface.Calls));
    }
}

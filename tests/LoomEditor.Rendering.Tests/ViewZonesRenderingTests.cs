using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the drawing of view zones (DOM): all tests are new.
public class ViewZonesRenderingTests
{
    private const string Text = "L1\nL2\nL3\nL4\nL5\nL6";

    private static void AddZone(ViewHarness harness, ViewZone zone) =>
        harness.ViewModel.ViewZones.Change(accessor => accessor.AddZone(zone));

    private static List<DrawImageCall> Images(RecordingRenderSurface surface) => [.. surface.Calls.OfType<DrawImageCall>().Where(call => call.Opacity == 1f)]; // the minimap draws translucent images

    [Fact]
    public void Draw_EmptyZone_DrawsOneImageOverZoneRect()
    {
        using var harness = new ViewHarness(Text);
        AddZone(harness, new ViewZone { AfterLineNumber = 2, HeightInPx = 30 });

        DrawImageCall image = Assert.Single(Images(harness.Render()));

        // Two lines of 20 above the zone.
        Assert.Equal((float)harness.LayoutInfo.ContentLeft, image.X);
        Assert.Equal(40f, image.Y);
        Assert.Equal(30f, image.Height);
        Assert.Equal(harness.LayoutInfo.Width - harness.LayoutInfo.ContentLeft, image.Width);
    }

    [Fact]
    public void Draw_NoZones_NoImageCalls()
    {
        using var harness = new ViewHarness(Text);

        Assert.Empty(Images(harness.Render()));
    }

    [Fact]
    public void Draw_ZoneOutsideViewport_NotDrawn()
    {
        using var harness = new ViewHarness(string.Join('\n', Enumerable.Range(1, 100)), height: 100);
        AddZone(harness, new ViewZone { AfterLineNumber = 80, HeightInPx = 30 });

        Assert.Empty(Images(harness.Render()));
    }

    [Fact]
    public void Draw_HiddenZone_NotDrawn()
    {
        using var harness = new ViewHarness(Text);
        harness.ViewModel.SetHiddenAreas(new[] { new Range(3, 1, 4, 1) });
        AddZone(harness, new ViewZone { AfterLineNumber = 3, HeightInPx = 30 });

        Assert.Empty(Images(harness.Render()));
    }

    [Fact]
    public void Draw_DeletedLinesZone_Skipped_NoException()
    {
        using var harness = new ViewHarness(Text);
        AddZone(harness, new ViewZone { AfterLineNumber = 2, HeightInPx = 30, Kind = ViewZoneKind.DeletedLines });
        AddZone(harness, new ViewZone { AfterLineNumber = 3, HeightInPx = 30, Kind = ViewZoneKind.Collapsed });

        Assert.Empty(Images(harness.Render()));
    }

    [Fact]
    public void Draw_ScrolledViewport_ZoneRectMovesWithScrollTop()
    {
        using var harness = new ViewHarness(string.Join('\n', Enumerable.Range(1, 100)), height: 100);
        AddZone(harness, new ViewZone { AfterLineNumber = 5, HeightInPx = 30 });
        harness.ScrollTo(scrollTop: 60);

        DrawImageCall image = Assert.Single(Images(harness.Render()));

        Assert.Equal(100f - 60f, image.Y);
    }

    [Fact]
    public void Draw_NoDiagonalFillColor_NotDrawn()
    {
        using var harness = new ViewHarness(Text);
        harness.Renderer.Theme = EditorTheme.Dark with
        {
            Type = ThemeType.HighContrastDark,
            Colors = new Dictionary<string, ThemeColor>(),
        };
        AddZone(harness, new ViewZone { AfterLineNumber = 2, HeightInPx = 30 });

        Assert.Empty(Images(harness.Render()));
    }

    [Fact]
    public void Pattern_Buffer_StripesAtExpectedPixels()
    {
        var color = new ThemeColor(255, 10, 20, 30);

        DiagonalFillPattern.Entry entry = new DiagonalFillPattern().Get(16, 8, color);

        byte AlphaAt(int x, int y) => entry.Rgba[(((y * entry.Width) + x) * 4) + 3];
        Assert.Equal(255, AlphaAt(0, 0));
        Assert.Equal(255, AlphaAt(1, 0));
        Assert.Equal(255, AlphaAt(8, 0));
        Assert.Equal(255, AlphaAt(7, 1));
        Assert.Equal(0, AlphaAt(2, 0));
        Assert.Equal(0, AlphaAt(4, 0));
        Assert.Equal((byte)10, entry.Rgba[0]);
    }

    [Fact]
    public void Pattern_Buffer_RowHasTwoStripePixelsPerTile()
    {
        DiagonalFillPattern.Entry entry = new DiagonalFillPattern().Get(8, 8, new ThemeColor(255, 10, 20, 30));

        for (int y = 0; y < 8; y++)
        {
            int colored = 0;
            for (int x = 0; x < 8; x++)
            {
                if (entry.Rgba[(((y * entry.Width) + x) * 4) + 3] != 0)
                {
                    colored++;
                }
            }

            Assert.Equal(2, colored);
        }
    }

    [Fact]
    public void Pattern_Cache_ReusesBufferForSameSize()
    {
        var pattern = new DiagonalFillPattern();
        var color = new ThemeColor(255, 1, 2, 3);

        Assert.Same(pattern.Get(10, 10, color), pattern.Get(10, 10, color));
    }

    [Fact]
    public void Pattern_Cache_EvictsBeyondEight()
    {
        var pattern = new DiagonalFillPattern();
        var color = new ThemeColor(255, 1, 2, 3);
        DiagonalFillPattern.Entry first = pattern.Get(1, 1, color);
        for (int i = 2; i <= DiagonalFillPattern.Capacity + 1; i++)
        {
            pattern.Get(i, 1, color);
        }

        DiagonalFillPattern.Entry again = pattern.Get(1, 1, color);

        Assert.NotSame(first, again);
        Assert.NotEqual(first.Version, again.Version);
    }
}

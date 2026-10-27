using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.Themes;
using LoomEditor.View.DiffEditor;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the drawing of the collapsed bar (DOM): all tests are new.
public class CollapsedZoneRenderingTests
{
    private const string Text = "L1\nL2\nL3\nL4\nL5\nL6";

    // A region of `count` lines hidden behind a bar after model line 2.
    private static void AddBar(ViewHarness harness, int count = 10, int top = 0, int bottom = 0) =>
        harness.ViewModel.ViewZones.Change(accessor => accessor.AddZone(new ViewZone
        {
            AfterLineNumber = 2,
            HeightInPx = 24,
            Kind = ViewZoneKind.Collapsed,
            Data = new CollapsedZoneData(new UnchangedRegion(3, 3, count, top, bottom), false),
        }));

    private static ThemeColor Color(ViewHarness harness, string id) => harness.Renderer.Theme.GetColor(id)!.Value;

    [Fact]
    public void Draw_Collapsed_DrawsBackgroundGlyphAndLabel()
    {
        using var harness = new ViewHarness(Text);
        AddBar(harness);

        RecordingRenderSurface surface = harness.Render();

        float left = (float)harness.LayoutInfo.ContentLeft;
        ThemeColor background = Color(harness, "diffEditor.unchangedRegionBackground");
        Assert.Contains(new FillRectangleCall(left, 40, harness.LayoutInfo.Width - left, 24, background), surface.Calls);
        Assert.Contains(surface.Calls.OfType<DrawLineLayoutCall>(), c => c.Text == "10 hidden lines" && c.Color == Color(harness, "diffEditor.unchangedRegionForeground"));
        Assert.Contains(surface.Calls.OfType<DrawLineLayoutCall>(), c => c.Text == "⇕" && c.X == harness.LayoutInfo.LineNumbersLeft);
    }

    [Fact]
    public void Draw_Label_SingularAndPlural()
    {
        Assert.Equal("1 hidden line", TextViewRenderer.CollapsedLabel(1));
        Assert.Equal("2 hidden lines", TextViewRenderer.CollapsedLabel(2));
    }

    private static int Strips(ViewHarness harness, RecordingRenderSurface surface, float y) =>
        surface.Calls.OfType<FillRectangleCall>().Count(c => c.Y == y && c.Height == 2 && c.Color == Color(harness, "diffEditor.unchangedRegionForeground"));

    [Fact]
    public void Draw_TopStrip_OnlyWhenVisibleTopLines()
    {
        using var with = new ViewHarness(Text);
        AddBar(with, top: 2);
        using var without = new ViewHarness(Text);
        AddBar(without);

        Assert.Equal(1, Strips(with, with.Render(), 40));
        Assert.Equal(0, Strips(without, without.Render(), 40));
    }

    [Fact]
    public void Draw_BottomStrip_OnlyWhenVisibleBottomLines()
    {
        using var with = new ViewHarness(Text);
        AddBar(with, bottom: 2);
        using var without = new ViewHarness(Text);
        AddBar(without);

        Assert.Equal(1, Strips(with, with.Render(), 62));
        Assert.Equal(0, Strips(without, without.Render(), 62));
    }
}

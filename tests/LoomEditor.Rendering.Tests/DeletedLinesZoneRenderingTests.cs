using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the drawing of view zones (DOM): all tests are new.
public class DeletedLinesZoneRenderingTests
{
    private const string Text = "L1\nL2\nL3\nL4\nL5\nL6";
    private const float Lh = FakeTextMeasurer.LineHeight;

    private static readonly TextModel OriginalModel = new("a1\na2\na3\na4\na5\na6\nalpha\nbeta\nb9");

    private static ViewLineData Line(int lineNumber) => IdentityModelLineProjection.Instance.GetViewLineData(OriginalModel, lineNumber, 0, lineNumber);

    // Two deleted lines (original lines 7 and 8) after modified line 2.
    private static void AddZone(ViewHarness harness, Func<int, ViewLineData>? getLine = null, bool indicators = true, params Range[] deletedRanges) =>
        harness.ViewModel.ViewZones.Change(accessor => accessor.AddZone(new ViewZone
        {
            AfterLineNumber = 2,
            HeightInPx = 40,
            Kind = ViewZoneKind.DeletedLines,
            Data = new DeletedLinesZoneData(new LineRange(7, 9), getLine ?? Line, deletedRanges, indicators),
        }));

    private static ThemeColor Color(ViewHarness harness, string id) => harness.Renderer.Theme.GetColor(id)!.Value;

    [Fact]
    public void Draw_DeletedZone_DrawsBackgroundAndOneLineLayoutPerLine()
    {
        using var harness = new ViewHarness(Text);
        AddZone(harness);

        RecordingRenderSurface surface = harness.Render();

        float left = (float)harness.LayoutInfo.ContentLeft;
        float width = harness.LayoutInfo.Width - left;
        ThemeColor background = Color(harness, "diffEditor.removedLineBackground");
        Assert.Contains(new FillRectangleCall(left, 40, width, Lh, background), surface.Calls);
        Assert.Contains(new FillRectangleCall(left, 60, width, Lh, background), surface.Calls);
        Assert.Contains(surface.Calls.OfType<DrawLineLayoutCall>(), c => c.Text == "alpha" && c.LineTop == 40 && c.X == left);
        Assert.Contains(surface.Calls.OfType<DrawLineLayoutCall>(), c => c.Text == "beta" && c.LineTop == 60);
    }

    [Fact]
    public void Draw_DeletedRanges_DrawBackgroundsBehindText()
    {
        using var harness = new ViewHarness(Text);
        AddZone(harness, deletedRanges: [new Range(7, 2, 7, 4), new Range(8, 3, 9, 1)]);

        RecordingRenderSurface surface = harness.Render();

        float left = (float)harness.LayoutInfo.ContentLeft;
        float cw = (float)FakeTextMeasurer.CharacterWidth;
        ThemeColor color = Color(harness, "diffEditor.removedTextBackground");
        int first = surface.Calls.IndexOf(new FillRectangleCall(left + cw, 40, 2 * cw, Lh, color));
        // The second range goes from column 3 to the end of "beta" (it continues on the line after the zone).
        int second = surface.Calls.IndexOf(new FillRectangleCall(left + (2 * cw), 60, 2 * cw, Lh, color));
        Assert.True(first >= 0 && second >= 0);
        Assert.True(first < surface.Calls.FindIndex(c => c is DrawLineLayoutCall { Text: "alpha" }));
    }

    [Fact]
    public void Draw_OriginalLineNumbersInMargin()
    {
        using var harness = new ViewHarness(Text);
        AddZone(harness);

        RecordingRenderSurface surface = harness.Render();

        float right = harness.LayoutInfo.LineNumbersLeft + harness.LayoutInfo.LineNumbersWidth;
        ThemeColor numberColor = harness.Renderer.Theme.LineNumberForeground;
        Assert.Contains(surface.Calls.OfType<DrawLineLayoutCall>(), c => c.Text == "7" && c.LineTop == 40 && c.Color == numberColor && c.X + FakeTextMeasurer.CharacterWidth == right);
        Assert.Contains(surface.Calls.OfType<DrawLineLayoutCall>(), c => c.Text == "8" && c.LineTop == 60);
        Assert.Contains(
            new FillRectangleCall(0, 40, (float)harness.LayoutInfo.ContentLeft, Lh, Color(harness, "diffEditor.removedLineBackground")), surface.Calls);
    }

    private static int MarginLines(ViewHarness harness, RecordingRenderSurface surface) =>
        surface.Calls.OfType<DrawLineCall>().Count(c => c.Y1 == 50 && c.X1 >= harness.LayoutInfo.DecorationsLeft && c.X2 <= harness.LayoutInfo.ContentLeft);

    [Fact]
    public void Draw_RenderIndicators_DrawsMinusSign()
    {
        using var withIndicators = new ViewHarness(Text);
        AddZone(withIndicators, indicators: true);
        using var withoutIndicators = new ViewHarness(Text);
        AddZone(withoutIndicators, indicators: false);

        Assert.Equal(1, MarginLines(withIndicators, withIndicators.Render()));
        Assert.Equal(0, MarginLines(withoutIndicators, withoutIndicators.Render()));
    }

    [Fact]
    public void Draw_LineMissing_Skipped()
    {
        using var harness = new ViewHarness(Text);
        AddZone(harness, line => line == 8 ? throw new ArgumentOutOfRangeException(nameof(line)) : Line(line));

        RecordingRenderSurface surface = harness.Render();

        Assert.Contains(surface.Calls.OfType<DrawLineLayoutCall>(), c => c.Text == "alpha");
        Assert.DoesNotContain(surface.Calls.OfType<DrawLineLayoutCall>(), c => c.Text is "beta" or "8");
    }

    [Fact]
    public void Draw_StaleRanges_AreClippedToTheLine()
    {
        using var harness = new ViewHarness(Text);
        AddZone(harness, deletedRanges: [new Range(7, 3, 7, 50)]);

        RecordingRenderSurface surface = harness.Render();

        float left = (float)harness.LayoutInfo.ContentLeft;
        float cw = (float)FakeTextMeasurer.CharacterWidth;
        Assert.Contains(new FillRectangleCall(left + (2 * cw), 40, 3 * cw, Lh, Color(harness, "diffEditor.removedTextBackground")), surface.Calls);
    }

    [Fact]
    public void Draw_NormalEditorRendering_Unchanged()
    {
        using var harness = new ViewHarness(Text);

        RecordingRenderSurface surface = harness.Render();

        ThemeColor removed = Color(harness, "diffEditor.removedLineBackground");
        Assert.DoesNotContain(surface.Calls.OfType<FillRectangleCall>(), c => c.Color == removed);
        Assert.Equal(6, surface.Calls.OfType<DrawLineLayoutCall>().Count(c => c.Text.StartsWith('L')));
    }
}

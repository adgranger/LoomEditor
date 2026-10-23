using LoomEditor.Core.Decorations;
using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the decorations overview ruler (it draws on a canvas): all tests are new.
public class TextViewRendererOverviewRulerTests
{
    private const string MarkColorValue = "#ff0000";

    private static readonly ThemeColor MarkColor = ThemeColor.Parse(MarkColorValue);

    // A 200-line document is much taller than the viewport, so one line is less than the 6 DIPs of a mark.
    private static ViewHarness CreateHarness(EditorOverviewRulerOptions? options = null)
    {
        var harness = new ViewHarness(string.Join('\n', Enumerable.Repeat("a", 200)));
        if (options is not null)
        {
            harness.ViewModel.SetOptions(harness.ViewModel.LayoutOptions with { OverviewRuler = options }, harness.ViewModel.ViewLayoutOptions);
        }

        return harness;
    }

    private static void AddMark(ViewHarness harness, int lineNumber, OverviewRulerLane lane = OverviewRulerLane.Center)
    {
        ModelDecorationOptions options = ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData
        {
            Description = "test",
            OverviewRuler = new ModelDecorationOverviewRulerOptions(DecorationColor.FromValue(MarkColorValue), null, lane),
        });
        harness.Model.DeltaDecorations(harness.ViewModel.EditorId, [], new[] { new ModelDeltaDecoration(new Range(lineNumber, 1, lineNumber, 1), options) });
    }

    private static float RulerLeft(ViewHarness harness) => harness.LayoutInfo.Width - harness.LayoutInfo.OverviewRuler.Right - harness.LayoutInfo.OverviewRuler.Width;

    private static List<FillRectangleCall> Fills(RecordingRenderSurface surface, ThemeColor color) =>
        [.. surface.Calls.OfType<FillRectangleCall>().Where(call => call.Color == color)];

    [Fact]
    public void DrawOverviewRuler_DecorationMark_HasTheMinimumHeight()
    {
        using ViewHarness harness = CreateHarness();
        AddMark(harness, 100);

        RecordingRenderSurface surface = harness.Render();

        // Center lane of 3: x = 1 + 4, width 5 (see OverviewRulerSettingsTests).
        FillRectangleCall mark = Assert.Single(Fills(surface, MarkColor));
        Assert.Equal(RulerLeft(harness) + 5, mark.X);
        Assert.Equal(5, mark.Width);
        Assert.Equal(6, mark.Height);
    }

    [Fact]
    public void DrawOverviewRuler_CloseMarksOfTheSameLane_AreMerged()
    {
        using ViewHarness harness = CreateHarness();
        AddMark(harness, 100);
        AddMark(harness, 103);
        AddMark(harness, 180);

        RecordingRenderSurface surface = harness.Render();

        // The first two are less than a mark apart: one rectangle, taller than a single mark. The third one is alone.
        List<FillRectangleCall> marks = Fills(surface, MarkColor);
        Assert.Equal(2, marks.Count);
        Assert.True(marks[0].Height > 6);
        Assert.Equal(6, marks[1].Height);
    }

    [Fact]
    public void DrawOverviewRuler_Cursor_DrawnInTheFullLane()
    {
        using ViewHarness harness = CreateHarness();
        ThemeColor cursor = harness.Renderer.Theme.GetColor("editorCursor.foreground")!.Value.WithAlphaFactor(0.7);

        RecordingRenderSurface surface = harness.Render();

        // The cursor is on the first line: clamped to the top of the ruler, 2 DIPs high, in all the lanes.
        FillRectangleCall mark = Assert.Single(Fills(surface, cursor));
        Assert.Equal(new FillRectangleCall(RulerLeft(harness) + 1, 0, 13, 2, cursor), mark);
    }

    [Fact]
    public void DrawOverviewRuler_HideCursor_NotDrawn()
    {
        using ViewHarness harness = CreateHarness(new EditorOverviewRulerOptions(HideCursor: true));
        ThemeColor cursor = harness.Renderer.Theme.GetColor("editorCursor.foreground")!.Value.WithAlphaFactor(0.7);

        RecordingRenderSurface surface = harness.Render();

        Assert.Empty(Fills(surface, cursor));
    }

    [Fact]
    public void DrawOverviewRuler_Border_DrawnWhenEnabled()
    {
        using ViewHarness harness = CreateHarness();
        using ViewHarness withoutBorder = CreateHarness(new EditorOverviewRulerOptions(Border: false));
        ThemeColor border = harness.Renderer.Theme.GetColor("editorOverviewRuler.border")!.Value;

        RecordingRenderSurface surface = harness.Render();
        RecordingRenderSurface surfaceWithoutBorder = withoutBorder.Render();

        float left = RulerLeft(harness);
        List<FillRectangleCall> borders = Fills(surface, border);
        Assert.Equal(2, borders.Count);
        Assert.Contains(new FillRectangleCall(left, 0, 1, harness.LayoutInfo.OverviewRuler.Height, border), borders);
        Assert.Contains(new FillRectangleCall(left + 1, 0, harness.LayoutInfo.OverviewRuler.Width - 1, 1, border), borders);
        Assert.Empty(Fills(surfaceWithoutBorder, border));
    }

    [Fact]
    public void DrawOverviewRuler_LanesZero_DrawsNothing()
    {
        using ViewHarness harness = CreateHarness(new EditorOverviewRulerOptions(Lanes: 0));
        AddMark(harness, 100);
        ThemeColor border = harness.Renderer.Theme.GetColor("editorOverviewRuler.border")!.Value;

        RecordingRenderSurface surface = harness.Render();

        float left = RulerLeft(harness);
        Assert.Empty(Fills(surface, MarkColor));
        Assert.Empty(Fills(surface, border));
        Assert.DoesNotContain(surface.Calls.OfType<FillRectangleCall>(), call => call.X >= left && call.Width <= 13 && call.Height == 2);
    }
}

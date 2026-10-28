using LoomEditor.Core.Text;
using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the drawing of view lines (it renders HTML): all tests are new.
public class TextViewRendererWhitespaceTests
{
    private static ViewHarness Harness(string text, RenderWhitespace mode)
    {
        var harness = new ViewHarness(text);
        harness.Renderer.RenderWhitespace = mode;
        return harness;
    }

    private static List<DrawLineLayoutCall> Dots(RecordingRenderSurface surface) =>
        [.. surface.Calls.OfType<DrawLineLayoutCall>().Where(call => call.Text is "·" or "⸱")];

    private static List<DrawLineLayoutCall> Arrows(RecordingRenderSurface surface) =>
        [.. surface.Calls.OfType<DrawLineLayoutCall>().Where(call => call.Text == "→")];

    [Fact]
    public void None_DrawsNothing()
    {
        using ViewHarness harness = Harness("a b\tc  ", RenderWhitespace.None);

        RecordingRenderSurface surface = harness.Render();

        Assert.Empty(Dots(surface));
        Assert.Empty(Arrows(surface));
    }

    [Fact]
    public void All_DrawsOneDotPerSpace()
    {
        using ViewHarness harness = Harness("a b c", RenderWhitespace.All);

        Assert.Equal(2, Dots(harness.Render()).Count);
    }

    [Fact]
    public void All_Tab_DrawsArrowAtTabStart()
    {
        using ViewHarness harness = Harness("a\tb", RenderWhitespace.All);

        DrawLineLayoutCall arrow = Assert.Single(Arrows(harness.Render()));

        Assert.Equal((float)(harness.LayoutInfo.ContentLeft + FakeTextMeasurer.CharacterWidth), arrow.X);
        Assert.Empty(Dots(harness.Render()));
    }

    [Fact]
    public void Boundary_InnerSingleSpace_NotDrawn()
    {
        using ViewHarness harness = Harness("a b  c", RenderWhitespace.Boundary);

        List<DrawLineLayoutCall> dots = Dots(harness.Render());

        Assert.Equal(2, dots.Count);
        Assert.All(dots, dot => Assert.True(dot.X >= harness.LayoutInfo.ContentLeft + (3 * FakeTextMeasurer.CharacterWidth)));
    }

    [Fact]
    public void Selection_OnlyInsideSelection()
    {
        using ViewHarness harness = Harness("a b c", RenderWhitespace.Selection);

        List<DrawLineLayoutCall> dots = Dots(harness.Render(new Selection(1, 1, 1, 3)));

        DrawLineLayoutCall dot = Assert.Single(dots);
        Assert.Equal((float)(harness.LayoutInfo.ContentLeft + FakeTextMeasurer.CharacterWidth), dot.X);
    }

    [Fact]
    public void Trailing_OnlyAtTheEnd()
    {
        using ViewHarness harness = Harness("a b  ", RenderWhitespace.Trailing);

        List<DrawLineLayoutCall> dots = Dots(harness.Render());

        Assert.Equal(
            (float[])[(float)(harness.LayoutInfo.ContentLeft + (3 * FakeTextMeasurer.CharacterWidth)), (float)(harness.LayoutInfo.ContentLeft + (4 * FakeTextMeasurer.CharacterWidth))],
            dots.Select(dot => dot.X));
    }

    [Fact]
    public void Dot_IsCentered_InTheCell()
    {
        using ViewHarness harness = Harness("a b", RenderWhitespace.All);

        DrawLineLayoutCall dot = Assert.Single(Dots(harness.Render()));

        // The fake dot is as wide as the cell, so it starts at the cell.
        Assert.Equal((float)(harness.LayoutInfo.ContentLeft + FakeTextMeasurer.CharacterWidth), dot.X);
        Assert.Equal(0f, dot.LineTop);
    }

    [Fact]
    public void UsesWhitespaceForeground()
    {
        using ViewHarness harness = Harness("a b\t", RenderWhitespace.All);

        RecordingRenderSurface surface = harness.Render();

        Assert.All(Dots(surface).Concat(Arrows(surface)), call => Assert.Equal(harness.Renderer.Theme.WhitespaceForeground, call.Color));
    }

    [Fact]
    public void ScrollLeft_ShiftsTheDots()
    {
        using ViewHarness harness = Harness("a b" + new string('x', 100), RenderWhitespace.All);
        float before = Assert.Single(Dots(harness.Render())).X;
        harness.ScrollTo(scrollLeft: 30);

        float after = Assert.Single(Dots(harness.Render())).X;

        Assert.Equal(before - 30, after);
    }

    [Fact]
    public void WrappedLine_FauxIndent_NotDrawn()
    {
        using ViewHarness harness = Harness("  aaa bbb ccc ddd\nshort\nx", RenderWhitespace.All);
        harness.ViewModel.SetOptions(
            harness.ViewModel.LayoutOptions with
            {
                Wrapping = new EditorWrappingOptions { WordWrap = WordWrap.WordWrapColumn, WordWrapColumn = 10 },
            },
            harness.ViewModel.ViewLayoutOptions);

        List<DrawLineLayoutCall> dots = Dots(harness.Render());

        // First view line "  aaa bbb ": offsets 0, 1, 5 and 9. Second "  ccc ddd": the 2-space indent is skipped, offset 5 only.
        Assert.Equal(4, dots.Count(dot => dot.LineTop == 0f));
        Assert.Single(dots, dot => dot.LineTop == 20f);
    }
}

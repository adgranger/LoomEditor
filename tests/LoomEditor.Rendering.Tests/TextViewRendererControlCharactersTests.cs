using LoomEditor.Core.Text;
using LoomEditor.Rendering.Tests.TestHelpers;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the drawing of view lines (it renders HTML): all tests are new.
public class TextViewRendererControlCharactersTests
{
    private static List<FillRectangleCall> RedCells(RecordingRenderSurface surface) =>
        [.. surface.Calls.OfType<FillRectangleCall>().Where(call => call.Color == LoomEditor.Themes.ThemeColor.Parse("#960000"))];

    [Fact]
    public void ControlCharacter_DrawsRedCellAndWhiteGlyph()
    {
        using var harness = new ViewHarness("a\u0001b");

        RecordingRenderSurface surface = harness.Render();

        FillRectangleCall cell = Assert.Single(RedCells(surface));
        Assert.Equal((float)(harness.LayoutInfo.ContentLeft + FakeTextMeasurer.CharacterWidth), cell.X);
        Assert.Equal((float)FakeTextMeasurer.CharacterWidth, cell.Width);
        Assert.Equal((float)FakeTextMeasurer.LineHeight, cell.Height);
        FakeLineLayout layout = harness.Measurer.Layouts.Single(l => l.Text == "a\u2401b");
        StyleRun glyph = Assert.Single(layout.Style.Runs, run => run.ForegroundArgb == 0xFFFFFFFF);
        Assert.Equal((1, 1), (glyph.Start, glyph.Length));
    }

    [Fact]
    public void OptionOff_NoRedCell()
    {
        using var harness = new ViewHarness("a\u0001b");
        harness.Renderer.RenderControlCharacters = false;

        Assert.Empty(RedCells(harness.Render()));
        Assert.Contains(harness.Measurer.Layouts, l => l.Text == "a\u0001b");
    }

    [Fact]
    public void Bom_NoRedCell_ButReplacementGlyph()
    {
        using var harness = new ViewHarness("a\uFEFFb");

        Assert.Empty(RedCells(harness.Render()));
        Assert.Contains(harness.Measurer.Layouts, l => l.Text == "a\uFFFDb");
    }

    [Fact]
    public void Caret_AfterControlCharacter_UsesOriginalOffset()
    {
        using var harness = new ViewHarness("a\u0001b");

        RecordingRenderSurface surface = harness.Render(Selection.Collapsed(new Position(1, 3)));

        // The caret is where it is in the same text without the control character.
        FillRectangleCall caret = Assert.Single(surface.Calls.OfType<FillRectangleCall>(), call => call.Color == harness.Renderer.Theme.CursorForeground);
        using var plain = new ViewHarness("axb");
        FillRectangleCall expected = Assert.Single(
            plain.Render(Selection.Collapsed(new Position(1, 3))).Calls.OfType<FillRectangleCall>(),
            call => call.Color == plain.Renderer.Theme.CursorForeground);
        Assert.Equal(expected.X, caret.X);
    }
}

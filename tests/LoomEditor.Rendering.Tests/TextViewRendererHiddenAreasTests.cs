using LoomEditor.Core.Text;
using LoomEditor.Rendering.Tests.TestHelpers;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the drawing of view lines (it renders HTML): all tests are new.
public class TextViewRendererHiddenAreasTests
{
    private const string Text = "L1\nL2\nL3\nL4\nL5\nL6";

    private static ViewHarness HideLines3To4()
    {
        var harness = new ViewHarness(Text);
        harness.ViewModel.SetHiddenAreas(new[] { new Range(3, 1, 4, 1) });
        return harness;
    }

    private static List<DrawLineLayoutCall> Draws(RecordingRenderSurface surface, Func<string, bool> filter) =>
        [.. surface.Calls.OfType<DrawLineLayoutCall>().Where(call => call.Text.Length > 0 && filter(call.Text))];

    [Fact]
    public void Render_WithHiddenLines_DrawsOnlyVisibleLines()
    {
        using ViewHarness harness = HideLines3To4();

        RecordingRenderSurface surface = harness.Render();

        List<DrawLineLayoutCall> texts = Draws(surface, text => text[0] == 'L');
        string[] expectedTexts = ["L1", "L2", "L5", "L6"];
        float[] expectedTops = [0f, 20f, 40f, 60f];
        string[] expectedNumbers = ["1", "2", "5", "6"];
        Assert.Equal(expectedTexts, texts.Select(call => call.Text));
        Assert.Equal(expectedTops, texts.Select(call => call.LineTop));
        Assert.Equal(expectedNumbers, Draws(surface, text => char.IsDigit(text[0])).Select(call => call.Text));
    }

    [Fact]
    public void Render_CursorBelowHiddenLines_HighlightsItsModelLineNumber()
    {
        using ViewHarness harness = HideLines3To4();

        // View line 3 is the model line 5.
        RecordingRenderSurface surface = harness.Render(Selection.Collapsed(new Position(3, 1)));

        List<DrawLineLayoutCall> numbers = Draws(surface, text => char.IsDigit(text[0]));
        Assert.Equal(harness.Renderer.Theme.LineNumberActiveForeground, numbers.Single(call => call.Text == "5").Color);
        Assert.All(numbers.Where(call => call.Text != "5"), call => Assert.Equal(harness.Renderer.Theme.LineNumberForeground, call.Color));
    }

    [Fact]
    public void Render_SelectionAcrossHiddenLines_DrawsTheViewSelection()
    {
        using ViewHarness harness = HideLines3To4();

        // The view selection from view line 2 to view line 3 (model lines 2 and 5).
        RecordingRenderSurface surface = harness.Render(new Selection(2, 1, 3, 3));

        List<FillRectangleCall> fills = [.. surface.Calls.OfType<FillRectangleCall>().Where(call => call.Y is 20 or 40 && call.X >= harness.LayoutInfo.ContentLeft)];
        Assert.Contains(fills, call => call.Y == 20);
        Assert.Contains(fills, call => call.Y == 40);
        Assert.DoesNotContain(surface.Calls.OfType<FillRectangleCall>(), call => call.Y > 60 && call.X >= harness.LayoutInfo.ContentLeft && call.Width < harness.LayoutInfo.Width - harness.LayoutInfo.ContentLeft);
    }
}

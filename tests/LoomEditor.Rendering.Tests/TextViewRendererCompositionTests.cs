using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.View.Controller;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the IME underlines (they are CSS borders): all tests are new.
public class TextViewRendererCompositionTests
{
    private static List<FillRectangleCall> Underlines(ViewHarness harness, RecordingRenderSurface surface) =>
        [.. surface.Calls.OfType<FillRectangleCall>().Where(call => call.Color == harness.Renderer.Theme.CompositionBorder)];

    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 1)]
    public void Render_Format_DrawsAnUnderlineAtTheBottomOfTheLine(bool primary, int thickness)
    {
        using var harness = new ViewHarness("hello world");
        harness.ViewModel.SetCompositionFormats(new[] { new CompositionFormat(new Range(1, 1, 1, 6), primary ? CompositionUnderline.Primary : CompositionUnderline.Secondary) });

        FillRectangleCall call = Assert.Single(Underlines(harness, harness.Render()));

        Assert.Equal(FakeTextMeasurer.LineHeight - thickness, call.Y);
        Assert.Equal(thickness, call.Height);
        Assert.True(call.Width > 0);
    }

    [Fact]
    public void Render_NoneFormatOrNoFormat_DrawsNoUnderline()
    {
        using var harness = new ViewHarness("hello world");
        Assert.Empty(Underlines(harness, harness.Render()));

        harness.ViewModel.SetCompositionFormats(new[] { new CompositionFormat(new Range(1, 1, 1, 6), CompositionUnderline.None) });

        Assert.Empty(Underlines(harness, harness.Render()));
    }
}

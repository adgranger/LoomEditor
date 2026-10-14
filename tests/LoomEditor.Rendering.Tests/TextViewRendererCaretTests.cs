using LoomEditor.Core.Text;
using LoomEditor.Rendering.Tests.TestHelpers;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the drawing of the caret (it renders HTML): all tests are new.
public class TextViewRendererCaretTests
{
    private const float CharacterWidth = (float)FakeTextMeasurer.CharacterWidth;

    private static string Lines(int count) => string.Join('\n', Enumerable.Range(1, count).Select(i => "Lxxxxx" + i));

    private static List<FillRectangleCall> CaretDraws(ViewHarness harness, RecordingRenderSurface surface) =>
        [.. surface.Calls.OfType<FillRectangleCall>().Where(call => call.Color == harness.Renderer.Theme.CursorForeground)];

    [Fact]
    public void Render_CaretAtColumn4_DrawsA2WideRectangleOneDipBeforeTheCharacter()
    {
        using var harness = new ViewHarness(Lines(10));

        RecordingRenderSurface surface = harness.Render(Selection.Collapsed(new Position(2, 4)));

        FillRectangleCall caret = Assert.Single(CaretDraws(harness, surface));
        Assert.Equal(harness.LayoutInfo.ContentLeft + (3 * CharacterWidth) - 1, caret.X);
        Assert.Equal(2, caret.Width);
        Assert.Equal(FakeTextMeasurer.LineHeight, caret.Y);
        Assert.Equal(FakeTextMeasurer.LineHeight, caret.Height);
    }

    [Fact]
    public void Render_CaretAtColumn1_IsNotShifted()
    {
        using var harness = new ViewHarness(Lines(10));

        RecordingRenderSurface surface = harness.Render(Selection.Collapsed(new Position(1, 1)));

        FillRectangleCall caret = Assert.Single(CaretDraws(harness, surface));
        Assert.Equal(harness.LayoutInfo.ContentLeft, caret.X);
    }

    [Fact]
    public void Render_PixelRatio1Point25_SnapsTheWidthToDevicePixels()
    {
        using var harness = new ViewHarness(Lines(10), pixelRatio: 1.25);

        RecordingRenderSurface surface = harness.Render(Selection.Collapsed(new Position(1, 4)));

        FillRectangleCall caret = Assert.Single(CaretDraws(harness, surface));
        Assert.Equal((float)(Math.Floor(2.5) / 1.25), caret.Width);
        // Narrower than 2 DIPs: no shift between the characters.
        Assert.Equal(harness.LayoutInfo.ContentLeft + (3 * CharacterWidth), caret.X);
    }

    [Fact]
    public void Render_CursorNotVisible_DrawsNoCaret()
    {
        using var harness = new ViewHarness(Lines(10));

        RecordingRenderSurface surface = harness.Render(cursorVisible: false);

        Assert.Empty(CaretDraws(harness, surface));
    }

    [Fact]
    public void Render_Unfocused_DrawsNoCaret()
    {
        using var harness = new ViewHarness(Lines(10));

        RecordingRenderSurface surface = harness.Render(hasFocus: false);

        Assert.Empty(CaretDraws(harness, surface));
    }

    [Fact]
    public void Render_CaretLineScrolledOut_DrawsNoCaret()
    {
        using var harness = new ViewHarness(Lines(1000), height: 200);
        harness.ScrollTo(scrollTop: 5000);

        RecordingRenderSurface surface = harness.Render(Selection.Collapsed(new Position(1, 1)));

        Assert.Empty(CaretDraws(harness, surface));
    }
}

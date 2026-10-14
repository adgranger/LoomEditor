using System.Globalization;
using LoomEditor.Core.Text;
using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.View.Features;

namespace LoomEditor.Rendering.Tests;

public class TextViewRendererTests
{
    private const float CharacterWidth = (float)FakeTextMeasurer.CharacterWidth;
    private const float LineHeight = FakeTextMeasurer.LineHeight;

    [Fact]
    public void Render_Always_ClearsWithThemeBackgroundFirst()
    {
        var renderer = new TextViewRenderer();
        RecordingRenderSurface surface = Draw(renderer, "a", selection: null);

        Assert.Equal(new ClearCall(renderer.Theme.Background), surface.Calls[0]);
    }

    [Fact]
    public void Render_ThreeLines_DrawsRightAlignedLineNumbersAndTextAtLineHeightSteps()
    {
        var renderer = new TextViewRenderer();
        RecordingRenderSurface surface = Draw(renderer, "a\nb\nc", selection: null);

        // Gutter = (1 digit + 3) characters wide; numbers start after one character of padding.
        float textLeft = 4 * CharacterWidth;
        List<DrawLineLayoutCall> texts = [.. surface.Calls.OfType<DrawLineLayoutCall>()];
        Assert.Equal(6, texts.Count);
        for (int index = 0; index < 3; index++)
        {
            float y = index * LineHeight;
            Assert.Contains(
                new DrawLineLayoutCall((index + 1).ToString(CultureInfo.InvariantCulture), CharacterWidth, y, renderer.Theme.LineNumberForeground),
                texts);
            Assert.Contains(new DrawLineLayoutCall("abc"[index].ToString(), textLeft, y, renderer.Theme.Foreground), texts);
        }
    }

    [Fact]
    public void Render_TenLines_RightAlignsShortLineNumbers()
    {
        var renderer = new TextViewRenderer();
        RecordingRenderSurface surface = Draw(renderer, string.Join('\n', Enumerable.Repeat("x", 10)), selection: null);

        Assert.Contains(new DrawLineLayoutCall("1", 2 * CharacterWidth, 0, renderer.Theme.LineNumberForeground), surface.Calls);
        Assert.Contains(new DrawLineLayoutCall("10", CharacterWidth, 9 * LineHeight, renderer.Theme.LineNumberForeground), surface.Calls);
    }

    [Fact]
    public void Render_Always_DisposesEveryLayout()
    {
        var measurer = new FakeTextMeasurer();
        Draw(new TextViewRenderer(), "a\nb", selection: null, measurer);

        Assert.NotEmpty(measurer.Layouts);
        Assert.All(measurer.Layouts, layout => Assert.True(layout.IsDisposed));
    }

    [Fact]
    public void Render_EmptySelection_DrawsNoSelectionRectangle()
    {
        RecordingRenderSurface surface = Draw(new TextViewRenderer(), "abc\ndef", Selection.Collapsed(new Position(1, 2)));

        Assert.Empty(surface.Calls.OfType<FillRectangleCall>());
    }

    [Fact]
    public void Render_TwoLineSelection_DrawsTwoRectangles()
    {
        var renderer = new TextViewRenderer();
        RecordingRenderSurface surface = Draw(renderer, "abc\ndef", new Selection(new Position(1, 2), new Position(2, 3)));

        float textLeft = 4 * CharacterWidth;
        List<FillRectangleCall> rectangles = [.. surface.Calls.OfType<FillRectangleCall>()];
        Assert.Equal(2, rectangles.Count);
        // Line 1: from column 2 to the end of the line plus the selected line break (3 characters + 1).
        Assert.Equal(new FillRectangleCall(textLeft + 10, 0, 30, LineHeight, renderer.Theme.SelectionBackground), rectangles[0]);
        // Line 2: from column 1 to column 3.
        Assert.Equal(new FillRectangleCall(textLeft, LineHeight, 20, LineHeight, renderer.Theme.SelectionBackground), rectangles[1]);
    }

    [Fact]
    public void Render_SelectionAfterATab_StartsAtTheTabStop()
    {
        var renderer = new TextViewRenderer();
        RecordingRenderSurface surface = Draw(renderer, "\tab", new Selection(new Position(1, 2), new Position(1, 3)));

        // The tab (tab size 4, characters 10 wide) ends at x = 40; "a" is 10 wide.
        float textLeft = 4 * CharacterWidth;
        FillRectangleCall rectangle = Assert.Single(surface.Calls.OfType<FillRectangleCall>());
        Assert.Equal(new FillRectangleCall(textLeft + 40, 0, 10, LineHeight, renderer.Theme.SelectionBackground), rectangle);
    }

    [Fact]
    public void GetGutterWidth_ThousandLines_IsDigitsPlusPaddingTimesDigitWidth()
    {
        Assert.Equal((4 + 3) * 10f, TextViewRenderer.GetGutterWidth(1000, new FakeTextMeasurer().FontInfo));
    }

    private static RecordingRenderSurface Draw(
        TextViewRenderer renderer, string text, Selection? selection, FakeTextMeasurer? measurer = null)
    {
        var context = new EditorContext();
        context.SetText(text);
        if (selection is { } value)
        {
            context.Selection = value;
        }

        var surface = new RecordingRenderSurface();
        renderer.Render(surface, measurer ?? new FakeTextMeasurer(), context, viewportHeight: 1000);
        return surface;
    }
}

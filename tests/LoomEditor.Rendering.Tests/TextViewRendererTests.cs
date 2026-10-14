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
    public void Render_SameFortyLinesTwice_CreatesFortyLayoutsPlusTheirNumbers()
    {
        var measurer = new FakeTextMeasurer();
        using var renderer = new TextViewRenderer();
        string text = string.Join('\n', Enumerable.Range(0, 40).Select(i => "line text " + (char)('A' + (i % 26)) + i));

        Draw(renderer, text, selection: null, measurer);
        int afterFirstFrame = measurer.Layouts.Count;
        Draw(renderer, text, selection: null, measurer);

        // 40 texts + 40 line numbers ("1".."40"), created once: the second frame creates nothing.
        Assert.Equal(80, afterFirstFrame);
        Assert.Equal(afterFirstFrame, measurer.Layouts.Count);
    }

    [Fact]
    public void Dispose_AfterRendering_DisposesTheCachedLayouts()
    {
        var measurer = new FakeTextMeasurer();
        var renderer = new TextViewRenderer();
        Draw(renderer, "a\nb", selection: null, measurer);

        renderer.Dispose();

        Assert.NotEmpty(measurer.Layouts);
        Assert.All(measurer.Layouts, layout => Assert.True(layout.IsDisposed));
    }

    [Fact]
    public void Render_LongLine_DrawsTheFirst10000CharactersAndAShowMoreLabel()
    {
        var measurer = new FakeTextMeasurer();
        var renderer = new TextViewRenderer();
        RecordingRenderSurface surface = Draw(renderer, new string('x', 25_000), selection: null, measurer);

        float textLeft = 4 * CharacterWidth;
        List<DrawLineLayoutCall> texts = [.. surface.Calls.OfType<DrawLineLayoutCall>()];
        DrawLineLayoutCall line = Assert.Single(texts, call => call.X == textLeft && call.Text.StartsWith('x'));
        Assert.Equal(10_000, line.Text.Length);
        // The label is drawn right after the cut text.
        Assert.Contains(
            new DrawLineLayoutCall("Show more (14.6 KB)", textLeft + (10_000 * CharacterWidth), 0, renderer.Theme.LineNumberForeground),
            texts);
    }

    [Fact]
    public void Render_LongLineCutInsideASurrogatePair_DoesNotThrow()
    {
        // The cut at 10,000 falls between the two UTF-16 units of the emoji.
        string line = new string('x', 9_999) + "\U0001F600" + new string('y', 100);

        RecordingRenderSurface surface = Draw(new TextViewRenderer(), line, selection: null);

        Assert.Contains(surface.Calls.OfType<DrawLineLayoutCall>(), call => call.Text.StartsWith("Show more", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_SelectionBeyondTheCutOfALongLine_IsClampedToTheDrawnText()
    {
        var renderer = new TextViewRenderer();
        RecordingRenderSurface surface = Draw(
            renderer, new string('x', 25_000), new Selection(new Position(1, 5), new Position(1, 20_000)));

        float textLeft = 4 * CharacterWidth;
        FillRectangleCall rectangle = Assert.Single(surface.Calls.OfType<FillRectangleCall>());
        Assert.Equal(textLeft + (4 * CharacterWidth), rectangle.X);
        Assert.Equal((10_000 - 4) * CharacterWidth, rectangle.Width);
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

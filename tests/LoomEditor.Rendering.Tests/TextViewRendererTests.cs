using System.Globalization;
using LoomEditor.Core.Text;
using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.View.Features;

namespace LoomEditor.Rendering.Tests;

public class TextViewRendererTests
{
    private static readonly FontMetrics Metrics = new(10, 20);

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
        float textLeft = 4 * Metrics.CharacterWidth;
        List<DrawTextCall> texts = [.. surface.Calls.OfType<DrawTextCall>()];
        Assert.Equal(6, texts.Count);
        for (int index = 0; index < 3; index++)
        {
            float y = index * Metrics.LineHeight;
            Assert.Contains(new DrawTextCall((index + 1).ToString(CultureInfo.InvariantCulture), 10, y, renderer.Theme.LineNumberForeground), texts);
            Assert.Contains(new DrawTextCall("abc"[index].ToString(), textLeft, y, renderer.Theme.Foreground), texts);
        }
    }

    [Fact]
    public void Render_TenLines_RightAlignsShortLineNumbers()
    {
        var renderer = new TextViewRenderer();
        RecordingRenderSurface surface = Draw(renderer, string.Join('\n', Enumerable.Repeat("x", 10)), selection: null);

        Assert.Contains(new DrawTextCall("1", 20, 0, renderer.Theme.LineNumberForeground), surface.Calls);
        Assert.Contains(new DrawTextCall("10", 10, 9 * Metrics.LineHeight, renderer.Theme.LineNumberForeground), surface.Calls);
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

        float textLeft = 4 * Metrics.CharacterWidth;
        List<FillRectangleCall> rectangles = [.. surface.Calls.OfType<FillRectangleCall>()];
        Assert.Equal(2, rectangles.Count);
        // Line 1: from column 2 to column 5 (length 3 + 2: the selected line break counts as one extra character).
        Assert.Equal(new FillRectangleCall(textLeft + 10, 0, 30, 20, renderer.Theme.SelectionBackground), rectangles[0]);
        // Line 2: from column 1 to column 3.
        Assert.Equal(new FillRectangleCall(textLeft, 20, 20, 20, renderer.Theme.SelectionBackground), rectangles[1]);
    }

    [Fact]
    public void GetGutterWidth_ThousandLines_IsDigitsPlusPaddingTimesCharacterWidth()
    {
        Assert.Equal((4 + 3) * 10f, TextViewRenderer.GetGutterWidth(1000, new FontMetrics(10, 20)));
    }

    private static RecordingRenderSurface Draw(TextViewRenderer renderer, string text, Selection? selection)
    {
        var context = new EditorContext();
        context.SetText(text);
        if (selection is { } value)
        {
            context.Selection = value;
        }

        var surface = new RecordingRenderSurface(Metrics);
        renderer.Render(surface, context, viewportHeight: 1000);
        return surface;
    }
}

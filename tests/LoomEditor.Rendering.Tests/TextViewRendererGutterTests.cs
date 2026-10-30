using LoomEditor.Core.Decorations;
using LoomEditor.Core.Text;
using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.Themes;
using LoomEditor.View.Decorations;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the drawing of the gutter (it renders HTML): all tests are new.
public class TextViewRendererGutterTests
{
    private static string Lines(int count) => string.Join('\n', Enumerable.Range(1, count).Select(i => "L" + i));

    private static List<DrawLineLayoutCall> NumberDraws(RecordingRenderSurface surface) =>
        [.. surface.Calls.OfType<DrawLineLayoutCall>().Where(call => call.Text.Length > 0 && char.IsDigit(call.Text[0]))];

    [Fact]
    public void Render_Line7Active_DrawsItsNumberWithTheActiveColor()
    {
        using var harness = new ViewHarness(Lines(10));
        EditorTheme theme = harness.Renderer.Theme;

        List<DrawLineLayoutCall> numbers = NumberDraws(harness.Render(Selection.Collapsed(new Position(7, 1))));

        Assert.Equal(10, numbers.Count);
        Assert.Equal(theme.LineNumberActiveForeground, numbers.Single(call => call.Text == "7").Color);
        Assert.All(numbers.Where(call => call.Text != "7"), call => Assert.Equal(theme.LineNumberForeground, call.Color));
    }

    [Fact]
    public void Render_Always_RightAlignsTheLineNumbers()
    {
        using var harness = new ViewHarness(Lines(10));

        List<DrawLineLayoutCall> numbers = NumberDraws(harness.Render());

        float right = harness.LayoutInfo.LineNumbersLeft + harness.LayoutInfo.LineNumbersWidth;
        Assert.All(numbers, call => Assert.Equal(right, call.X + (call.Text.Length * (float)FakeTextMeasurer.CharacterWidth)));
    }

    [Fact]
    public void Render_TextEndingWithANewline_NumbersTheLastEmptyLine()
    {
        using var harness = new ViewHarness("a\n");

        List<DrawLineLayoutCall> numbers = NumberDraws(harness.Render());

        Assert.Equal(2, numbers.Count);
        Assert.Equal("1", numbers[0].Text);
        Assert.Equal("2", numbers[1].Text);
    }

    [Fact]
    public void Render_EmptySelection_DrawsTheLineHighlightBorder()
    {
        using var harness = new ViewHarness(Lines(10));
        EditorTheme theme = harness.Renderer.Theme;

        RecordingRenderSurface surface = harness.Render(Selection.Collapsed(new Position(3, 1)));

        DrawRectangleCall border = Assert.Single(surface.Calls.OfType<DrawRectangleCall>());
        Assert.Equal(theme.LineHighlightBorder, border.Color);
        Assert.Equal(2, border.StrokeWidth);
        // Inset by 1 so that the 2 DIP stroke stays inside the line box (line 3 is at y = 40).
        Assert.Equal(harness.LayoutInfo.ContentLeft + 1, border.X);
        Assert.Equal(41, border.Y);
        Assert.Equal(harness.LayoutInfo.Width - harness.LayoutInfo.ContentLeft - 2, border.Width);
        Assert.Equal(FakeTextMeasurer.LineHeight - 2, border.Height);
    }

    [Fact]
    public void Render_NonEmptySelection_DrawsNoLineHighlight()
    {
        using var harness = new ViewHarness(Lines(10));

        RecordingRenderSurface surface = harness.Render(new Selection(new Position(3, 1), new Position(3, 2)));

        Assert.Empty(surface.Calls.OfType<DrawRectangleCall>());
    }

    [Fact]
    public void Render_LineHighlightBackgroundSet_FillsItInsteadOfTheBorder()
    {
        using var harness = new ViewHarness(Lines(10));
        ThemeColor background = ThemeColor.Parse("#112233");
        harness.Renderer.Theme = EditorTheme.Dark with { LineHighlightBackground = background };

        RecordingRenderSurface surface = harness.Render(Selection.Collapsed(new Position(3, 1)));

        Assert.Empty(surface.Calls.OfType<DrawRectangleCall>());
        Assert.Contains(surface.Calls.OfType<FillRectangleCall>(), call => call.Color == background && call.Y == 40);
    }

    [Fact]
    public void Render_HighlightAll_AlsoDrawsTheMarginHighlight()
    {
        using var harness = new ViewHarness(Lines(10));
        harness.Renderer.RenderLineHighlight = RenderLineHighlight.All;

        RecordingRenderSurface surface = harness.Render(Selection.Collapsed(new Position(3, 1)));

        List<DrawRectangleCall> borders = [.. surface.Calls.OfType<DrawRectangleCall>()];
        Assert.Equal(2, borders.Count);
        Assert.Contains(borders, call => call.X == 1 && call.Width == harness.LayoutInfo.ContentLeft - 2);
    }

    [Fact]
    public void Render_DefaultHighlight_DoesNotDrawInTheMargin()
    {
        using var harness = new ViewHarness(Lines(10));

        RecordingRenderSurface surface = harness.Render(Selection.Collapsed(new Position(3, 1)));

        Assert.DoesNotContain(surface.Calls.OfType<DrawRectangleCall>(), call => call.X < harness.LayoutInfo.ContentLeft);
    }

    [Fact]
    public void Render_HostMarginClass_FillsTheGutterOfItsLine()
    {
        const string className = "test-host-margin";
        var background = ThemeColor.FromRgb(0x12, 0x34, 0x56);
        CustomDecorationStyles.Register(className, new CustomDecorationStyle { Background = background });
        try
        {
            using var harness = new ViewHarness(Lines(3));
            ModelDecorationOptions options = new EditorDecorationOptions { MarginClassName = className }.ToModelOptions();
            harness.Model.DeltaDecorations(harness.ViewModel.EditorId, [], new[] { new ModelDeltaDecoration(new Range(2, 1, 2, 1), options) });

            RecordingRenderSurface surface = harness.Render();

            FillRectangleCall fill = Assert.Single(surface.Calls.OfType<FillRectangleCall>(), call => call.Color == background);
            Assert.Equal(new FillRectangleCall(0, FakeTextMeasurer.LineHeight, harness.LayoutInfo.ContentLeft, FakeTextMeasurer.LineHeight, background), fill);
        }
        finally
        {
            CustomDecorationStyles.Unregister(className);
        }
    }
}

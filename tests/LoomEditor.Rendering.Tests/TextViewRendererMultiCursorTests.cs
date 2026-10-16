using LoomEditor.Core.Text;
using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.Themes;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the drawing of several cursors (it renders HTML): all tests are new.
public class TextViewRendererMultiCursorTests
{
    private static string Lines(int count) => string.Join('\n', Enumerable.Range(1, count).Select(i => "Lxxxxx" + i));

    private static List<FillRectangleCall> Fills(ViewHarness harness, RecordingRenderSurface surface, ThemeColor color) =>
        [.. surface.Calls.OfType<FillRectangleCall>().Where(call => call.Color == color)];

    [Fact]
    public void Render_TwoEmptySelections_DrawsTwoCaretsTwoHighlightsAndOneActiveNumber()
    {
        using var harness = new ViewHarness(Lines(10));
        EditorTheme theme = harness.Renderer.Theme;

        RecordingRenderSurface surface = harness.Render([Selection.Collapsed(new Position(2, 1)), Selection.Collapsed(new Position(5, 3))]);

        List<FillRectangleCall> carets = Fills(harness, surface, theme.CursorForeground);
        Assert.Equal(2, carets.Count);
        Assert.Equal(FakeTextMeasurer.LineHeight, carets[0].Y);
        Assert.Equal(4 * FakeTextMeasurer.LineHeight, carets[1].Y);
        Assert.Equal(2, surface.Calls.OfType<DrawRectangleCall>().Count(call => call.Color == theme.LineHighlightBorder));
        List<DrawLineLayoutCall> numbers =
            [.. surface.Calls.OfType<DrawLineLayoutCall>().Where(call => call.Text.Length > 0 && char.IsDigit(call.Text[0]))];
        Assert.Equal("2", Assert.Single(numbers, call => call.Color == theme.LineNumberActiveForeground).Text);
    }

    [Fact]
    public void Render_EmptyAndNonEmptySelection_DrawsNoLineHighlightTwoCaretsAndOneSelection()
    {
        using var harness = new ViewHarness(Lines(10));
        EditorTheme theme = harness.Renderer.Theme;

        RecordingRenderSurface surface = harness.Render([Selection.Collapsed(new Position(2, 1)), new Selection(new Position(5, 1), new Position(5, 4))]);

        Assert.Empty(surface.Calls.OfType<DrawRectangleCall>());
        Assert.Equal(2, Fills(harness, surface, theme.CursorForeground).Count);
        Assert.Single(Fills(harness, surface, theme.SelectionBackground));
    }

    [Fact]
    public void Render_TwoCursorsOnTheSameLine_DrawsOneHighlightAndTwoCarets()
    {
        using var harness = new ViewHarness(Lines(10));
        EditorTheme theme = harness.Renderer.Theme;

        RecordingRenderSurface surface = harness.Render([Selection.Collapsed(new Position(3, 1)), Selection.Collapsed(new Position(3, 5))]);

        Assert.Single(surface.Calls.OfType<DrawRectangleCall>(), call => call.Color == theme.LineHighlightBorder);
        Assert.Equal(2, Fills(harness, surface, theme.CursorForeground).Count);
    }

    [Fact]
    public void Render_NoSelections_Throws()
    {
        using var harness = new ViewHarness(Lines(3));

        Assert.Throws<ArgumentOutOfRangeException>(() => harness.Render(Array.Empty<Selection>()));
    }
}

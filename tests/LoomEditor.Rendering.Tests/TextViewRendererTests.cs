using LoomEditor.Core.Text;
using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the drawing of the lines (it renders HTML): all tests are new.
public class TextViewRendererTests
{
    private const float CharacterWidth = (float)FakeTextMeasurer.CharacterWidth;
    private const float LineHeight = FakeTextMeasurer.LineHeight;

    private static string Lines(int count, Func<int, string> line) =>
        string.Join('\n', Enumerable.Range(1, count).Select(line));

    // Text lines are "L<n>..."; line numbers are digits only.
    private static List<DrawLineLayoutCall> TextDraws(RecordingRenderSurface surface) =>
        [.. surface.Calls.OfType<DrawLineLayoutCall>().Where(call => call.Text.StartsWith('L'))];

    [Fact]
    public void Render_Always_ClearsWithThemeBackgroundFirst()
    {
        using var harness = new ViewHarness("a");

        RecordingRenderSurface surface = harness.Render();

        Assert.Equal(new ClearCall(harness.Renderer.Theme.Background), surface.Calls[0]);
    }

    [Fact]
    public void Render_ThousandLinesInA200HighViewport_DrawsLines1To10AtLineHeightSteps()
    {
        using var harness = new ViewHarness(Lines(1000, i => "L" + i), height: 200);

        List<DrawLineLayoutCall> texts = TextDraws(harness.Render());

        Assert.Equal(Enumerable.Range(0, 10).Select(i => i * LineHeight), texts.Select(call => call.LineTop));
        Assert.Equal("L1", texts[0].Text);
        Assert.Equal("L10", texts[^1].Text);
        Assert.All(texts, call => Assert.Equal(harness.LayoutInfo.ContentLeft, call.X));
    }

    [Fact]
    public void Render_ScrolledBy10_DrawsTheFirstLineAboveTheViewportAndTheNextOneBelow()
    {
        using var harness = new ViewHarness(Lines(1000, i => "L" + i), height: 200);
        harness.ScrollTo(scrollTop: 10);

        List<DrawLineLayoutCall> texts = TextDraws(harness.Render());

        Assert.Equal(-10, texts[0].LineTop);
        Assert.Equal("L1", texts[0].Text);
        Assert.Equal(190, texts[^1].LineTop);
        Assert.Equal("L11", texts[^1].Text);
    }

    [Fact]
    public void Render_NearTheEndOfA2500000LineModel_DrawsEveryLineInsideTheViewport()
    {
        using var harness = new ViewHarness(Lines(2_500_000, _ => "Lx"), height: 200);
        harness.ScrollTo(scrollTop: (2_500_000 - 12) * (double)LineHeight);
        Assert.True(harness.ViewModel.ViewLayout.GetCurrentScrollTop() > 40_000_000);

        List<DrawLineLayoutCall> texts = TextDraws(harness.Render());

        Assert.NotEmpty(texts);
        Assert.All(texts, call => Assert.InRange(call.LineTop, -LineHeight, 200));
    }

    [Fact]
    public void Render_ScrolledLeftBy30_ShiftsTheTextAndDrawsTheGutterAfterIt()
    {
        using var harness = new ViewHarness(Lines(3, i => "L" + new string('x', 100)), width: 500, height: 200);
        harness.Render(); // reports the width of the lines, so that there is room to scroll
        harness.ScrollTo(scrollLeft: 30);

        RecordingRenderSurface surface = harness.Render();

        int contentLeft = harness.LayoutInfo.ContentLeft;
        List<DrawLineLayoutCall> texts = TextDraws(surface);
        Assert.All(texts, call => Assert.Equal(contentLeft - 30, call.X));
        int lastText = surface.Calls.FindLastIndex(call => call is DrawLineLayoutCall { Text: [ 'L', ..] });
        int gutter = surface.Calls.FindIndex(call => call is FillRectangleCall { X: 0, Y: 0 } rectangle && rectangle.Width == contentLeft);
        Assert.True(gutter > lastText, "the gutter background must be drawn after the text");
    }

    [Fact]
    public void Render_LineNumbers_AreRightAlignedInTheGutter()
    {
        using var harness = new ViewHarness(Lines(10, i => "L" + i), height: 400);

        RecordingRenderSurface surface = harness.Render();

        float right = harness.LayoutInfo.LineNumbersLeft + harness.LayoutInfo.LineNumbersWidth;
        var theme = harness.Renderer.Theme;
        Assert.Contains(new DrawLineLayoutCall("1", right - CharacterWidth, 0, theme.LineNumberActiveForeground), surface.Calls);
        Assert.Contains(new DrawLineLayoutCall("10", right - (2 * CharacterWidth), 9 * LineHeight, theme.LineNumberForeground), surface.Calls);
    }

    [Fact]
    public void Render_EmptySelection_DrawsNoSelectionRectangle()
    {
        using var harness = new ViewHarness("Labc\nLdef");

        RecordingRenderSurface surface = harness.Render(Selection.Collapsed(new Position(1, 2)));

        // The other rectangles are the gutter background and the caret: none has the selection color.
        Assert.DoesNotContain(surface.Calls.OfType<FillRectangleCall>(), call => call.Color == harness.Renderer.Theme.SelectionBackground);
    }

    [Fact]
    public void Render_SelectionAcrossThreeLines_DrawsThreeRectanglesWithRealWidths()
    {
        using var harness = new ViewHarness("abc\ndefg\nhi");
        var theme = harness.Renderer.Theme;

        RecordingRenderSurface surface = harness.Render(new Selection(new Position(1, 2), new Position(3, 2)));

        int left = harness.LayoutInfo.ContentLeft;
        List<FillRectangleCall> rectangles = [.. surface.Calls.OfType<FillRectangleCall>().Where(r => r.Color == theme.SelectionBackground)];
        // A selected line break adds one space (10 wide).
        Assert.Equal(
            [
                new FillRectangleCall(left + 10, 0, 30, LineHeight, theme.SelectionBackground),
                new FillRectangleCall(left, LineHeight, 50, LineHeight, theme.SelectionBackground),
                new FillRectangleCall(left, 2 * LineHeight, 10, LineHeight, theme.SelectionBackground),
            ],
            rectangles);
    }

    [Fact]
    public void Render_SelectionAfterATab_StartsAtTheTabStop()
    {
        using var harness = new ViewHarness("\tab");
        var theme = harness.Renderer.Theme;

        RecordingRenderSurface surface = harness.Render(new Selection(new Position(1, 2), new Position(1, 3)));

        // The tab (tab size 4, characters 10 wide) ends at x = 40; "a" is 10 wide.
        FillRectangleCall rectangle = Assert.Single(surface.Calls.OfType<FillRectangleCall>(), r => r.Color == theme.SelectionBackground);
        Assert.Equal(new FillRectangleCall(harness.LayoutInfo.ContentLeft + 40, 0, 10, LineHeight, theme.SelectionBackground), rectangle);
    }

    [Fact]
    public void Render_SameFortyLinesTwice_CreatesFortyLayoutsPlusTheirNumbers()
    {
        using var harness = new ViewHarness(Lines(40, i => "line text " + i), height: 800);

        harness.Render();
        int afterFirstFrame = harness.Measurer.Layouts.Count;
        harness.Render();

        // 40 texts + 40 line numbers, created once: the second frame creates nothing.
        Assert.Equal(80, afterFirstFrame);
        Assert.Equal(afterFirstFrame, harness.Measurer.Layouts.Count);
    }

    [Fact]
    public void Dispose_AfterRendering_DisposesTheCachedLayouts()
    {
        var harness = new ViewHarness("a\nb");
        harness.Render();

        harness.Dispose();

        Assert.NotEmpty(harness.Measurer.Layouts);
        Assert.All(harness.Measurer.Layouts, layout => Assert.True(layout.IsDisposed));
    }

    [Fact]
    public void Render_LongLine_DrawsTheFirst10000CharactersAndAShowMoreLabel()
    {
        using var harness = new ViewHarness("L" + new string('x', 24_999));

        RecordingRenderSurface surface = harness.Render();

        int left = harness.LayoutInfo.ContentLeft;
        DrawLineLayoutCall line = Assert.Single(TextDraws(surface));
        Assert.Equal(10_000, line.Text.Length);
        // The label (for 15,000 characters) is drawn right after the cut text.
        Assert.Contains(
            new DrawLineLayoutCall("Show more (14.6 KB)", left + (10_000 * CharacterWidth), 0, harness.Renderer.Theme.LineNumberForeground),
            surface.Calls);
    }

    [Fact]
    public void Render_LongLineCutInsideASurrogatePair_DoesNotThrow()
    {
        // The cut at 10,000 falls between the two UTF-16 units of the emoji.
        using var harness = new ViewHarness("L" + new string('x', 9_998) + "\U0001F600" + new string('y', 100));

        RecordingRenderSurface surface = harness.Render();

        Assert.Contains(surface.Calls.OfType<DrawLineLayoutCall>(), call => call.Text.StartsWith("Show more", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_SelectionBeyondTheCutOfALongLine_IsClampedToTheDrawnText()
    {
        using var harness = new ViewHarness(new string('x', 25_000));
        var theme = harness.Renderer.Theme;

        RecordingRenderSurface surface = harness.Render(new Selection(new Position(1, 5), new Position(1, 20_000)));

        FillRectangleCall rectangle = Assert.Single(surface.Calls.OfType<FillRectangleCall>(), r => r.Color == theme.SelectionBackground);
        Assert.Equal(harness.LayoutInfo.ContentLeft + (4 * CharacterWidth), rectangle.X);
        Assert.Equal((10_000 - 4) * CharacterWidth, rectangle.Width);
    }

    [Fact]
    public void Render_AWideLineInAShownPartOfTheDocument_RaisesTheContentWidthOnce()
    {
        using var harness = new ViewHarness(Lines(1000, i => i == 3 ? "L" + new string('x', 29) : "L"), height: 200);
        var changes = new List<ContentSizeChangedEventArgs>();
        harness.ViewModel.ViewLayout.ContentSizeChanged += (_, e) => changes.Add(e);

        harness.Render();
        int afterFirstFrame = changes.Count(e => e.ContentWidthChanged);
        harness.Render();

        Assert.Equal(1, afterFirstFrame);
        Assert.Equal(afterFirstFrame, changes.Count(e => e.ContentWidthChanged));
        // 300 DIPs for the widest line, plus Monaco's extra space and the scrollbar.
        Assert.True(harness.ViewModel.ViewLayout.GetContentWidth() >= 300);
    }

    [Fact]
    public void Render_EveryLineVisibleAndTheWidestLineGetsShorter_ShrinksTheContentWidth()
    {
        using var harness = new ViewHarness("L" + new string('x', 59) + "\nLy", height: 200);
        harness.Render();
        int wideContentWidth = harness.ViewModel.ViewLayout.GetContentWidth();

        harness.Model.ApplyEdits(new[] { new TextEdit(new Range(1, 6, 1, 61), string.Empty) });
        harness.Render();

        Assert.True(harness.ViewModel.ViewLayout.GetContentWidth() < wideContentWidth);
    }

    [Fact]
    public void Render_ResetMaxLineWidth_LetsTheWidthShrinkEvenWhenNotEveryLineIsVisible()
    {
        using var harness = new ViewHarness(Lines(1000, i => i == 3 ? "L" + new string('x', 59) : "L"), height: 200);
        harness.Render();
        int wideContentWidth = harness.ViewModel.ViewLayout.GetContentWidth();

        harness.Model.ApplyEdits(new[] { new TextEdit(new Range(3, 6, 3, 61), string.Empty) });
        harness.Render();
        Assert.Equal(wideContentWidth, harness.ViewModel.ViewLayout.GetContentWidth()); // it only grows

        harness.Renderer.ResetMaxLineWidth();
        harness.Render();
        Assert.True(harness.ViewModel.ViewLayout.GetContentWidth() < wideContentWidth);
    }
}

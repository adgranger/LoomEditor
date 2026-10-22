using LoomEditor.Themes;
using LoomEditor.View.Layout;
using LoomEditor.View.Minimap;

namespace LoomEditor.View.Tests.Minimap;

public class MinimapLayoutTests
{
    private const int Height = 600;

    private static MinimapOptions CreateOptions(bool heightIsEditorHeight, bool scrollBeyondLastLine = false)
    {
        var layout = new EditorLayoutInfo(
            Width: 800, Height: Height, GlyphMarginLeft: 0, GlyphMarginWidth: 0, GlyphMarginDecorationLaneCount: 1,
            LineNumbersLeft: 0, LineNumbersWidth: 42, DecorationsLeft: 42, DecorationsWidth: 10, ContentLeft: 52, ContentWidth: 634,
            Minimap: new EditorMinimapLayoutInfo(RenderMinimap.Text, 686, 100, heightIsEditorHeight, false, 1, 2, 100, Height, 100, Height),
            ViewportColumn: 90, IsViewportWrapping: false, WrappingColumn: -1, VerticalScrollbarWidth: 14,
            HorizontalScrollbarHeight: 12, OverviewRuler: new OverviewRulerPosition(14, Height, 0, 0));
        var theme = EditorTheme.Dark;
        return new MinimapOptions(
            layout, new EditorMinimapOptions(), FontInfo.Default, scrollBeyondLastLine, theme, new MinimapTokensColorTracker(theme),
            (scale, family) => MinimapCharRendererFactory.Create(scale, family, null));
    }

    private static MinimapLayout Create(MinimapOptions options, int lineCount, double scrollTop, MinimapLayout? previous = null)
    {
        double scrollHeight = lineCount * (double)options.LineHeight;
        int viewportStart = (int)(scrollTop / options.LineHeight) + 1;
        int viewportEnd = Math.Min(lineCount, viewportStart + (Height / options.LineHeight));
        return MinimapLayout.Create(
            options, viewportStart, viewportEnd, (viewportStart - 1) * (double)options.LineHeight, Height, false,
            lineCount, lineCount, scrollTop, scrollHeight, previous);
    }

    [Fact]
    public void Create_EditorHeightMode_SliderCoversTheViewportRatio()
    {
        MinimapOptions options = CreateOptions(heightIsEditorHeight: true);

        MinimapLayout layout = Create(options, lineCount: 300, scrollTop: 0);

        Assert.Equal(Math.Floor(Height * (double)Height / (300 * options.LineHeight)), layout.SliderHeight);
        Assert.True(layout.SliderNeeded);
        Assert.Equal(1, layout.StartLineNumber);
        Assert.Equal(300, layout.EndLineNumber);
    }

    [Fact]
    public void Create_Proportional_DocumentShorterThanMinimap_NoSampling_SliderAtScrollTop()
    {
        MinimapOptions options = CreateOptions(heightIsEditorHeight: false);
        double scrollTop = 10 * options.LineHeight;

        MinimapLayout layout = Create(options, lineCount: 200, scrollTop: scrollTop);

        double sliderHeight = Math.Floor((double)Height / options.LineHeight * options.MinimapLineHeight / options.PixelRatio);
        double maxTop = Math.Min(Height - sliderHeight, (200 * options.MinimapLineHeight / options.PixelRatio) - sliderHeight);
        double ratio = maxTop / ((200 * options.LineHeight) - Height);
        Assert.Equal(1, layout.StartLineNumber);
        Assert.Equal(200, layout.EndLineNumber);
        Assert.Equal(0, layout.TopPaddingLineCount);
        Assert.Equal(sliderHeight, layout.SliderHeight);
        Assert.Equal(scrollTop * ratio, layout.SliderTop, 6);
    }

    [Fact]
    public void Create_Proportional_LongDocument_StartLineFollowsTheScroll()
    {
        MinimapOptions options = CreateOptions(heightIsEditorHeight: false);

        MinimapLayout atTop = Create(options, lineCount: 5000, scrollTop: 0);
        MinimapLayout middle = Create(options, lineCount: 5000, scrollTop: 2000 * options.LineHeight);
        MinimapLayout further = Create(options, lineCount: 5000, scrollTop: 3000 * options.LineHeight);

        Assert.Equal(1, atTop.StartLineNumber);
        Assert.True(middle.StartLineNumber > atTop.StartLineNumber);
        Assert.True(further.StartLineNumber > middle.StartLineNumber);
        Assert.Equal(middle.StartLineNumber + 300 - 1, middle.EndLineNumber);
        Assert.True(middle.SliderNeeded);
    }

    [Fact]
    public void Create_WithPreviousLayout_KeepsTheStartLineWhenPossible()
    {
        MinimapOptions options = CreateOptions(heightIsEditorHeight: false);
        double scrollTop = 2000 * options.LineHeight;
        double scrollHeight = 5000 * (double)options.LineHeight;
        MinimapLayout fresh = Create(options, lineCount: 5000, scrollTop: scrollTop);
        // Scrolled down since the previous layout, which started further down: the start line must not decrease.
        var previous = new MinimapLayout(scrollTop - 1, scrollHeight, true, 1, 0, 10, 0, fresh.StartLineNumber + 5, fresh.EndLineNumber + 5);

        MinimapLayout layout = Create(options, lineCount: 5000, scrollTop: scrollTop, previous: previous);

        Assert.Equal(fresh.StartLineNumber + 5, layout.StartLineNumber);
        Assert.Equal(fresh.StartLineNumber, Create(options, 5000, scrollTop, new MinimapLayout(scrollTop - 1, scrollHeight + 1, true, 1, 0, 10, 0, 4000, 4300)).StartLineNumber);
    }

    [Fact]
    public void GetDesiredScrollTopFromDelta_InvertsTheSliderRatio()
    {
        MinimapOptions options = CreateOptions(heightIsEditorHeight: true);
        MinimapLayout layout = Create(options, lineCount: 300, scrollTop: 1000);

        Assert.Equal(1000, layout.GetDesiredScrollTopFromDelta(0));
        // Moving the slider by its whole offset from the top doubles the scroll position.
        Assert.Equal(2000, layout.GetDesiredScrollTopFromDelta(layout.SliderTop), 1);
    }

    [Fact]
    public void IntersectWithViewport_OutsideReturnsNull()
    {
        MinimapOptions options = CreateOptions(heightIsEditorHeight: true);
        MinimapLayout layout = Create(options, lineCount: 100, scrollTop: 0);

        Assert.Null(layout.IntersectWithViewport(new Range(200, 1, 210, 1)));
        Assert.Equal((50, 100), layout.IntersectWithViewport(new Range(50, 1, 500, 1)));
    }
}

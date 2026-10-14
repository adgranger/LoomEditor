using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Layout.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

// Monaco has no viewLayout.test.ts: all tests are new.
public class ViewLayoutTests
{
    private const int LineHeight = 20;
    private const int ViewportHeight = 500;

    private readonly FakeTimeProvider _clock = new();
    private readonly FakeFrameScheduler _frames = new();

    private static EditorLayoutInfo CreateLayoutInfo(int contentWidth = 600, int height = ViewportHeight)
    {
        return new EditorLayoutInfo(
            Width: contentWidth + 52,
            Height: height,
            GlyphMarginLeft: 0,
            GlyphMarginWidth: 0,
            GlyphMarginDecorationLaneCount: 1,
            LineNumbersLeft: 0,
            LineNumbersWidth: 42,
            DecorationsLeft: 42,
            DecorationsWidth: 10,
            ContentLeft: 52,
            ContentWidth: contentWidth,
            ViewportColumn: 70,
            VerticalScrollbarWidth: 14,
            HorizontalScrollbarHeight: 12,
            OverviewRuler: new OverviewRulerPosition(14, height, 0, 0));
    }

    private static FontInfo CreateFontInfo()
    {
        return new FontInfo(1, "Consolas", "normal", 14, LineHeight, 0, true, 8, 16, true, 8, 8, 8, 8);
    }

    private ViewLayout CreateViewLayout(int lineCount, ViewLayoutOptions? options = null)
    {
        return new ViewLayout(
            options ?? new ViewLayoutOptions(LineHeight),
            CreateLayoutInfo(),
            CreateFontInfo(),
            lineCount,
            [],
            _frames.Schedule,
            _clock);
    }

    [Theory]
    [InlineData(true, 2000 + ViewportHeight - LineHeight)]
    [InlineData(false, 2000)]
    public void GetContentHeight_ScrollBeyondLastLine_AddsViewportMinusOneLine(bool scrollBeyondLastLine, int expected)
    {
        using var layout = CreateViewLayout(100, new ViewLayoutOptions(LineHeight, ScrollBeyondLastLine: scrollBeyondLastLine));

        Assert.Equal(expected, layout.GetContentHeight());
    }

    [Fact]
    public void SetScrollPosition_BeyondEnd_ClampsToScrollHeightMinusHeight()
    {
        using var layout = CreateViewLayout(100);

        layout.SetScrollPosition(new NewScrollPosition(ScrollTop: 1e9), ScrollType.Immediate);

        Assert.Equal(layout.GetScrollHeight() - ViewportHeight, layout.GetCurrentScrollTop());
    }

    [Fact]
    public void SetMaxLineWidth_WiderThanContentArea_AddsExtraSpaceAndScrollbarAndRaisesOnce()
    {
        using var layout = CreateViewLayout(100);
        var events = new List<ContentSizeChangedEventArgs>();
        layout.ContentSizeChanged += (_, e) => events.Add(e);

        layout.SetMaxLineWidth(800);

        // 800 + 4 * typicalHalfwidthCharacterWidth (8) + verticalScrollbarWidth (14)
        Assert.Equal(846, layout.GetContentWidth());
        Assert.Equal(846, layout.GetScrollWidth());
        var e = Assert.Single(events);
        Assert.True(e.ContentWidthChanged);
        Assert.False(e.ContentHeightChanged);
    }

    [Fact]
    public void OnLinesInsertedAndDeleted_AfterHeightMaybeChanged_UpdateContentHeight()
    {
        using var layout = CreateViewLayout(100, new ViewLayoutOptions(LineHeight, ScrollBeyondLastLine: false));

        layout.OnLinesInserted(101, 110);
        layout.OnHeightMaybeChanged();
        Assert.Equal(110 * LineHeight, layout.GetContentHeight());

        layout.OnLinesDeleted(1, 20);
        layout.OnHeightMaybeChanged();
        Assert.Equal(90 * LineHeight, layout.GetContentHeight());
    }

    [Fact]
    public void GetLinesViewportData_AfterScrollingToLine1000_StartsAtLine1000()
    {
        using var layout = CreateViewLayout(5000);

        layout.SetScrollPosition(
            new NewScrollPosition(ScrollTop: layout.GetVerticalOffsetForLineNumber(1000)), ScrollType.Immediate);
        PartialViewLinesViewportData data = layout.GetLinesViewportData();

        Assert.Equal(1000, data.StartLineNumber);
        Assert.Equal(1000, data.CompletelyVisibleStartLineNumber);
    }

    [Fact]
    public void SetScrollPosition_Smooth_AnimatesWithFakeSchedulerAndClock()
    {
        using var layout = CreateViewLayout(5000, new ViewLayoutOptions(LineHeight, SmoothScrolling: true));

        layout.SetScrollPosition(new NewScrollPosition(ScrollTop: 1000), ScrollType.Smooth);

        Assert.True(layout.HasPendingScrollAnimation);
        Assert.Equal(0, layout.GetCurrentScrollTop());
        Assert.Equal(1000, layout.GetFutureViewport().Top);

        _clock.Advance(50);
        _frames.RunFrame();
        Assert.InRange(layout.GetCurrentScrollTop(), 1, 999);
        Assert.True(layout.HasPendingScrollAnimation);

        _clock.Advance(200);
        _frames.RunFrame();
        Assert.Equal(1000, layout.GetCurrentScrollTop());
        Assert.False(layout.HasPendingScrollAnimation);
    }

    [Fact]
    public void SetScrollPosition_SmoothWithSmoothScrollingOff_ScrollsImmediately()
    {
        using var layout = CreateViewLayout(5000);

        layout.SetScrollPosition(new NewScrollPosition(ScrollTop: 1000), ScrollType.Smooth);

        Assert.False(layout.HasPendingScrollAnimation);
        Assert.Equal(1000, layout.GetCurrentScrollTop());
    }

    [Fact]
    public void OnOptionsChanged_NewLineHeight_UpdatesOffsetsAndContentHeight()
    {
        using var layout = CreateViewLayout(100, new ViewLayoutOptions(LineHeight, ScrollBeyondLastLine: false));

        layout.OnOptionsChanged(new ViewLayoutOptions(30, ScrollBeyondLastLine: false));

        Assert.Equal(3000, layout.GetContentHeight());
        Assert.Equal(30, layout.GetLineHeightForLineNumber(1));
    }

    [Fact]
    public void OnLayoutInfoChanged_NewSize_UpdatesViewport()
    {
        using var layout = CreateViewLayout(100);

        layout.OnLayoutInfoChanged(CreateLayoutInfo(contentWidth: 400, height: 300));

        Viewport viewport = layout.GetCurrentViewport();
        Assert.Equal(400, viewport.Width);
        Assert.Equal(300, viewport.Height);
        Assert.Equal(2000 + 300 - LineHeight, layout.GetContentHeight());
    }

    [Fact]
    public void SaveState_AfterScrolling_ReturnsScrollPositions()
    {
        using var layout = CreateViewLayout(5000);
        layout.SetScrollPosition(new NewScrollPosition(ScrollLeft: 0, ScrollTop: 400), ScrollType.Immediate);

        ViewLayoutState state = layout.SaveState();

        Assert.Equal(400, state.ScrollTop);
        Assert.Equal(400, state.ScrollTopWithoutViewZones);
        Assert.Equal(0, state.ScrollLeft);
    }

    [Fact]
    public void DeltaScrollNow_AddsToCurrentPosition()
    {
        using var layout = CreateViewLayout(5000);
        layout.SetScrollPosition(new NewScrollPosition(ScrollTop: 100), ScrollType.Immediate);

        layout.DeltaScrollNow(0, 50);

        Assert.Equal(150, layout.GetCurrentScrollTop());
    }
}

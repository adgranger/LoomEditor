using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Layout.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

// Monaco has no test for Scrollable: all tests are new.
public class ScrollableTests
{
    private readonly FakeTimeProvider _clock = new();
    private readonly FakeFrameScheduler _frames = new();

    private Scrollable CreateScrollable(int smoothScrollDuration = 100)
    {
        var scrollable = new Scrollable(new ScrollableOptions(true, smoothScrollDuration, _frames.Schedule, _clock));
        scrollable.SetScrollDimensions(new NewScrollDimensions(100, 100, 100, 1000), true);
        return scrollable;
    }

    [Fact]
    public void SetScrollPositionNow_NewPosition_RaisesOneEventNotSmooth()
    {
        using var scrollable = CreateScrollable();
        var events = new List<ScrollEvent>();
        scrollable.Scroll += (_, e) => events.Add(e);

        scrollable.SetScrollPositionNow(new NewScrollPosition(ScrollTop: 50));
        scrollable.SetScrollPositionNow(new NewScrollPosition(ScrollTop: 50));

        var e = Assert.Single(events);
        Assert.False(e.InSmoothScrolling);
        Assert.True(e.ScrollTopChanged);
        Assert.Equal(50, scrollable.GetCurrentScrollPosition().ScrollTop);
    }

    [Fact]
    public void SetScrollPositionSmooth_ZeroDuration_BehavesLikeNow()
    {
        using var scrollable = CreateScrollable(0);
        var events = new List<ScrollEvent>();
        scrollable.Scroll += (_, e) => events.Add(e);

        scrollable.SetScrollPositionSmooth(new NewScrollPosition(ScrollTop: 50));

        Assert.False(scrollable.HasPendingScrollAnimation);
        Assert.Equal(0, _frames.PendingCount);
        Assert.Equal(50, scrollable.GetCurrentScrollPosition().ScrollTop);
        Assert.False(Assert.Single(events).InSmoothScrolling);
    }

    [Fact]
    public void SetScrollPositionSmooth_FramesAdvance_ReachesTargetInLastFrame()
    {
        using var scrollable = CreateScrollable();
        var events = new List<ScrollEvent>();
        scrollable.Scroll += (_, e) => events.Add(e);

        scrollable.SetScrollPositionSmooth(new NewScrollPosition(ScrollTop: 400));

        Assert.True(scrollable.HasPendingScrollAnimation);
        Assert.Equal(400, scrollable.GetFutureScrollPosition().ScrollTop);

        _clock.Advance(50);
        _frames.RunFrame();
        var middle = scrollable.GetCurrentScrollPosition().ScrollTop;
        Assert.InRange(middle, 1, 399);
        Assert.True(scrollable.HasPendingScrollAnimation);

        _clock.Advance(200);
        _frames.RunFrame();

        Assert.Equal(400, scrollable.GetCurrentScrollPosition().ScrollTop);
        Assert.False(scrollable.HasPendingScrollAnimation);
        Assert.Equal(0, _frames.PendingCount);
        Assert.NotEmpty(events);
        Assert.All(events, e => Assert.True(e.InSmoothScrolling));
    }

    [Fact]
    public void SetScrollDimensions_DuringAnimation_ClampsTarget()
    {
        using var scrollable = CreateScrollable();
        scrollable.SetScrollPositionSmooth(new NewScrollPosition(ScrollTop: 800));
        Assert.Equal(800, scrollable.GetFutureScrollPosition().ScrollTop);

        scrollable.SetScrollDimensions(new NewScrollDimensions(ScrollHeight: 500), false);

        Assert.Equal(400, scrollable.GetFutureScrollPosition().ScrollTop);
    }

    [Fact]
    public void Dispose_DuringAnimation_DisposesScheduledFrame()
    {
        var scrollable = CreateScrollable();
        scrollable.SetScrollPositionSmooth(new NewScrollPosition(ScrollTop: 400));
        Assert.Equal(1, _frames.PendingCount);

        scrollable.Dispose();

        Assert.Equal(0, _frames.PendingCount);
        Assert.Equal(1, _frames.DisposedCount);
        Assert.False(scrollable.HasPendingScrollAnimation);
    }
}

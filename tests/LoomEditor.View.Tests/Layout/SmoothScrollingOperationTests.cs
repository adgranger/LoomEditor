using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

public class SmoothScrollingOperationTests
{
    private const int ViewportHeight = 800;
    private const int AnimationDuration = 125;
    private const int LineHeight = 20;

    private static readonly int[] SampleTimes = [0, 25, 50, 75, 100, 125];

    // Monaco: 'scroll 25 lines (40 fit)'
    [Fact]
    public void Tick_Scroll25Lines_MatchesMonaco()
    {
        AssertSmoothScroll(0, 500, [(5, 46), (14, 55), (20, 61), (23, 64), (24, 65), (25, 65)]);
    }

    // Monaco: 'scroll 75 lines (40 fit)'
    [Fact]
    public void Tick_Scroll75Lines_MatchesMonaco()
    {
        AssertSmoothScroll(0, 1500, [(15, 56), (44, 85), (62, 103), (71, 112), (74, 115), (75, 115)]);
    }

    // Monaco: 'scroll 100 lines (40 fit)'
    [Fact]
    public void Tick_Scroll100Lines_MatchesMonaco()
    {
        AssertSmoothScroll(0, 2000, [(20, 61), (59, 100), (82, 123), (94, 135), (99, 140), (100, 140)]);
    }

    // Monaco: 'scroll 125 lines (40 fit)'
    [Fact]
    public void Tick_Scroll125Lines_MatchesMonaco()
    {
        AssertSmoothScroll(0, 2500, [(16, 57), (29, 70), (107, 148), (119, 160), (124, 165), (125, 165)]);
    }

    // Monaco: 'scroll 500 lines (40 fit)'
    [Fact]
    public void Tick_Scroll500Lines_MatchesMonaco()
    {
        AssertSmoothScroll(0, 10000, [(16, 57), (29, 70), (482, 523), (494, 535), (499, 540), (500, 540)]);
    }

    // Monaco: TestSmoothScrollingOperation (applies the same +10 / -10 adjustments as SmoothScrollingOperation.start).
    private static SmoothScrollingOperation CreateOperation(double from, double to, double viewportSize, double startTime, double duration)
    {
        duration += 10;
        startTime -= 10;

        return new SmoothScrollingOperation(
            new SmoothScrollPosition(0, from, 0, viewportSize),
            new SmoothScrollPosition(0, to, 0, viewportSize),
            startTime,
            duration);
    }

    private static (int Start, int End) ExtractLines(SmoothScrollingOperation operation, double now)
    {
        var scrollTop = operation.Tick(now).ScrollTop;
        var scrollBottom = scrollTop + ViewportHeight;

        return ((int)Math.Floor(scrollTop / LineHeight), (int)Math.Ceiling(scrollBottom / LineHeight));
    }

    private static void AssertSmoothScroll(double from, double to, (int, int)[] expected)
    {
        var operation = CreateOperation(from, to, ViewportHeight, 0, AnimationDuration);

        var actual = SampleTimes.Select(now => ExtractLines(operation, now)).ToArray();

        Assert.Equal(expected, actual.Select(t => (t.Start, t.End)).ToArray());
    }
}

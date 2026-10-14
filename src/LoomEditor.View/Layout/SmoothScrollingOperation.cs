// Ported from microsoft/vscode: src/vs/base/common/scrollable.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>One step of a smooth scrolling animation. Monaco: <c>SmoothScrollingUpdate</c>.</summary>
/// <remarks>Threading: immutable value, safe from any thread.</remarks>
internal readonly record struct SmoothScrollingUpdate(double ScrollLeft, double ScrollTop, bool IsDone);

/// <summary>
/// An ease-out cubic animation of the scroll position, from one position to another.
/// Monaco: <c>SmoothScrollingOperation</c> (with its easing functions).
/// </summary>
/// <remarks>
/// Threading: not thread-safe; used by <see cref="Scrollable"/> on the UI thread.
/// Difference from Monaco: the clock is a <see cref="TimeProvider"/> instead of <c>Date.now()</c>, and the protected
/// <c>_tick(now)</c> is <see cref="Tick(double)"/>.
/// </remarks>
internal sealed class SmoothScrollingOperation : IDisposable
{
    private readonly TimeProvider _timeProvider;
    private Func<double, double> _scrollLeft = null!;
    private Func<double, double> _scrollTop = null!;

    public SmoothScrollingOperation(
        ISmoothScrollPosition from,
        ISmoothScrollPosition to,
        double startTime,
        double duration,
        TimeProvider? timeProvider = null)
    {
        From = from;
        To = to;
        Duration = duration;
        StartTime = startTime;
        _timeProvider = timeProvider ?? TimeProvider.System;

        InitAnimations();
    }

    public ISmoothScrollPosition From { get; }

    public ISmoothScrollPosition To { get; private set; }

    public double Duration { get; }

    public double StartTime { get; }

    public IDisposable? AnimationFrameDisposable { get; set; }

    public static SmoothScrollingOperation Start(
        ISmoothScrollPosition from,
        ISmoothScrollPosition to,
        double duration,
        TimeProvider timeProvider)
    {
        // +10 / -10 : pretend the animation already started for a quicker response to a scroll request
        duration += 10;
        var startTime = timeProvider.GetUtcNow().ToUnixTimeMilliseconds() - 10;

        return new SmoothScrollingOperation(from, to, startTime, duration, timeProvider);
    }

    public SmoothScrollingOperation Combine(ISmoothScrollPosition from, ISmoothScrollPosition to, double duration)
    {
        return Start(from, to, duration, _timeProvider);
    }

    public void Dispose()
    {
        if (AnimationFrameDisposable is not null)
        {
            AnimationFrameDisposable.Dispose();
            AnimationFrameDisposable = null;
        }
    }

    public void AcceptScrollDimensions(ScrollState state)
    {
        To = state.WithScrollPosition(new NewScrollPosition(To.ScrollLeft, To.ScrollTop));
        InitAnimations();
    }

    public SmoothScrollingUpdate Tick()
    {
        return Tick(_timeProvider.GetUtcNow().ToUnixTimeMilliseconds());
    }

    public SmoothScrollingUpdate Tick(double now)
    {
        var completion = (now - StartTime) / Duration;

        if (completion < 1)
        {
            return new SmoothScrollingUpdate(_scrollLeft(completion), _scrollTop(completion), false);
        }

        return new SmoothScrollingUpdate(To.ScrollLeft, To.ScrollTop, true);
    }

    private static Func<double, double> CreateEaseOutCubic(double from, double to)
    {
        var delta = to - from;
        return completion => from + (delta * EaseOutCubic(completion));
    }

    private static Func<double, double> CreateComposed(Func<double, double> a, Func<double, double> b, double cut)
    {
        return completion =>
        {
            if (completion < cut)
            {
                return a(completion / cut);
            }

            return b((completion - cut) / (1 - cut));
        };
    }

    private static double EaseInCubic(double t) => Math.Pow(t, 3);

    private static double EaseOutCubic(double t) => 1 - EaseInCubic(1 - t);

    private static Func<double, double> InitAnimation(double from, double to, double viewportSize)
    {
        var delta = Math.Abs(from - to);
        if (delta > 2.5 * viewportSize)
        {
            double stop1, stop2;
            if (from < to)
            {
                // scroll to 75% of the viewportSize
                stop1 = from + (0.75 * viewportSize);
                stop2 = to - (0.75 * viewportSize);
            }
            else
            {
                stop1 = from - (0.75 * viewportSize);
                stop2 = to + (0.75 * viewportSize);
            }

            return CreateComposed(CreateEaseOutCubic(from, stop1), CreateEaseOutCubic(stop2, to), 0.33);
        }

        return CreateEaseOutCubic(from, to);
    }

    private void InitAnimations()
    {
        _scrollLeft = InitAnimation(From.ScrollLeft, To.ScrollLeft, To.Width);
        _scrollTop = InitAnimation(From.ScrollTop, To.ScrollTop, To.Height);
    }
}

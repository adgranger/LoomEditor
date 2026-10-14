// Ported from microsoft/vscode: src/vs/base/common/scrollable.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>
/// Options of a <see cref="Scrollable"/>. Monaco: <c>IScrollableOptions</c>.
/// </summary>
/// <param name="ForceIntegerValues">Whether the scroll values are always integers.</param>
/// <param name="SmoothScrollDuration">Duration (ms) of smooth scroll animations; 0 turns them off.</param>
/// <param name="ScheduleAtNextAnimationFrame">Runs a callback at the next animation frame; dispose to cancel.</param>
/// <param name="TimeProvider">The clock (replaces <c>Date.now()</c>).</param>
internal sealed record ScrollableOptions(
    bool ForceIntegerValues,
    int SmoothScrollDuration,
    Func<Action, IDisposable> ScheduleAtNextAnimationFrame,
    TimeProvider TimeProvider);

/// <summary>
/// Owns the current <see cref="ScrollState"/>, raises <see cref="Scroll"/> and animates smooth scrolling.
/// Monaco: <c>Scrollable</c>.
/// </summary>
/// <remarks>Threading: UI thread only; <see cref="Scroll"/> is raised synchronously on the calling thread.</remarks>
internal sealed class Scrollable : IDisposable
{
    private readonly Func<Action, IDisposable> _scheduleAtNextAnimationFrame;
    private readonly TimeProvider _timeProvider;
    private int _smoothScrollDuration;
    private ScrollState _state;
    private SmoothScrollingOperation? _smoothScrolling;

    public Scrollable(ScrollableOptions options)
    {
        _smoothScrollDuration = options.SmoothScrollDuration;
        _scheduleAtNextAnimationFrame = options.ScheduleAtNextAnimationFrame;
        _timeProvider = options.TimeProvider;
        _state = new ScrollState(options.ForceIntegerValues, 0, 0, 0, 0, 0, 0);
    }

    public event EventHandler<ScrollEvent>? Scroll;

    public bool HasPendingScrollAnimation => _smoothScrolling is not null;

    public void Dispose()
    {
        if (_smoothScrolling is not null)
        {
            _smoothScrolling.Dispose();
            _smoothScrolling = null;
        }
    }

    public void SetSmoothScrollDuration(int smoothScrollDuration)
    {
        _smoothScrollDuration = smoothScrollDuration;
    }

    public IScrollPosition ValidateScrollPosition(NewScrollPosition scrollPosition)
    {
        return _state.WithScrollPosition(scrollPosition);
    }

    public IScrollDimensions GetScrollDimensions()
    {
        return _state;
    }

    public void SetScrollDimensions(NewScrollDimensions dimensions, bool useRawScrollPositions)
    {
        var newState = _state.WithScrollDimensions(dimensions, useRawScrollPositions);
        SetState(newState, _smoothScrolling is not null);

        // Validate outstanding animated scroll position target
        _smoothScrolling?.AcceptScrollDimensions(_state);
    }

    /// <summary>
    /// Returns the final scroll position that the instance will have once the smooth scroll animation concludes.
    /// If no scroll animation is occurring, it returns the current scroll position instead.
    /// </summary>
    public IScrollPosition GetFutureScrollPosition()
    {
        if (_smoothScrolling is not null)
        {
            return _smoothScrolling.To;
        }

        return _state;
    }

    /// <summary>
    /// Returns the current scroll position. It might be an intermediate position if a smooth scroll animation is running.
    /// </summary>
    public IScrollPosition GetCurrentScrollPosition()
    {
        return _state;
    }

    public void SetScrollPositionNow(NewScrollPosition update)
    {
        // no smooth scrolling requested
        var newState = _state.WithScrollPosition(update);

        // Terminate any outstanding smooth scrolling
        if (_smoothScrolling is not null)
        {
            _smoothScrolling.Dispose();
            _smoothScrolling = null;
        }

        SetState(newState, false);
    }

    public void SetScrollPositionSmooth(NewScrollPosition update, bool reuseAnimation = false)
    {
        if (_smoothScrollDuration == 0)
        {
            // Smooth scrolling not supported.
            SetScrollPositionNow(update);
            return;
        }

        if (_smoothScrolling is not null)
        {
            // Combine our pending scrollLeft/scrollTop with incoming scrollLeft/scrollTop
            update = new NewScrollPosition(
                update.ScrollLeft ?? _smoothScrolling.To.ScrollLeft,
                update.ScrollTop ?? _smoothScrolling.To.ScrollTop);

            // Validate `update`
            var validTarget = _state.WithScrollPosition(update);

            if (_smoothScrolling.To.ScrollLeft == validTarget.ScrollLeft && _smoothScrolling.To.ScrollTop == validTarget.ScrollTop)
            {
                // No need to interrupt or extend the current animation since we're going to the same place
                return;
            }

            SmoothScrollingOperation newSmoothScrolling;
            if (reuseAnimation)
            {
                newSmoothScrolling = new SmoothScrollingOperation(
                    _smoothScrolling.From, validTarget, _smoothScrolling.StartTime, _smoothScrolling.Duration, _timeProvider);
            }
            else
            {
                newSmoothScrolling = _smoothScrolling.Combine(_state, validTarget, _smoothScrollDuration);
            }

            _smoothScrolling.Dispose();
            _smoothScrolling = newSmoothScrolling;
        }
        else
        {
            // Validate `update`
            var validTarget = _state.WithScrollPosition(update);

            _smoothScrolling = SmoothScrollingOperation.Start(_state, validTarget, _smoothScrollDuration, _timeProvider);
        }

        // Begin smooth scrolling animation
        ScheduleNextFrame();
    }

    private void ScheduleNextFrame()
    {
        _smoothScrolling!.AnimationFrameDisposable = _scheduleAtNextAnimationFrame(() =>
        {
            if (_smoothScrolling is null)
            {
                return;
            }

            _smoothScrolling.AnimationFrameDisposable = null;
            PerformSmoothScrolling();
        });
    }

    private void PerformSmoothScrolling()
    {
        if (_smoothScrolling is null)
        {
            return;
        }

        var update = _smoothScrolling.Tick();
        var newState = _state.WithScrollPosition(new NewScrollPosition(update.ScrollLeft, update.ScrollTop));

        SetState(newState, true);

        if (_smoothScrolling is null)
        {
            // Looks like someone canceled the smooth scrolling
            // from the scroll event handler
            return;
        }

        if (update.IsDone)
        {
            _smoothScrolling.Dispose();
            _smoothScrolling = null;
            return;
        }

        // Continue smooth scrolling animation
        ScheduleNextFrame();
    }

    private void SetState(ScrollState newState, bool inSmoothScrolling)
    {
        var oldState = _state;
        if (oldState.Equals(newState))
        {
            // no change
            return;
        }

        _state = newState;
        Scroll?.Invoke(this, _state.CreateScrollEvent(oldState, inSmoothScrolling));
    }
}

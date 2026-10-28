// Ported from microsoft/vscode: src/vs/base/browser/ui/scrollbar/scrollbarState.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>
/// The size and the position of the slider of a scrollbar, computed from the scroll sizes (Monaco: <c>ScrollbarState</c>). Used here for
/// the viewport slider of the diff overview only.
/// </summary>
/// <remarks>
/// Behavior difference: <c>getDesiredScrollPositionFromOffsetPaged</c> and the accessors the DOM scrollbar needs (<c>clone</c>,
/// <c>getRectangle*Size</c>, <c>getArrowSize</c>, <c>isNeeded</c> setters) are not ported.
/// Threading: UI thread only; not thread-safe.
/// </remarks>
internal sealed class ScrollbarState
{
    private const int MinimumSliderSize = 20;

    private readonly int _arrowSize;
    private readonly int _oppositeScrollbarSize;
    private double _visibleSize;
    private double _scrollSize;
    private double _scrollPosition;
    private bool _computedIsNeeded;
    private double _computedSliderSize;
    private double _computedSliderRatio;
    private double _computedSliderPosition;

    /// <summary>Creates the state.</summary>
    /// <param name="arrowSize">The size of the arrows (0 without arrows).</param>
    /// <param name="scrollbarSize">The width of a vertical scrollbar.</param>
    /// <param name="oppositeScrollbarSize">The height of the pair horizontal scrollbar.</param>
    /// <param name="visibleSize">The viewport height.</param>
    /// <param name="scrollSize">The scroll height.</param>
    /// <param name="scrollPosition">The scroll top.</param>
    public ScrollbarState(double arrowSize, double scrollbarSize, double oppositeScrollbarSize, double visibleSize, double scrollSize, double scrollPosition)
    {
        ScrollbarSize = (int)Math.Round(scrollbarSize, MidpointRounding.AwayFromZero);
        _oppositeScrollbarSize = (int)Math.Round(oppositeScrollbarSize, MidpointRounding.AwayFromZero);
        _arrowSize = (int)Math.Round(arrowSize, MidpointRounding.AwayFromZero);
        _visibleSize = visibleSize;
        _scrollSize = scrollSize;
        _scrollPosition = scrollPosition;
        RefreshComputedValues();
    }

    /// <summary>The width of a vertical scrollbar.</summary>
    public int ScrollbarSize { get; }

    /// <summary>Sets the viewport size; true when it changed.</summary>
    public bool SetVisibleSize(double visibleSize)
    {
        double rounded = Round(visibleSize);
        if (_visibleSize == rounded)
        {
            return false;
        }

        _visibleSize = rounded;
        RefreshComputedValues();
        return true;
    }

    /// <summary>Sets the scroll size; true when it changed.</summary>
    public bool SetScrollSize(double scrollSize)
    {
        double rounded = Round(scrollSize);
        if (_scrollSize == rounded)
        {
            return false;
        }

        _scrollSize = rounded;
        RefreshComputedValues();
        return true;
    }

    /// <summary>Sets the scroll position; true when it changed.</summary>
    public bool SetScrollPosition(double scrollPosition)
    {
        double rounded = Round(scrollPosition);
        if (_scrollPosition == rounded)
        {
            return false;
        }

        _scrollPosition = rounded;
        RefreshComputedValues();
        return true;
    }

    /// <summary>The size of the slider.</summary>
    public double GetSliderSize() => _computedSliderSize;

    /// <summary>The position of the slider.</summary>
    public double GetSliderPosition() => _computedSliderPosition;

    /// <summary>Computes the scroll position that puts <paramref name="offset"/> (in the coordinates of the slider position) at the center of the slider.</summary>
    public double GetDesiredScrollPositionFromOffset(double offset)
    {
        if (!_computedIsNeeded)
        {
            return 0;
        }

        double desiredSliderPosition = offset - _arrowSize - (_computedSliderSize / 2);
        return Round(desiredSliderPosition / _computedSliderRatio);
    }

    /// <summary>Computes the scroll position that moves the slider by <paramref name="delta"/>.</summary>
    public double GetDesiredScrollPositionFromDelta(double delta)
    {
        if (!_computedIsNeeded)
        {
            return 0;
        }

        double desiredSliderPosition = _computedSliderPosition + delta;
        return Round(desiredSliderPosition / _computedSliderRatio);
    }

    // Monaco: Math.round (half up), which is not Math.Round's default (half to even).
    private static double Round(double value) => Math.Floor(value + 0.5);

    private void RefreshComputedValues()
    {
        double availableSize = Math.Max(0, _visibleSize - _oppositeScrollbarSize);
        double representableSize = Math.Max(0, availableSize - (2 * _arrowSize));
        _computedIsNeeded = _scrollSize > 0 && _scrollSize > _visibleSize;

        if (!_computedIsNeeded)
        {
            _computedSliderSize = Round(representableSize);
            _computedSliderRatio = 0;
            _computedSliderPosition = 0;
            return;
        }

        // The slider is made bigger when needed: it would be too small to grab otherwise.
        double sliderSize = Round(Math.Max(MinimumSliderSize, Math.Floor(_visibleSize * representableSize / _scrollSize)));

        // The slider moves from 0 to `representableSize - sliderSize` as the scroll position moves from 0 to `scrollSize - visibleSize`.
        _computedSliderSize = sliderSize;
        _computedSliderRatio = (representableSize - sliderSize) / (_scrollSize - _visibleSize);
        _computedSliderPosition = Round(_scrollPosition * _computedSliderRatio);
    }
}

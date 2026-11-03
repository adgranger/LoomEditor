using LoomEditor.View.Layout;

#if LOOM_WINUI
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif

// Scrollbars and the scroll API. The scrollbars are the framework's ScrollBars laid over the canvas (ADR 0005, decision 3);
// the ViewLayout owns the scroll position and they mirror it in both directions.
public sealed partial class LoomEditor
{
    private const string VerticalScrollBarPartName = "PART_VerticalScrollBar";
    private const string HorizontalScrollBarPartName = "PART_HorizontalScrollBar";

    private bool _isUpdatingScrollBars;
    private double _pendingScrollTop;
    private double _pendingScrollLeft;

    /// <summary>The vertical scroll position, in DIPs (Monaco: getScrollTop / setScrollTop).</summary>
    public double ScrollTop
    {
        get => _measurer is null ? _pendingScrollTop : _viewModel.ViewLayout.GetCurrentScrollTop();
        set
        {
            _pendingScrollTop = value;
            if (_measurer is not null)
            {
                _viewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(null, value), ScrollType.Immediate);
            }
        }
    }

    /// <summary>The horizontal scroll position, in DIPs (Monaco: getScrollLeft / setScrollLeft).</summary>
    public double ScrollLeft
    {
        get => _measurer is null ? _pendingScrollLeft : _viewModel.ViewLayout.GetCurrentScrollLeft();
        set
        {
            _pendingScrollLeft = value;
            if (_measurer is not null)
            {
                _viewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(value, null), ScrollType.Immediate);
            }
        }
    }

    // The scroll of a wheel event, once the host has read its deltas and fed the classifier. Returns whether the event is consumed.
    private bool ScrollByWheelDelta(MouseWheelInput input)
    {
        Scrollable scrollable = _viewModel.ViewLayout.GetScrollable();
        IScrollPosition future = scrollable.GetFutureScrollPosition();
        bool didScroll = false;
        NewScrollPosition? desired = MouseWheelScrolling.ComputeScrollPosition(
            input, new ScrollPosition(future.ScrollLeft, future.ScrollTop), _wheelOptions);
        if (desired is not null)
        {
            IScrollPosition valid = _viewModel.ViewLayout.ValidateScrollPosition(desired.Value);
            if (valid.ScrollLeft != future.ScrollLeft || valid.ScrollTop != future.ScrollTop)
            {
                _viewModel.ViewLayout.SetScrollPosition(
                    new NewScrollPosition(valid.ScrollLeft, valid.ScrollTop),
                    MouseWheelScrolling.ShouldScrollSmoothly(_wheelClassifier) ? ScrollType.Smooth : ScrollType.Immediate);
                didScroll = true;
            }
        }

        // Monaco: `alwaysConsumeMouseWheel` is true in the editor, as long as there is a scrollbar to scroll with.
        return didScroll || IsAnyScrollBarVisible();
    }

    // Applies the values set before the view model existed.
    private void ApplyPendingScrollPosition()
    {
        if (_pendingScrollTop != 0 || _pendingScrollLeft != 0)
        {
            _viewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(_pendingScrollLeft, _pendingScrollTop), ScrollType.Immediate);
        }
    }

    // The user moved a scroll bar.
    private void ScrollFromScrollBar(bool vertical, double value)
    {
        if (_isUpdatingScrollBars)
        {
            return;
        }

        _viewModel.ViewLayout.SetScrollPosition(
            vertical ? new NewScrollPosition(null, value) : new NewScrollPosition(value, null),
            ScrollType.Immediate);
    }

    private void OnViewLayoutContentSizeChanged(object? sender, ContentSizeChangedEventArgs e)
    {
        UpdateScrollBars();
        InvalidateCanvas();
        RaiseContentSizeChanged();
    }

    // Monaco: `ScrollbarVisibility.Auto`: a scrollbar is shown only when the content is larger than the viewport.
    private void UpdateScrollBarValues()
    {
        IScrollDimensions dimensions = _viewModel.ViewLayout.GetScrollable().GetScrollDimensions();
        FontInfo font = _viewModel.FontInfo;
        _isUpdatingScrollBars = true;
        try
        {
            UpdateScrollBar(
                vertical: true, dimensions.ScrollHeight, dimensions.Height, font.LineHeight, _viewModel.ViewLayout.GetCurrentScrollTop());
            UpdateScrollBar(
                vertical: false, dimensions.ScrollWidth, dimensions.Width, font.TypicalHalfwidthCharacterWidth,
                _viewModel.ViewLayout.GetCurrentScrollLeft());
        }
        finally
        {
            _isUpdatingScrollBars = false;
        }
    }

    private void UpdateScrollBar(bool vertical, double scrollSize, double viewportSize, double smallChange, double value)
    {
        double maximum = scrollSize - viewportSize;
        SetScrollBarValues(vertical, maximum > 0, Math.Max(0, maximum), viewportSize, smallChange, value);
    }
}

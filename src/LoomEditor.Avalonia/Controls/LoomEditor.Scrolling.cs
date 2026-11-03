using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Controls;

// Scrollbars and the scroll API. The scrollbars are Avalonia ScrollBars laid over the canvas (ADR 0005, decision 3);
// the ViewLayout owns the scroll position and they mirror it in both directions.
public sealed partial class LoomEditor
{
    private const string VerticalScrollBarPartName = "PART_VerticalScrollBar";
    private const string HorizontalScrollBarPartName = "PART_HorizontalScrollBar";

    private readonly MouseWheelClassifier _wheelClassifier = new();
    private readonly MouseWheelOptions _wheelOptions = new();
    private ScrollBar? _verticalScrollBar;
    private ScrollBar? _horizontalScrollBar;
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

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerWheelChanged(e);
        ScrollFromWheel(e, allowZoom: true);
    }

    // Also used by the diff overview, which hands its wheel events to the modified editor (Monaco: `delegateScrollFromMouseWheelEvent`).
    internal void ScrollFromWheel(PointerWheelEventArgs e, bool allowZoom = false)
    {
        // Monaco: `StandardWheelEvent`: one notch is 1, positive = up / left. Avalonia gives the same, in lines (fractional on a trackpad).
        double deltaX = e.Delta.X;
        double deltaY = e.Delta.Y;
        bool isHorizontal = deltaX != 0 && deltaY == 0;
        var input = new MouseWheelInput(deltaX, deltaY, e.KeyModifiers.HasFlag(KeyModifiers.Shift), e.KeyModifiers.HasFlag(KeyModifiers.Alt));

        // Avalonia timestamps are in milliseconds, as Monaco's classifier wants.
        _wheelClassifier.Accept(e.Timestamp, input.DeltaX, input.DeltaY);

        if (allowZoom && MouseWheelZoom && !isHorizontal && TryZoomFromWheel(e, deltaY))
        {
            return;
        }

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
        e.Handled = didScroll
            || _verticalScrollBar?.IsVisible == true
            || _horizontalScrollBar?.IsVisible == true;
    }

    private void AttachScrollBars(INameScope nameScope)
    {
        DetachScrollBars();
        _verticalScrollBar = nameScope.Find<ScrollBar>(VerticalScrollBarPartName);
        _horizontalScrollBar = nameScope.Find<ScrollBar>(HorizontalScrollBarPartName);
        if (_verticalScrollBar is not null)
        {
            _verticalScrollBar.AllowAutoHide = true;
            _verticalScrollBar.Scroll += OnScrollBarScroll;
        }

        if (_horizontalScrollBar is not null)
        {
            _horizontalScrollBar.AllowAutoHide = true;
            _horizontalScrollBar.Scroll += OnScrollBarScroll;
        }

        UpdateScrollBars();
    }

    private void DetachScrollBars()
    {
        if (_verticalScrollBar is not null)
        {
            _verticalScrollBar.Scroll -= OnScrollBarScroll;
            _verticalScrollBar = null;
        }

        if (_horizontalScrollBar is not null)
        {
            _horizontalScrollBar.Scroll -= OnScrollBarScroll;
            _horizontalScrollBar = null;
        }
    }

    // Applies the values set before the view model existed.
    private void ApplyPendingScrollPosition()
    {
        if (_pendingScrollTop != 0 || _pendingScrollLeft != 0)
        {
            _viewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(_pendingScrollLeft, _pendingScrollTop), ScrollType.Immediate);
        }
    }

    private void OnScrollBarScroll(object? sender, ScrollEventArgs e)
    {
        if (_isUpdatingScrollBars)
        {
            return;
        }

        _viewModel.ViewLayout.SetScrollPosition(
            ReferenceEquals(sender, _verticalScrollBar)
                ? new NewScrollPosition(null, e.NewValue)
                : new NewScrollPosition(e.NewValue, null),
            ScrollType.Immediate);
    }

    private void OnViewLayoutContentSizeChanged(object? sender, ContentSizeChangedEventArgs e)
    {
        UpdateScrollBars();
        Invalidate();
    }

    // Monaco: `ScrollbarVisibility.Auto`: a scrollbar is shown only when the content is larger than the viewport.
    private void UpdateScrollBars()
    {
        if (_isDrawing)
        {
            // Changing a scroll bar invalidates the layout, which Avalonia refuses during a render pass.
            Dispatcher.UIThread.Post(UpdateScrollBars);
            return;
        }

        IScrollDimensions dimensions = _viewModel.ViewLayout.GetScrollable().GetScrollDimensions();
        FontInfo font = _viewModel.FontInfo;
        _isUpdatingScrollBars = true;
        try
        {
            UpdateScrollBar(
                _verticalScrollBar, dimensions.ScrollHeight, dimensions.Height, font.LineHeight, _viewModel.ViewLayout.GetCurrentScrollTop());
            UpdateScrollBar(
                _horizontalScrollBar, dimensions.ScrollWidth, dimensions.Width, font.TypicalHalfwidthCharacterWidth,
                _viewModel.ViewLayout.GetCurrentScrollLeft());
        }
        finally
        {
            _isUpdatingScrollBars = false;
        }
    }

    private static void UpdateScrollBar(ScrollBar? scrollBar, double scrollSize, double viewportSize, double smallChange, double value)
    {
        if (scrollBar is null)
        {
            return;
        }

        double maximum = scrollSize - viewportSize;
        scrollBar.IsVisible = maximum > 0;
        scrollBar.Minimum = 0;
        scrollBar.Maximum = Math.Max(0, maximum);
        scrollBar.ViewportSize = viewportSize;
        scrollBar.LargeChange = viewportSize;
        scrollBar.SmallChange = smallChange;
        scrollBar.Value = Math.Clamp(value, 0, scrollBar.Maximum);
    }
}

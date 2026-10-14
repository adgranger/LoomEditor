using LoomEditor.View.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace LoomEditor.WinUI.Controls;

// Scrollbars and the scroll API. The scrollbars are XAML ScrollBars laid over the canvas (ADR 0005, decision 3);
// the ViewLayout owns the scroll position and they mirror it in both directions.
public sealed partial class LoomEditor
{
    private const string VerticalScrollBarPartName = "PART_VerticalScrollBar";
    private const string HorizontalScrollBarPartName = "PART_HorizontalScrollBar";

    private ScrollBar? _verticalScrollBar;
    private ScrollBar? _horizontalScrollBar;
    private bool _isUpdatingScrollBars;
    private double _pendingScrollTop;
    private double _pendingScrollLeft;

    /// <summary>The vertical scroll position, in DIPs (Monaco: getScrollTop / setScrollTop).</summary>
    public double ScrollTop
    {
        get => _viewModel?.ViewLayout.GetCurrentScrollTop() ?? _pendingScrollTop;
        set
        {
            _pendingScrollTop = value;
            _viewModel?.ViewLayout.SetScrollPosition(new NewScrollPosition(null, value), ScrollType.Immediate);
        }
    }

    /// <summary>The horizontal scroll position, in DIPs (Monaco: getScrollLeft / setScrollLeft).</summary>
    public double ScrollLeft
    {
        get => _viewModel?.ViewLayout.GetCurrentScrollLeft() ?? _pendingScrollLeft;
        set
        {
            _pendingScrollLeft = value;
            _viewModel?.ViewLayout.SetScrollPosition(new NewScrollPosition(value, null), ScrollType.Immediate);
        }
    }

    private void AttachScrollBars()
    {
        DetachScrollBars();
        _verticalScrollBar = GetTemplateChild(VerticalScrollBarPartName) as ScrollBar;
        _horizontalScrollBar = GetTemplateChild(HorizontalScrollBarPartName) as ScrollBar;
        if (_verticalScrollBar is not null)
        {
            _verticalScrollBar.ValueChanged += OnScrollBarValueChanged;
        }

        if (_horizontalScrollBar is not null)
        {
            _horizontalScrollBar.ValueChanged += OnScrollBarValueChanged;
        }

        UpdateScrollBars();
    }

    private void DetachScrollBars()
    {
        if (_verticalScrollBar is not null)
        {
            _verticalScrollBar.ValueChanged -= OnScrollBarValueChanged;
            _verticalScrollBar = null;
        }

        if (_horizontalScrollBar is not null)
        {
            _horizontalScrollBar.ValueChanged -= OnScrollBarValueChanged;
            _horizontalScrollBar = null;
        }
    }

    // Applies the values set before the view model existed.
    private void ApplyPendingScrollPosition()
    {
        if (_viewModel is not null && (_pendingScrollTop != 0 || _pendingScrollLeft != 0))
        {
            _viewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(_pendingScrollLeft, _pendingScrollTop), ScrollType.Immediate);
        }
    }

    private void OnScrollBarValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isUpdatingScrollBars || _viewModel is null)
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
        _canvas?.Invalidate();
    }

    // Monaco: `ScrollbarVisibility.Auto`: a scrollbar is shown only when the content is larger than the viewport.
    private void UpdateScrollBars()
    {
        if (_viewModel is null)
        {
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
        scrollBar.Visibility = maximum > 0 ? Visibility.Visible : Visibility.Collapsed;
        scrollBar.Minimum = 0;
        scrollBar.Maximum = Math.Max(0, maximum);
        scrollBar.ViewportSize = viewportSize;
        scrollBar.LargeChange = viewportSize;
        scrollBar.SmallChange = smallChange;
        scrollBar.Value = Math.Clamp(value, 0, scrollBar.Maximum);
    }
}

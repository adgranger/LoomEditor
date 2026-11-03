using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Controls;

// The Avalonia scroll bars and wheel event.
public sealed partial class LoomEditor
{
    private readonly MouseWheelClassifier _wheelClassifier = new();
    private readonly MouseWheelOptions _wheelOptions = new();
    private ScrollBar? _verticalScrollBar;
    private ScrollBar? _horizontalScrollBar;

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

        e.Handled = ScrollByWheelDelta(input);
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

    private void OnScrollBarScroll(object? sender, ScrollEventArgs e) => ScrollFromScrollBar(ReferenceEquals(sender, _verticalScrollBar), e.NewValue);

    private void UpdateScrollBars()
    {
        if (_isDrawing)
        {
            // Changing a scroll bar invalidates the layout, which Avalonia refuses during a render pass.
            Dispatcher.UIThread.Post(UpdateScrollBars);
            return;
        }

        UpdateScrollBarValues();
    }

    private bool IsAnyScrollBarVisible() => _verticalScrollBar?.IsVisible == true || _horizontalScrollBar?.IsVisible == true;

    private void SetScrollBarValues(bool vertical, bool visible, double maximum, double viewportSize, double smallChange, double value)
    {
        if ((vertical ? _verticalScrollBar : _horizontalScrollBar) is not { } scrollBar)
        {
            return;
        }

        scrollBar.IsVisible = visible;
        scrollBar.Minimum = 0;
        scrollBar.Maximum = maximum;
        scrollBar.ViewportSize = viewportSize;
        scrollBar.LargeChange = viewportSize;
        scrollBar.SmallChange = smallChange;
        scrollBar.Value = Math.Clamp(value, 0, scrollBar.Maximum);
    }
}

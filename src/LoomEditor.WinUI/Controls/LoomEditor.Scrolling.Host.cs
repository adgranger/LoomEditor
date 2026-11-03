using LoomEditor.View.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace LoomEditor.WinUI.Controls;

// The XAML scroll bars and the WinUI wheel event.
public sealed partial class LoomEditor
{
    private ScrollBar? _verticalScrollBar;
    private ScrollBar? _horizontalScrollBar;

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerWheelChanged(e);
        ScrollFromWheel(e, allowZoom: true);
    }

    // Also used by the diff overview, which hands its wheel events to the modified editor (Monaco: `delegateScrollFromMouseWheelEvent`).
    internal void ScrollFromWheel(PointerRoutedEventArgs e, bool allowZoom = false)
    {
        // Monaco: `StandardWheelEvent` on Windows: one notch (WHEEL_DELTA = 120) is 1, positive = up / left.
        PointerPoint point = e.GetCurrentPoint(this);
        double delta = point.Properties.MouseWheelDelta / 120.0;
        bool isHorizontal = point.Properties.IsHorizontalMouseWheel;
        var input = new MouseWheelInput(
            isHorizontal ? -delta : 0,
            isHorizontal ? 0 : delta,
            e.KeyModifiers.HasFlag(global::Windows.System.VirtualKeyModifiers.Shift),
            e.KeyModifiers.HasFlag(global::Windows.System.VirtualKeyModifiers.Menu));

        // Timestamp is in microseconds, Monaco's classifier takes milliseconds.
        _wheelClassifier.Accept(point.Timestamp / 1000.0, input.DeltaX, input.DeltaY);

        if (allowZoom && MouseWheelZoom && !isHorizontal && TryZoomFromWheel(e, point, delta))
        {
            return;
        }

        e.Handled = ScrollByWheelDelta(input);
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

    private void OnScrollBarValueChanged(object sender, RangeBaseValueChangedEventArgs e) =>
        ScrollFromScrollBar(ReferenceEquals(sender, _verticalScrollBar), e.NewValue);

    private void UpdateScrollBars() => UpdateScrollBarValues();

    private bool IsAnyScrollBarVisible() =>
        _verticalScrollBar?.Visibility == Visibility.Visible || _horizontalScrollBar?.Visibility == Visibility.Visible;

    private void SetScrollBarValues(bool vertical, bool visible, double maximum, double viewportSize, double smallChange, double value)
    {
        if ((vertical ? _verticalScrollBar : _horizontalScrollBar) is not { } scrollBar)
        {
            return;
        }

        scrollBar.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        scrollBar.Minimum = 0;
        scrollBar.Maximum = maximum;
        scrollBar.ViewportSize = viewportSize;
        scrollBar.LargeChange = viewportSize;
        scrollBar.SmallChange = smallChange;
        scrollBar.Value = Math.Clamp(value, 0, scrollBar.Maximum);
    }
}

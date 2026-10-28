using LoomEditor.View.Controller;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace LoomEditor.WinUI.Controls;

// Editor zoom. Monaco: `EditorZoom` and the `editor.fontSize` / `lineHeight` multiplier of `FontInfo`.
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="ZoomLevel"/> dependency property.</summary>
    public static readonly DependencyProperty ZoomLevelProperty = DependencyProperty.Register(
        nameof(ZoomLevel), typeof(double), typeof(LoomEditor), new PropertyMetadata(0.0, OnZoomLevelChanged));

    /// <summary>
    /// The zoom level, from -5 to 20; the font size and the line height are multiplied by <c>1 + level * 0.1</c>.
    /// Values outside the range are clamped (50 reads back 20). Defaults to 0 (100 %).
    /// </summary>
    public double ZoomLevel
    {
        get => (double)GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    /// <summary>Identifies the <see cref="MouseWheelZoom"/> dependency property.</summary>
    public static readonly DependencyProperty MouseWheelZoomProperty = DependencyProperty.Register(
        nameof(MouseWheelZoom), typeof(bool), typeof(LoomEditor), new PropertyMetadata(false));

    private readonly MouseWheelZoomGesture _wheelZoom = new();

    /// <summary>Whether Ctrl + mouse wheel zooms the editor (Monaco: <c>mouseWheelZoom</c>). Defaults to <see langword="false"/>.</summary>
    public bool MouseWheelZoom
    {
        get => (bool)GetValue(MouseWheelZoomProperty);
        set => SetValue(MouseWheelZoomProperty, value);
    }

    private static void OnZoomLevelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        // The state clamps and raises Changed, which writes the clamped value back (see OnZoomChanged).
        var editor = (LoomEditor)sender;
        editor._context.Zoom.SetLevel((double)e.NewValue);
        if (editor.ZoomLevel != editor._context.Zoom.Level)
        {
            editor.SetValue(ZoomLevelProperty, editor._context.Zoom.Level);
        }
    }

    private void OnZoomChanged(object? sender, EventArgs e)
    {
        if (ZoomLevel != _context.Zoom.Level)
        {
            SetValue(ZoomLevelProperty, _context.Zoom.Level);
        }

        RecreateTextMeasurer();
    }

    // The classifier has already accepted this event (see ScrollFromWheel).
    private bool TryZoomFromWheel(PointerRoutedEventArgs e, Microsoft.UI.Input.PointerPoint point, double delta)
    {
        VirtualKeyModifiers keys = e.KeyModifiers;
        double? level = _wheelZoom.OnWheel(
            delta,
            _wheelClassifier.IsPhysicalMouseWheel(),
            keys.HasFlag(VirtualKeyModifiers.Control),
            keys.HasFlag(VirtualKeyModifiers.Shift),
            keys.HasFlag(VirtualKeyModifiers.Menu),
            keys.HasFlag(VirtualKeyModifiers.Windows),
            (long)(point.Timestamp / 1000),
            _context.Zoom.Level);
        if (level is null)
        {
            return false;
        }

        _context.Zoom.SetLevel(level.Value);
        e.Handled = true;
        return true;
    }
}

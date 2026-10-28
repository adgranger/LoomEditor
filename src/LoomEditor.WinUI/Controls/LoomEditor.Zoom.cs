using Microsoft.UI.Xaml;

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
}

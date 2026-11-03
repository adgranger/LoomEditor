using Avalonia;
using Avalonia.Input;
using LoomEditor.View.Controller;

namespace LoomEditor.Avalonia.Controls;

// Editor zoom. Monaco: `EditorZoom` and the `editor.fontSize` / `lineHeight` multiplier of `FontInfo`.
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="ZoomLevel"/> styled property.</summary>
    public static readonly StyledProperty<double> ZoomLevelProperty =
        AvaloniaProperty.Register<LoomEditor, double>(nameof(ZoomLevel), 0.0);

    /// <summary>
    /// The zoom level, from -5 to 20; the font size and the line height are multiplied by <c>1 + level * 0.1</c>.
    /// Values outside the range are clamped (50 reads back 20). Defaults to 0 (100 %).
    /// </summary>
    public double ZoomLevel
    {
        get => GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    /// <summary>Identifies the <see cref="MouseWheelZoom"/> styled property.</summary>
    public static readonly StyledProperty<bool> MouseWheelZoomProperty =
        AvaloniaProperty.Register<LoomEditor, bool>(nameof(MouseWheelZoom), false);

    /// <summary>Whether Ctrl + mouse wheel zooms the editor (Monaco: <c>mouseWheelZoom</c>). Defaults to <see langword="false"/>.</summary>
    public bool MouseWheelZoom
    {
        get => GetValue(MouseWheelZoomProperty);
        set => SetValue(MouseWheelZoomProperty, value);
    }

    private readonly MouseWheelZoomGesture _wheelZoom = new();

    private static void InitializeZoomProperties()
    {
        ZoomLevelProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) => editor.OnZoomLevelChanged((double)e.NewValue!));
    }

    private void OnZoomLevelChanged(double level)
    {
        // The state clamps and raises Changed, which writes the clamped value back (see OnZoomChanged).
        _context.Zoom.SetLevel(level);
        if (!DoubleComparison.AreClose(ZoomLevel, _context.Zoom.Level))
        {
            SetCurrentValue(ZoomLevelProperty, _context.Zoom.Level);
        }
    }

    private void OnZoomChanged(object? sender, EventArgs e)
    {
        if (!DoubleComparison.AreClose(ZoomLevel, _context.Zoom.Level))
        {
            SetCurrentValue(ZoomLevelProperty, _context.Zoom.Level);
        }

        RecreateTextMeasurer();
    }

    // The classifier has already accepted this event (see ScrollFromWheel).
    private bool TryZoomFromWheel(PointerWheelEventArgs e, double delta)
    {
        KeyModifiers keys = e.KeyModifiers;
        double? level = _wheelZoom.OnWheel(
            delta,
            _wheelClassifier.IsPhysicalMouseWheel(),
            keys.HasFlag(KeyModifiers.Control),
            keys.HasFlag(KeyModifiers.Shift),
            keys.HasFlag(KeyModifiers.Alt),
            keys.HasFlag(KeyModifiers.Meta),
            (long)e.Timestamp,
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

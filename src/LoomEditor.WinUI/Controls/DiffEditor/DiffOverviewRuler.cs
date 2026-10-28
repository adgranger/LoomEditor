using LoomEditor.Rendering;
using LoomEditor.Rendering.Win2D;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.DiffEditor;

/// <summary>
/// The strip on the right of a diff editor (Monaco: the <c>diffOverview</c> element): a canvas the diff control draws the two rulers and the
/// viewport slider on, and the pointer events that scroll the modified editor.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class DiffOverviewRuler : Grid
{
    /// <summary>The width of the whole strip, in DIPs (Monaco: <c>ENTIRE_DIFF_OVERVIEW_WIDTH</c>).</summary>
    public const double StripWidth = 30;

    private readonly CanvasControl _canvas = new();
    private bool _isDragging;

    /// <summary>Creates the strip.</summary>
    public DiffOverviewRuler()
    {
        Width = StripWidth;
        HorizontalAlignment = HorizontalAlignment.Right;
        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        Children.Add(_canvas);
        _canvas.Draw += OnDraw;
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += (_, e) => EndDrag(e.Pointer);
        PointerCaptureLost += (_, _) => _isDragging = false;
        PointerEntered += (_, _) => SetPointerOver(true);
        PointerExited += (_, _) => SetPointerOver(false);
        PointerWheelChanged += (_, e) => WheelChanged?.Invoke(this, e);
    }

    /// <summary>Draws the content: the surface, then the width and the height of the strip. Only fills shapes.</summary>
    public Action<IRenderSurface, float, float>? DrawContent { get; set; }

    /// <summary>Whether the pointer is over the strip (the slider is drawn with its hover color).</summary>
    public bool IsPointerOver { get; private set; }

    /// <summary>The device pixels per DIP.</summary>
    public double PixelRatio => XamlRoot?.RasterizationScale ?? 1;

    /// <summary>Raised when the left button goes down, with the y in the strip.</summary>
    public event EventHandler<double>? Pressed;

    /// <summary>Raised while the pointer moves during a drag, with the y in the strip.</summary>
    public event EventHandler<double>? Dragged;

    /// <summary>Raised for the mouse wheel over the strip.</summary>
    public event EventHandler<PointerRoutedEventArgs>? WheelChanged;

    /// <summary>Draws the strip again.</summary>
    public void Invalidate() => _canvas.Invalidate();

    private void OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        var surface = new Win2DRenderSurface(args.DrawingSession, null);
        DrawContent?.Invoke(surface, (float)sender.ActualWidth, (float)sender.ActualHeight);
    }

    private void SetPointerOver(bool value)
    {
        IsPointerOver = value;
        Invalidate();
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _isDragging = CapturePointer(e.Pointer);
        Pressed?.Invoke(this, e.GetCurrentPoint(this).Position.Y);
        e.Handled = true;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging)
        {
            Dragged?.Invoke(this, e.GetCurrentPoint(this).Position.Y);
            e.Handled = true;
        }
    }

    private void EndDrag(Microsoft.UI.Xaml.Input.Pointer pointer)
    {
        if (_isDragging)
        {
            _isDragging = false;
            ReleasePointerCapture(pointer);
        }
    }
}

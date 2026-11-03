using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using LoomEditor.Avalonia.Rendering;
using LoomEditor.Rendering;

namespace LoomEditor.Avalonia.Controls.DiffEditor;

/// <summary>
/// The strip on the right of a diff editor (Monaco: the <c>diffOverview</c> element): a control the diff control draws the two rulers and the
/// viewport slider on, and the pointer events that scroll the modified editor.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class DiffOverviewRuler : Control
{
    /// <summary>The width of the whole strip, in DIPs (Monaco: <c>ENTIRE_DIFF_OVERVIEW_WIDTH</c>).</summary>
    public const double StripWidth = 30;

    private bool _isDragging;
    private bool _invalidatePending;

    /// <summary>Creates the strip.</summary>
    public DiffOverviewRuler()
    {
        Width = StripWidth;
        HorizontalAlignment = HorizontalAlignment.Right;
    }

    /// <summary>Draws the content: the surface, then the width and the height of the strip. Only fills shapes.</summary>
    public Action<IRenderSurface, float, float>? DrawContent { get; set; }

    /// <summary>The device pixels per DIP.</summary>
    public double PixelRatio => TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;

    /// <summary>Raised when the left button goes down, with the y in the strip.</summary>
    public event EventHandler<double>? Pressed;

    /// <summary>Raised while the pointer moves during a drag, with the y in the strip.</summary>
    public event EventHandler<double>? Dragged;

    /// <summary>Raised for the mouse wheel over the strip.</summary>
    public event EventHandler<PointerWheelEventArgs>? WheelChanged;

    /// <summary>Draws the strip again (the slider is drawn with its hover color while <see cref="InputElement.IsPointerOver"/>).</summary>
    /// <remarks>Posted: Avalonia refuses an invalidation during the render pass, and the editors raise their events while they draw.</remarks>
    public void Invalidate()
    {
        if (_invalidatePending)
        {
            return;
        }

        _invalidatePending = true;
        Dispatcher.UIThread.Post(() =>
        {
            _invalidatePending = false;
            InvalidateVisual();
        });
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var bounds = new Rect(Bounds.Size);
        // A transparent fill so the strip takes the pointer.
        context.DrawRectangle(Brushes.Transparent, null, bounds);
        DrawContent?.Invoke(new AvaloniaRenderSurface(context, bounds, null), (float)bounds.Width, (float)bounds.Height);
    }

    /// <inheritdoc />
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _isDragging = true;
        e.Pointer.Capture(this);
        Pressed?.Invoke(this, e.GetPosition(this).Y);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDragging)
        {
            Dragged?.Invoke(this, e.GetPosition(this).Y);
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDragging = false;
        e.Pointer.Capture(null);
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isDragging = false;
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        WheelChanged?.Invoke(this, e);
    }
}

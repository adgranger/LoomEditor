using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Controls.Overlay;

/// <summary>A canvas of the control template that floats widgets above the text (hover popup, completion list, zone content).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class OverlayHost(Canvas canvas)
{
    /// <summary>Width of the canvas in DIPs.</summary>
    public double Width => canvas.Bounds.Width;

    /// <summary>Height of the canvas in DIPs.</summary>
    public double Height => canvas.Bounds.Height;

    /// <summary>
    /// Measures the control as it will show: in the canvas, visible (a hidden control measures 0 x 0) and without the limits
    /// of its last <see cref="Show"/>.
    /// </summary>
    public Size Measure(Control element)
    {
        ArgumentNullException.ThrowIfNull(element);

        Add(element);
        element.IsVisible = true;
        element.MaxWidth = double.PositiveInfinity;
        element.MaxHeight = double.PositiveInfinity;
        element.Measure(new Size(Width, Height));
        return element.DesiredSize;
    }

    /// <summary>Adds the control if needed and puts it at <paramref name="rect"/>.</summary>
    public void Show(Control element, OverlayRect rect)
    {
        ArgumentNullException.ThrowIfNull(element);

        Add(element);
        Canvas.SetLeft(element, rect.X);
        Canvas.SetTop(element, rect.Y);
        element.MaxWidth = rect.Width;
        element.MaxHeight = rect.Height;
        element.IsVisible = true;
    }

    /// <summary>Removes the control from the canvas.</summary>
    public void Remove(Control element)
    {
        ArgumentNullException.ThrowIfNull(element);
        canvas.Children.Remove(element);
    }

    /// <summary>Hides the control.</summary>
    public static void Hide(Control element)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.IsVisible = false;
    }

    /// <summary>True when <paramref name="visual"/> is the canvas or one of its descendants.</summary>
    public bool Contains(Visual? visual) => visual is not null && (ReferenceEquals(visual, canvas) || canvas.IsVisualAncestorOf(visual));

    private void Add(Control element)
    {
        if (!canvas.Children.Contains(element))
        {
            canvas.Children.Add(element);
        }
    }
}

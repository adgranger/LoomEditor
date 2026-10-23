using LoomEditor.View.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Overlay;

/// <summary>The overlay canvas of the control template: widgets that float above the text (hover popup, later the completion list).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class OverlayHost(Canvas canvas)
{
    /// <summary>Width of the canvas in DIPs.</summary>
    public double Width => canvas.ActualWidth;

    /// <summary>Height of the canvas in DIPs.</summary>
    public double Height => canvas.ActualHeight;

    /// <summary>Adds the element if needed and puts it at <paramref name="rect"/>.</summary>
    public void Show(FrameworkElement element, OverlayRect rect)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (!canvas.Children.Contains(element))
        {
            canvas.Children.Add(element);
        }

        Canvas.SetLeft(element, rect.X);
        Canvas.SetTop(element, rect.Y);
        element.MaxWidth = rect.Width;
        element.MaxHeight = rect.Height;
        element.Visibility = Visibility.Visible;
    }

    /// <summary>Collapses the element.</summary>
    public static void Hide(FrameworkElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.Visibility = Visibility.Collapsed;
    }

    /// <summary>True when <paramref name="d"/> is a child (at any depth) of the overlay canvas.</summary>
    public bool Contains(DependencyObject? d)
    {
        while (d is not null)
        {
            if (ReferenceEquals(d, canvas))
            {
                return true;
            }

            d = VisualTreeHelper.GetParent(d);
        }

        return false;
    }
}

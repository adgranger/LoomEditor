using Avalonia;
using Avalonia.Controls;

namespace LoomEditor.Avalonia.Input;

/// <summary>Converts a rectangle of a control (DIPs) to physical screen pixels, which is what the IME wants.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal static class ScreenCoordinates
{
    /// <summary>
    /// Converts <paramref name="rectInElement"/> to screen pixels, or returns <see langword="null"/> when the element is not
    /// in a window.
    /// </summary>
    public static Rect? ToScreenPixels(Visual element, Rect rectInElement)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (TopLevel.GetTopLevel(element) is not { } top)
        {
            return null;
        }

        PixelPoint origin = element.PointToScreen(rectInElement.TopLeft);
        double scale = top.RenderScaling;
        return new Rect(origin.X, origin.Y, rectInElement.Width * scale, rectInElement.Height * scale);
    }

    /// <summary>The inverse of <see cref="ToScreenPixels"/> for a point; <see langword="null"/> when the element is not in a window.</summary>
    public static Point? ToElement(Visual element, Point screenPixels)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (TopLevel.GetTopLevel(element) is null)
        {
            return null;
        }

        return element.PointToClient(new PixelPoint((int)Math.Round(screenPixels.X), (int)Math.Round(screenPixels.Y)));
    }
}

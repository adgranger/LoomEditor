using LoomEditor.WinUI.Interop;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace LoomEditor.WinUI.Input;

/// <summary>Converts a rectangle of a control (DIPs) to physical screen pixels, which is what the IME wants.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal static class ScreenCoordinates
{
    /// <summary>
    /// Converts <paramref name="rectInElement"/> to screen pixels, or returns <see langword="null"/> when the element is not
    /// in a window (ADR 0006, P3-48: <c>LayoutBounds</c> is in screen pixels).
    /// </summary>
    public static Rect? ToScreenPixels(UIElement element, Rect rectInElement)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (element.XamlRoot is not { } root)
        {
            return null;
        }

        // 1. DIPs in the window content, 2. client pixels, 3. screen pixels.
        Rect inContent = element.TransformToVisual(null).TransformBounds(rectInElement);
        double scale = root.RasterizationScale;
        nint hwnd = Win32Interop.GetWindowFromWindowId(root.ContentIslandEnvironment.AppWindowId);
        var origin = new NativeMethods.Point();
        if (!NativeMethods.ClientToScreen(hwnd, ref origin))
        {
            return null;
        }

        return new Rect(inContent.X * scale + origin.X, inContent.Y * scale + origin.Y, inContent.Width * scale, inContent.Height * scale);
    }
}

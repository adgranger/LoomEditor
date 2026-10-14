// Ported from microsoft/vscode: src/vs/base/browser/ui/scrollbar/scrollableElement.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>
/// A mouse wheel event. Monaco's <c>StandardWheelEvent</c> convention: <b>positive <see cref="DeltaY"/> scrolls up</b>,
/// one wheel notch is 1.
/// </summary>
/// <remarks>Threading: immutable value, safe from any thread.</remarks>
internal readonly record struct MouseWheelInput(double DeltaX, double DeltaY, bool ShiftKey, bool AltKey);

/// <summary>The wheel options. Defaults are Monaco's editor defaults.</summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
/// <param name="MouseWheelScrollSensitivity">Multiplier of the wheel deltas.</param>
/// <param name="FastScrollSensitivity">Multiplier applied while Alt is held.</param>
/// <param name="ScrollPredominantAxis">Whether only the axis with the largest delta scrolls.</param>
internal sealed record MouseWheelOptions(
    double MouseWheelScrollSensitivity = 1,
    double FastScrollSensitivity = 5,
    bool ScrollPredominantAxis = true);

/// <summary>
/// Turns a wheel delta into a new scroll position. Monaco: the body of <c>_onMouseWheel</c>
/// (<c>AbstractScrollableElement</c>).
/// </summary>
/// <remarks>
/// Threading: stateless, safe from any thread. Not ported: inertial scrolling, <c>scrollYToX</c>, <c>flipAxes</c>.
/// Behavior difference: the result is not validated here (Monaco validates inside <c>_onMouseWheel</c> and only counts
/// the event as a scroll if the validated position differs); the caller runs <see cref="ViewLayout.ValidateScrollPosition"/>
/// and compares with the future position.
/// </remarks>
internal static class MouseWheelScrolling
{
    private const double ScrollWheelSensitivity = 50;

    /// <summary>
    /// The scroll position the wheel asks for, or <c>null</c> when the event carries no delta. Fields that do not
    /// change are <c>null</c>.
    /// </summary>
    public static NewScrollPosition? ComputeScrollPosition(
        MouseWheelInput input, ScrollPosition futurePosition, MouseWheelOptions options)
    {
        if (input.DeltaY == 0 && input.DeltaX == 0)
        {
            return null;
        }

        double deltaY = input.DeltaY * options.MouseWheelScrollSensitivity;
        double deltaX = input.DeltaX * options.MouseWheelScrollSensitivity;

        if (options.ScrollPredominantAxis)
        {
            if (Math.Abs(deltaY) >= Math.Abs(deltaX))
            {
                deltaX = 0;
            }
            else
            {
                deltaY = 0;
            }
        }

        // Convert vertical scrolling to horizontal if shift is held (always, as this port targets Windows only).
        if (input.ShiftKey && deltaX == 0)
        {
            deltaX = deltaY;
            deltaY = 0;
        }

        if (input.AltKey)
        {
            // fastScrolling
            deltaX *= options.FastScrollSensitivity;
            deltaY *= options.FastScrollSensitivity;
        }

        double? scrollTop = null;
        double? scrollLeft = null;
        if (deltaY != 0)
        {
            scrollTop = futurePosition.ScrollTop - ToWholePixels(ScrollWheelSensitivity * deltaY);
        }

        if (deltaX != 0)
        {
            scrollLeft = futurePosition.ScrollLeft - ToWholePixels(ScrollWheelSensitivity * deltaX);
        }

        return new NewScrollPosition(scrollLeft, scrollTop);
    }

    /// <summary>Whether the wheel should animate: only a physical wheel does, a touchpad scrolls immediately.</summary>
    public static bool ShouldScrollSmoothly(MouseWheelClassifier classifier)
    {
        // Monaco: SCROLL_WHEEL_SMOOTH_SCROLL_ENABLED && options.mouseWheelSmoothScroll (both true) && classifier...
        return classifier.IsPhysicalMouseWheel();
    }

    // Here we convert values such as -0.3 to -1 or 0.3 to 1, otherwise low speed scrolling will never scroll.
    private static double ToWholePixels(double deltaScroll)
    {
        return deltaScroll < 0 ? Math.Floor(deltaScroll) : Math.Ceiling(deltaScroll);
    }
}

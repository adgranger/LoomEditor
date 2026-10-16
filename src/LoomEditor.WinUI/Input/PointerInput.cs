using LoomEditor.View.Controller;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace LoomEditor.WinUI.Input;

/// <summary>Turns WinUI pointer events into <see cref="EditorMouseEvent"/>s.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal static class PointerInput
{
    /// <summary>
    /// <see cref="EditorMouseEvent.Detail"/> of a press: WinUI reports no click count, so the control passes 4 (Monaco's highest
    /// count) and <c>MouseDownState</c> counts from the time and the position.
    /// </summary>
    public const int PressDetail = 4;

    /// <summary>Builds the event; the position is relative to <paramref name="relativeTo"/> in DIPs.</summary>
    public static EditorMouseEvent Create(PointerRoutedEventArgs e, UIElement relativeTo, int detail)
    {
        ArgumentNullException.ThrowIfNull(e);
        ArgumentNullException.ThrowIfNull(relativeTo);

        PointerPoint point = e.GetCurrentPoint(relativeTo);
        VirtualKeyModifiers modifiers = e.KeyModifiers;
        return new EditorMouseEvent(
            point.Position.X,
            point.Position.Y,
            point.Properties.IsLeftButtonPressed,
            point.Properties.IsMiddleButtonPressed,
            modifiers.HasFlag(VirtualKeyModifiers.Menu),
            modifiers.HasFlag(VirtualKeyModifiers.Control),
            modifiers.HasFlag(VirtualKeyModifiers.Shift),
            modifiers.HasFlag(VirtualKeyModifiers.Windows),
            detail,
            // Microseconds to milliseconds.
            (long)(point.Timestamp / 1000));
    }
}

using Avalonia;
using Avalonia.Input;
using LoomEditor.View.Controller;
using ViewKeyModifiers = LoomEditor.View.Commands.KeyModifiers;

namespace LoomEditor.Avalonia.Input;

/// <summary>Turns Avalonia pointer events into <see cref="EditorMouseEvent"/>s.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal static class PointerInput
{
    /// <summary>Builds the event; the position is relative to <paramref name="relativeTo"/> in DIPs.</summary>
    /// <param name="e">The pointer event.</param>
    /// <param name="relativeTo">The visual the position is relative to.</param>
    /// <param name="detail">The click count: <see cref="PointerPressedEventArgs.ClickCount"/> for a press, 1 for the other events.</param>
    public static EditorMouseEvent Create(PointerEventArgs e, Visual relativeTo, int detail)
    {
        ArgumentNullException.ThrowIfNull(e);
        ArgumentNullException.ThrowIfNull(relativeTo);

        PointerPoint point = e.GetCurrentPoint(relativeTo);
        // Cmd+click is the multi-cursor gesture on macOS: the modifiers go through the same mapping as the keys.
        ViewKeyModifiers modifiers = KeyMapping.ToModifiers(e.KeyModifiers, OperatingSystem.IsMacOS());
        return new EditorMouseEvent(
            point.Position.X,
            point.Position.Y,
            point.Properties.IsLeftButtonPressed,
            point.Properties.IsMiddleButtonPressed,
            modifiers.HasFlag(ViewKeyModifiers.Alt),
            modifiers.HasFlag(ViewKeyModifiers.Ctrl),
            modifiers.HasFlag(ViewKeyModifiers.Shift),
            modifiers.HasFlag(ViewKeyModifiers.Meta),
            detail,
            (long)e.Timestamp);
    }
}

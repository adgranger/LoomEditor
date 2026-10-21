using LoomEditor.View.Commands;
using Windows.System;

namespace LoomEditor.WinUI.Input;

/// <summary>Maps WinUI virtual keys to the keys of the keybinding table.</summary>
/// <remarks>Threading: pure functions; thread-safe.</remarks>
internal static class KeyMapping
{
    /// <summary>Returns the key code, or <c>null</c> for a key that no default keybinding uses.</summary>
    public static KeyCode? ToKeyCode(VirtualKey key) => key switch
    {
        VirtualKey.Back => KeyCode.Backspace,
        VirtualKey.Tab => KeyCode.Tab,
        VirtualKey.Enter => KeyCode.Enter,
        VirtualKey.Escape => KeyCode.Escape,
        VirtualKey.Space => KeyCode.Space,
        VirtualKey.PageUp => KeyCode.PageUp,
        VirtualKey.PageDown => KeyCode.PageDown,
        VirtualKey.End => KeyCode.End,
        VirtualKey.Home => KeyCode.Home,
        VirtualKey.Left => KeyCode.LeftArrow,
        VirtualKey.Up => KeyCode.UpArrow,
        VirtualKey.Right => KeyCode.RightArrow,
        VirtualKey.Down => KeyCode.DownArrow,
        VirtualKey.Insert => KeyCode.Insert,
        VirtualKey.Delete => KeyCode.Delete,
        (VirtualKey)220 => KeyCode.Backslash, // VK_OEM_5
        (VirtualKey)191 => KeyCode.Slash, // VK_OEM_2
        (VirtualKey)219 => KeyCode.BracketLeft, // VK_OEM_4
        (VirtualKey)221 => KeyCode.BracketRight, // VK_OEM_6
        >= VirtualKey.A and <= VirtualKey.Z => KeyCode.KeyA + (key - VirtualKey.A),
        >= VirtualKey.F1 and <= VirtualKey.F12 => KeyCode.F1 + (key - VirtualKey.F1),
        _ => null,
    };
}

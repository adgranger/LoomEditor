using Avalonia.Input;
using LoomEditor.View.Commands;
using AvaloniaKeyModifiers = global::Avalonia.Input.KeyModifiers;
using ViewKeyModifiers = LoomEditor.View.Commands.KeyModifiers;

namespace LoomEditor.Avalonia.Input;

/// <summary>Maps Avalonia keys and modifiers to the keys and modifiers of the keybinding table.</summary>
/// <remarks>Threading: pure functions; thread-safe.</remarks>
internal static class KeyMapping
{
    /// <summary>Returns the key code, or <c>null</c> for a key that no default keybinding uses.</summary>
    public static KeyCode? ToKeyCode(Key key) => key switch
    {
        Key.Back => KeyCode.Backspace,
        Key.Tab => KeyCode.Tab,
        Key.Enter => KeyCode.Enter,
        Key.Escape => KeyCode.Escape,
        Key.Space => KeyCode.Space,
        Key.PageUp => KeyCode.PageUp,
        Key.PageDown => KeyCode.PageDown,
        Key.End => KeyCode.End,
        Key.Home => KeyCode.Home,
        Key.Left => KeyCode.LeftArrow,
        Key.Up => KeyCode.UpArrow,
        Key.Right => KeyCode.RightArrow,
        Key.Down => KeyCode.DownArrow,
        Key.Insert => KeyCode.Insert,
        Key.Delete => KeyCode.Delete,
        Key.OemPipe => KeyCode.Backslash, // the \| key
        Key.OemQuestion => KeyCode.Slash,
        Key.OemOpenBrackets => KeyCode.BracketLeft,
        Key.OemCloseBrackets => KeyCode.BracketRight,
        Key.OemMinus => KeyCode.Minus,
        Key.OemPlus => KeyCode.Equal, // the = key on a US layout
        Key.OemComma => KeyCode.Comma,
        Key.OemPeriod => KeyCode.Period,
        >= Key.A and <= Key.Z => KeyCode.KeyA + (key - Key.A),
        >= Key.F1 and <= Key.F12 => KeyCode.F1 + (key - Key.F1),
        >= Key.D0 and <= Key.D9 => KeyCode.Digit0 + (key - Key.D0),
        _ => null,
    };

    /// <summary>
    /// Maps the modifiers of a key event. On macOS the Command key (<c>Meta</c>) plays the part of Ctrl, and the Control key the part of Meta
    /// (Monaco's <c>CtrlCmd</c>); elsewhere Control is Ctrl and Meta is Meta. Alt and Shift map directly.
    /// </summary>
    /// <param name="modifiers">The Avalonia modifiers.</param>
    /// <param name="isMacOS">Whether the application runs on macOS (a parameter so that tests cover both systems).</param>
    public static ViewKeyModifiers ToModifiers(AvaloniaKeyModifiers modifiers, bool isMacOS)
    {
        ViewKeyModifiers result = ViewKeyModifiers.None;
        if (modifiers.HasFlag(AvaloniaKeyModifiers.Control))
        {
            result |= isMacOS ? ViewKeyModifiers.Meta : ViewKeyModifiers.Ctrl;
        }

        if (modifiers.HasFlag(AvaloniaKeyModifiers.Meta))
        {
            result |= isMacOS ? ViewKeyModifiers.Ctrl : ViewKeyModifiers.Meta;
        }

        if (modifiers.HasFlag(AvaloniaKeyModifiers.Shift))
        {
            result |= ViewKeyModifiers.Shift;
        }

        if (modifiers.HasFlag(AvaloniaKeyModifiers.Alt))
        {
            result |= ViewKeyModifiers.Alt;
        }

        return result;
    }
}

using LoomEditor.View.Commands;
using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;

namespace LoomEditor.WinUI.Input;

/// <summary>
/// Turns key presses into command ids (through the default keybindings) and runs them.
/// </summary>
/// <remarks>
/// <para>
/// The key and the modifiers become a <see cref="Keybinding"/>, <see cref="KeybindingDispatcher"/> picks the command for the
/// current <see cref="KeybindingContext"/> (and remembers the first key press of a two-key chord), and
/// <see cref="CommandRegistry.Execute"/> runs it.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class KeyboardInput(CommandRegistry commands)
{
    private readonly KeybindingDispatcher _dispatcher = new(KeybindingResolver.Default);

    /// <summary>Runs the command bound to <paramref name="key"/> (with the current modifiers), if any.</summary>
    /// <returns>True if the key press was handled (a command ran, or a chord is in progress); the key event should then be marked as handled.</returns>
    public bool TryHandle(VirtualKey key, KeybindingContext context)
    {
        if (KeyMapping.ToKeyCode(key) is not { } keyCode)
        {
            // A key that no rule uses ends a chord; pressing a modifier on its way to the second key does not.
            if (_dispatcher.InChordMode && !IsModifier(key))
            {
                _dispatcher.LeaveChordMode();
                return true;
            }

            return false;
        }

        KeyModifiers modifiers = KeyModifiers.None;
        if (IsDown(VirtualKey.Control))
        {
            modifiers |= KeyModifiers.Ctrl;
        }

        if (IsDown(VirtualKey.Shift))
        {
            modifiers |= KeyModifiers.Shift;
        }

        if (IsDown(VirtualKey.Menu))
        {
            modifiers |= KeyModifiers.Alt;
        }

        if (IsDown(VirtualKey.LeftWindows) || IsDown(VirtualKey.RightWindows))
        {
            modifiers |= KeyModifiers.Meta;
        }

        bool handled = _dispatcher.Dispatch(new Keybinding(modifiers, keyCode), context, out string? commandId);
        if (commandId is not null)
        {
            commands.Execute(commandId);
        }

        return handled;
    }

    /// <summary>Forgets the first key press of a chord, e.g. when the editor loses focus.</summary>
    public void LeaveChordMode() => _dispatcher.LeaveChordMode();

    private static bool IsModifier(VirtualKey key) =>
        key is VirtualKey.Control or VirtualKey.LeftControl or VirtualKey.RightControl
            or VirtualKey.Shift or VirtualKey.LeftShift or VirtualKey.RightShift
            or VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu
            or VirtualKey.LeftWindows or VirtualKey.RightWindows;

    private static bool IsDown(VirtualKey key) =>
        InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
}

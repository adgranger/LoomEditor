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
/// The key and the modifiers become a <see cref="Keybinding"/>, <see cref="KeybindingResolver"/> picks the command for the
/// current <see cref="KeybindingContext"/>, and <see cref="CommandRegistry.Execute"/> runs it.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class KeyboardInput(CommandRegistry commands)
{
    private readonly KeybindingResolver _resolver = KeybindingResolver.Default;

    /// <summary>Runs the command bound to <paramref name="key"/> (with the current modifiers), if any.</summary>
    /// <returns>True if a command ran; the key event should then be marked as handled.</returns>
    public bool TryHandle(VirtualKey key, KeybindingContext context)
    {
        if (KeyMapping.ToKeyCode(key) is not { } keyCode)
        {
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

        string? commandId = _resolver.Resolve(new Keybinding(modifiers, keyCode), context);
        return commandId is not null && commands.Execute(commandId);
    }

    private static bool IsDown(VirtualKey key) =>
        InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
}

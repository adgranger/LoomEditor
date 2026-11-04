using Avalonia.Input;
using LoomEditor.View.Commands;
using ViewKeyModifiers = LoomEditor.View.Commands.KeyModifiers;

namespace LoomEditor.Avalonia.Input;

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

    /// <summary>Runs the command bound to <paramref name="key"/> with <paramref name="modifiers"/>, if any.</summary>
    /// <param name="key">The key.</param>
    /// <param name="modifiers">The modifiers held, already mapped by <see cref="KeyMapping.ToModifiers"/>.</param>
    /// <param name="context">The editor state the keybindings' conditions read.</param>
    /// <param name="filter">When given, only a command whose id it accepts runs; any other key press is left unhandled (and never starts a chord).</param>
    /// <returns>True if the key press was handled (a command ran, or a chord is in progress); the key event should then be marked as handled.</returns>
    public bool TryHandle(Key key, ViewKeyModifiers modifiers, KeybindingContext context, Func<string, bool>? filter = null)
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

        bool handled = _dispatcher.Dispatch(new Keybinding(modifiers, keyCode), context, out string? commandId);
        if (filter is not null && (commandId is null ? handled : !filter(commandId)))
        {
            _dispatcher.LeaveChordMode();
            return false;
        }

        if (commandId is not null)
        {
            commands.Execute(commandId);
        }

        return handled;
    }

    /// <summary>Forgets the first key press of a chord, e.g. when the editor loses focus.</summary>
    public void LeaveChordMode() => _dispatcher.LeaveChordMode();

    private static bool IsModifier(Key key) =>
        key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin;
}

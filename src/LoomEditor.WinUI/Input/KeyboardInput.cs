using LoomEditor.View.Commands;
using LoomEditor.View.Features.SelectAll;
using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;

namespace LoomEditor.WinUI.Input;

/// <summary>
/// Turns key presses into command ids and runs them.
/// </summary>
/// <remarks>
/// <para>
/// Phase 0: a hard-coded table with a few shortcuts. Phase 3 moves the table to
/// <c>LoomEditor.View/Commands/DefaultKeybindings.cs</c> (UI-independent and testable).
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class KeyboardInput(CommandRegistry commands)
{
    /// <summary>Shortcuts that use the Ctrl key, mapped to command ids.</summary>
    private static readonly Dictionary<VirtualKey, string> CtrlShortcuts = new()
    {
        [VirtualKey.A] = SelectAllCommands.SelectAll,
    };

    /// <summary>Runs the command bound to <paramref name="key"/> (with the current modifiers), if any.</summary>
    /// <returns>True if a command ran; the key event should then be marked as handled.</returns>
    public bool TryHandle(VirtualKey key)
    {
        if (!IsDown(VirtualKey.Control) || IsDown(VirtualKey.Menu) || IsDown(VirtualKey.Shift))
        {
            return false;
        }

        return CtrlShortcuts.TryGetValue(key, out string? commandId) && commands.Execute(commandId);
    }

    private static bool IsDown(VirtualKey key) =>
        InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
}

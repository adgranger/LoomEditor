// Ported from microsoft/vscode: src/vs/platform/keybinding/common/abstractKeybindingService.ts (_doDispatch, _expectAnotherChord, _leaveChordMode)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Commands;

/// <summary>Resolves key presses to commands and remembers the first key press of a chord (Monaco: <c>AbstractKeybindingService._doDispatch</c>).</summary>
/// <remarks>
/// <para>
/// Behavior differences with Monaco: no status bar message while waiting for the second key press; no 5-second timeout
/// (<c>_scheduleLeaveChordMode</c>): chord mode ends on the next key press or when the editor loses focus (the host calls
/// <see cref="LeaveChordMode"/>).
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class KeybindingDispatcher(KeybindingResolver resolver)
{
    private Keybinding? _currentChord;

    /// <summary>True after the first key press of a chord, until the next key press.</summary>
    public bool InChordMode => _currentChord is not null;

    /// <summary>Handles a key press.</summary>
    /// <param name="keypress">The key press.</param>
    /// <param name="context">The editor state the rules' conditions read.</param>
    /// <param name="commandId">The command to run, or <see langword="null"/>.</param>
    /// <returns>True if the key press was handled (Monaco: <c>shouldPreventDefault</c>).</returns>
    public bool Dispatch(Keybinding keypress, KeybindingContext context, out string? commandId)
    {
        commandId = null;
        KeybindingResolution resolution = resolver.Resolve(context, _currentChord, keypress);
        switch (resolution.Kind)
        {
            case KeybindingResolutionKind.MoreChordsNeeded:
                _currentChord = keypress;
                return true;

            case KeybindingResolutionKind.KbFound:
                LeaveChordMode();
                commandId = resolution.CommandId;
                return true;

            default:
                // A key press that cannot end the chord is swallowed, and ends chord mode.
                bool wasInChordMode = InChordMode;
                LeaveChordMode();
                return wasInChordMode;
        }
    }

    /// <summary>Forgets the first key press of a chord (Monaco: <c>_leaveChordMode</c>).</summary>
    public void LeaveChordMode() => _currentChord = null;
}

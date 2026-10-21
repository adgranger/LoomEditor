namespace LoomEditor.View.Commands;

/// <summary>A keyboard shortcut for a command, active when <paramref name="When"/> is true.</summary>
/// <remarks>Threading: immutable; thread-safe as long as <paramref name="When"/> is.</remarks>
/// <param name="Keybinding">The key press.</param>
/// <param name="CommandId">The id of the command to run.</param>
/// <param name="When">The condition (Monaco's <c>precondition AND kbExpr</c>); <c>null</c> means always.</param>
/// <param name="Weight">See <see cref="KeybindingWeight"/>.</param>
/// <param name="Chord">The second key press of a two-key chord (<c>Ctrl+K Ctrl+C</c>); <c>null</c> for a single key press.</param>
internal sealed record KeybindingRule(
    Keybinding Keybinding,
    string CommandId,
    Func<KeybindingContext, bool>? When,
    int Weight,
    Keybinding? Chord = null);

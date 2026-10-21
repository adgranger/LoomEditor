namespace LoomEditor.View.Commands;

/// <summary>One key press with its modifiers. A chord (<c>Ctrl+K Ctrl+C</c>) is two of them: see <see cref="KeybindingRule.Chord"/>.</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal readonly record struct Keybinding(KeyModifiers Modifiers, KeyCode Key);

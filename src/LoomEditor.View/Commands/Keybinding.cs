namespace LoomEditor.View.Commands;

/// <summary>One key press with its modifiers. There are no chords (<c>Ctrl+K Ctrl+D</c>) in phase 3.</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal readonly record struct Keybinding(KeyModifiers Modifiers, KeyCode Key);

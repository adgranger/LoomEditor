namespace LoomEditor.View.Commands;

/// <summary>The editor state that a keybinding's <c>when</c> condition reads (Monaco's context keys).</summary>
/// <remarks>
/// Monaco's names are in the parameter docs. The control fills this record for every key press (P3-41).
/// Threading: immutable; thread-safe.
/// </remarks>
/// <param name="TextInputFocus"><c>textInputFocus</c>: the text input has the focus.</param>
/// <param name="EditorTextFocus"><c>editorTextFocus</c>: the editor's text has the focus.</param>
/// <param name="HasNonEmptySelection"><c>editorHasSelection</c>: some selection is not empty.</param>
/// <param name="HasMultipleSelections"><c>editorHasMultipleSelections</c>: there are several cursors.</param>
/// <param name="ColumnSelection"><c>editorColumnSelection</c>: the <c>columnSelection</c> option.</param>
/// <param name="Writable"><c>!editorReadonly</c>.</param>
/// <param name="TabDoesNotMoveFocus"><c>editorTabMovesFocus</c> is false.</param>
/// <param name="AccessibilityModeEnabled"><c>accessibilityModeEnabled</c>.</param>
internal readonly record struct KeybindingContext(
    bool TextInputFocus = false,
    bool EditorTextFocus = false,
    bool HasNonEmptySelection = false,
    bool HasMultipleSelections = false,
    bool ColumnSelection = false,
    bool Writable = false,
    bool TabDoesNotMoveFocus = false,
    bool AccessibilityModeEnabled = false);

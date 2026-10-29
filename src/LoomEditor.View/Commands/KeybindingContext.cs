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
/// <param name="EditorReadOnly"><c>editorReadonly</c>: user edits are ignored (the default, false, is a writable editor).</param>
/// <param name="TabDoesNotMoveFocus"><c>editorTabMovesFocus</c> is false.</param>
/// <param name="AccessibilityModeEnabled"><c>accessibilityModeEnabled</c>.</param>
/// <param name="EditorFocus"><c>editorFocus</c>: the editor (or its find widget) has the focus.</param>
/// <param name="FindWidgetVisible"><c>findWidgetVisible</c>: the find widget is revealed.</param>
/// <param name="FindInputFocused"><c>findInputFocussed</c>: the find box has the focus.</param>
/// <param name="ReplaceInputFocused"><c>replaceInputFocussed</c>: the replace box has the focus.</param>
/// <param name="HoverVisible"><c>editorHoverVisible</c>: a hover popup is shown.</param>
/// <param name="SuggestWidgetVisible"><c>suggestWidgetVisible</c>: the completion list is shown.</param>
/// <param name="SuggestWidgetMultipleSuggestions"><c>suggestWidgetMultipleSuggestions</c>: the completion list has several items.</param>
/// <param name="HasCompletionItemProvider"><c>editorHasCompletionItemProvider</c>: a provider is registered for the language of the model.</param>
/// <param name="HasDefinitionProvider"><c>editorHasDefinitionProvider</c>: a provider is registered for the language of the model.</param>
/// <param name="IsInDiffEditor"><c>isInDiffEditor</c>: the editor is a side of a diff editor.</param>
/// <param name="SelectionAnchorSet"><c>selectionAnchorSet</c>: the selection anchor exists.</param>
/// <param name="MarkersNavigationVisible"><c>markersNavigationVisible</c>: a go to problem session is active.</param>
/// <param name="InSnippetMode"><c>inSnippetMode</c>: a snippet with tab stops is active.</param>
/// <param name="HasNextTabstop"><c>hasNextTabstop</c>: the active snippet has a next tab stop.</param>
/// <param name="HasPrevTabstop"><c>hasPrevTabstop</c>: the active snippet has a previous tab stop.</param>
internal readonly record struct KeybindingContext(
    bool TextInputFocus = false,
    bool EditorTextFocus = false,
    bool HasNonEmptySelection = false,
    bool HasMultipleSelections = false,
    bool ColumnSelection = false,
    bool EditorReadOnly = false,
    bool TabDoesNotMoveFocus = false,
    bool AccessibilityModeEnabled = false,
    bool EditorFocus = false,
    bool FindWidgetVisible = false,
    bool FindInputFocused = false,
    bool ReplaceInputFocused = false,
    bool HoverVisible = false,
    bool SuggestWidgetVisible = false,
    bool SuggestWidgetMultipleSuggestions = false,
    bool HasCompletionItemProvider = false,
    bool HasDefinitionProvider = false,
    bool IsInDiffEditor = false,
    bool SelectionAnchorSet = false,
    bool InSnippetMode = false,
    bool HasNextTabstop = false,
    bool HasPrevTabstop = false,
    bool MarkersNavigationVisible = false);

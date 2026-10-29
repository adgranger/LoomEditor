// Ported from microsoft/vscode: src/vs/editor/common/editorContextKeys.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Commands;

/// <summary>
/// The <c>when</c> conditions of the default keybindings, named after Monaco's context keys.
/// There is no expression parser: each condition is a C# predicate (ADR 0006, decision 8).
/// </summary>
/// <remarks>Threading: pure functions; thread-safe.</remarks>
internal static class WhenClauses
{
    /// <summary><c>writable</c> (<c>!editorReadonly</c>): the precondition of the editing actions.</summary>
    public static bool Writable(KeybindingContext c) => !c.EditorReadOnly;

    /// <summary><c>editorTextFocus</c> (kbExpr of the multi-cursor commands).</summary>
    public static bool EditorTextFocus(KeybindingContext c) => c.EditorTextFocus;

    /// <summary><c>textInputFocus</c>.</summary>
    public static bool TextInputFocus(KeybindingContext c) => c.TextInputFocus;

    /// <summary><c>hasNonEmptySelection AND textInputFocus</c> (<c>cancelSelection</c>).</summary>
    public static bool CancelSelection(KeybindingContext c) => c.HasNonEmptySelection && c.TextInputFocus;

    /// <summary><c>hasMultipleSelections AND textInputFocus</c> (<c>removeSecondaryCursors</c>).</summary>
    public static bool RemoveSecondaryCursors(KeybindingContext c) => c.HasMultipleSelections && c.TextInputFocus;

    /// <summary><c>textInputFocus AND columnSelection</c>.</summary>
    public static bool ColumnSelection(KeybindingContext c) => c.TextInputFocus && c.ColumnSelection;

    /// <summary><c>writable AND editorTextFocus AND tabDoesNotMoveFocus</c> (<c>tab</c>, <c>outdent</c>).</summary>
    public static bool Tab(KeybindingContext c) => !c.EditorReadOnly && c.EditorTextFocus && c.TabDoesNotMoveFocus;

    /// <summary><c>writable AND editorTextFocus</c> (the comment commands).</summary>
    public static bool WritableEditorTextFocus(KeybindingContext c) => !c.EditorReadOnly && c.EditorTextFocus;

    /// <summary><c>writable AND textInputFocus</c> (word deletes).</summary>
    public static bool WritableTextInput(KeybindingContext c) => !c.EditorReadOnly && c.TextInputFocus;

    /// <summary>
    /// <c>textInputFocus AND NOT (accessibilityModeEnabled AND isWindows)</c> (word moves). The default keybindings are the
    /// Windows table, so <c>isWindows</c> is true.
    /// </summary>
    public static bool WordMove(KeybindingContext c) => c.TextInputFocus && !c.AccessibilityModeEnabled;

    /// <summary><c>editorFocus</c> (kbExpr of most find keys).</summary>
    public static bool EditorFocus(KeybindingContext c) => c.EditorFocus;

    /// <summary><c>editorFocus AND findWidgetVisible</c>.</summary>
    public static bool FindWidgetVisible(KeybindingContext c) => c.EditorFocus && c.FindWidgetVisible;

    /// <summary><c>editorFocus AND findInputFocussed</c>.</summary>
    public static bool FindInputFocused(KeybindingContext c) => c.EditorFocus && c.FindInputFocused;

    /// <summary><c>editorFocus AND findWidgetVisible AND replaceInputFocussed</c>.</summary>
    public static bool ReplaceInputFocused(KeybindingContext c) => c.EditorFocus && c.FindWidgetVisible && c.ReplaceInputFocused;

    /// <summary><c>editorHoverVisible</c>.</summary>
    public static bool HoverVisible(KeybindingContext c) => c.HoverVisible;

    /// <summary><c>suggestWidgetVisible AND textInputFocus</c> (Tab, Enter, Escape while the list is shown).</summary>
    public static bool SuggestVisible(KeybindingContext c) => c.SuggestWidgetVisible && c.TextInputFocus;

    /// <summary><c>suggestWidgetVisible AND suggestWidgetMultipleSuggestions AND textInputFocus</c> (the selection keys).</summary>
    public static bool SuggestNavigation(KeybindingContext c) => c.SuggestWidgetVisible && c.SuggestWidgetMultipleSuggestions && c.TextInputFocus;

    /// <summary><c>textInputFocus AND writable AND editorHasCompletionItemProvider AND NOT suggestWidgetVisible</c> (<c>editor.action.triggerSuggest</c>).</summary>
    public static bool TriggerSuggest(KeybindingContext c) => c.TextInputFocus && !c.EditorReadOnly && c.HasCompletionItemProvider && !c.SuggestWidgetVisible;

    /// <summary><c>editorHasDefinitionProvider AND editorTextFocus</c> (<c>editor.action.revealDefinition</c>).</summary>
    public static bool GotoDefinition(KeybindingContext c) => c.EditorTextFocus && c.HasDefinitionProvider;

    /// <summary><c>inSnippetMode AND hasNextTabstop AND textInputFocus</c> (Tab).</summary>
    public static bool SnippetNext(KeybindingContext c) => c.InSnippetMode && c.HasNextTabstop && c.TextInputFocus;

    /// <summary><c>inSnippetMode AND hasPrevTabstop AND textInputFocus</c> (Shift+Tab).</summary>
    public static bool SnippetPrev(KeybindingContext c) => c.InSnippetMode && c.HasPrevTabstop && c.TextInputFocus;

    /// <summary><c>inSnippetMode AND textInputFocus</c> (Escape).</summary>
    public static bool InSnippet(KeybindingContext c) => c.InSnippetMode && c.TextInputFocus;

    /// <summary><c>selectionAnchorSet AND editorTextFocus</c> (<c>selectFromAnchorToCursor</c>, <c>cancelSelectionAnchor</c>).</summary>
    public static bool SelectionAnchorSet(KeybindingContext c) => c.SelectionAnchorSet && c.EditorTextFocus;

    /// <summary><c>isInDiffEditor</c> (the diff navigation keys).</summary>
    public static bool InDiffEditor(KeybindingContext c) => c.IsInDiffEditor;
}

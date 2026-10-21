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
    public static bool Tab(KeybindingContext c) => c.Writable && c.EditorTextFocus && c.TabDoesNotMoveFocus;

    /// <summary><c>writable AND editorTextFocus</c> (the comment commands).</summary>
    public static bool WritableEditorTextFocus(KeybindingContext c) => c.Writable && c.EditorTextFocus;

    /// <summary><c>writable AND textInputFocus</c> (word deletes).</summary>
    public static bool WritableTextInput(KeybindingContext c) => c.Writable && c.TextInputFocus;

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
}

// Ported from microsoft/vscode: src/vs/editor/browser/coreCommands.ts (kbOpts of the commands)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Features.Folding;
using LoomEditor.View.Features.DiffNavigation;
using LoomEditor.View.Features.GotoDefinition;
using LoomEditor.View.Features.Hover;
using LoomEditor.View.Features.LineSelection;
using LoomEditor.View.Features.CursorUndo;
using LoomEditor.View.Features.Overtype;
using LoomEditor.View.Features.SelectionAnchor;
using LoomEditor.View.Features.SmartSelect;
using LoomEditor.View.Features.GotoError;
using LoomEditor.View.Features.Snippet;
using LoomEditor.View.Features.Suggest;
using LoomEditor.View.Features.TabFocus;
using LoomEditor.View.Features.WordHighlighter;

namespace LoomEditor.View.Commands;

/// <summary>
/// VS Code's default Windows shortcuts for the commands ported so far, with their <c>when</c> conditions and weights.
/// </summary>
/// <remarks>
/// <para>
/// The sources are the <c>kbOpts</c> of <c>coreCommands.ts</c>, <c>editorExtensions.ts</c> (undo, redo, select all),
/// <c>wordOperations.ts</c>, <c>multicursor.ts</c> and <c>clipboard.ts</c>. For Windows, a <c>win</c> override is used when
/// Monaco has one, otherwise <c>primary</c> and <c>secondary</c>; <c>mac</c> and <c>linux</c> are ignored.
/// The <c>when</c> of a rule is Monaco's <c>precondition AND kbExpr</c>. Rules are listed in Monaco's registration order,
/// which breaks ties between equal weights (see <see cref="KeybindingResolver"/>).
/// </para>
/// <para>Threading: immutable after the type initializer; thread-safe.</para>
/// </remarks>
internal static class DefaultKeybindings
{
    /// <summary>Command id of cut (registered with the Windows clipboard, P3-47).</summary>
    public const string ClipboardCut = "editor.action.clipboardCutAction";

    /// <summary>Command id of copy (registered with the Windows clipboard, P3-47).</summary>
    public const string ClipboardCopy = "editor.action.clipboardCopyAction";

    /// <summary>Command id of paste (registered with the Windows clipboard, P3-47).</summary>
    public const string ClipboardPaste = "editor.action.clipboardPasteAction";

    /// <summary>The rules for Windows, in registration order.</summary>
    public static IReadOnlyList<KeybindingRule> Windows { get; } = Build();

    private static List<KeybindingRule> Build()
    {
        var rules = new List<KeybindingRule>();

        void Add(KeyModifiers modifiers, KeyCode key, string id, Func<KeybindingContext, bool>? when, int weight = KeybindingWeight.EditorCore) =>
            rules.Add(new KeybindingRule(new Keybinding(modifiers, key), id, when, weight));

        // Moves and their selecting variants: a key and the same key with Shift.
        void Pair(KeyCode key, string move, string select, KeyModifiers extra = KeyModifiers.None)
        {
            Add(extra, key, move, WhenClauses.TextInputFocus);
            Add(extra | KeyModifiers.Shift, key, select, WhenClauses.TextInputFocus);
        }

        Pair(KeyCode.LeftArrow, "cursorLeft", "cursorLeftSelect");
        Pair(KeyCode.RightArrow, "cursorRight", "cursorRightSelect");
        Pair(KeyCode.UpArrow, "cursorUp", "cursorUpSelect");
        Pair(KeyCode.DownArrow, "cursorDown", "cursorDownSelect");
        Pair(KeyCode.PageUp, "cursorPageUp", "cursorPageUpSelect");
        Pair(KeyCode.PageDown, "cursorPageDown", "cursorPageDownSelect");
        Pair(KeyCode.Home, "cursorHome", "cursorHomeSelect");
        Pair(KeyCode.End, "cursorEnd", "cursorEndSelect");
        Pair(KeyCode.Home, "cursorTop", "cursorTopSelect", KeyModifiers.Ctrl);
        Pair(KeyCode.End, "cursorBottom", "cursorBottomSelect", KeyModifiers.Ctrl);

        // Monaco: cursorUpSelect and cursorDownSelect also have Ctrl+Shift+Up/Down as a secondary key (not on Linux or macOS).
        // wordHighlighter.ts (Contrib): F7 and Shift+F7 go through the highlights.
        Add(KeyModifiers.None, KeyCode.F7, WordHighlighterCommands.Next, WhenClauses.EditorTextFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Shift, KeyCode.F7, WordHighlighterCommands.Prev, WhenClauses.EditorTextFocus, KeybindingWeight.EditorContrib);

        Add(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.UpArrow, "cursorUpSelect", WhenClauses.TextInputFocus);
        Add(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.DownArrow, "cursorDownSelect", WhenClauses.TextInputFocus);

        // Column selection: Ctrl+Shift+Alt+key always (only Linux disables it), Shift+key when the columnSelection option is on.
        (KeyCode Key, string Id)[] columnSelect =
        [
            (KeyCode.LeftArrow, "cursorColumnSelectLeft"),
            (KeyCode.RightArrow, "cursorColumnSelectRight"),
            (KeyCode.UpArrow, "cursorColumnSelectUp"),
            (KeyCode.PageUp, "cursorColumnSelectPageUp"),
            (KeyCode.DownArrow, "cursorColumnSelectDown"),
            (KeyCode.PageDown, "cursorColumnSelectPageDown"),
        ];
        foreach ((KeyCode key, string id) in columnSelect)
        {
            Add(KeyModifiers.Ctrl | KeyModifiers.Shift | KeyModifiers.Alt, key, id, WhenClauses.TextInputFocus);
        }

        foreach ((KeyCode key, string id) in columnSelect)
        {
            Add(KeyModifiers.Shift, key, id, WhenClauses.ColumnSelection, KeybindingWeight.EditorCore + 1);
        }

        // Scrolling: Windows overrides the page keys (Alt instead of Ctrl).
        Add(KeyModifiers.Ctrl, KeyCode.UpArrow, "scrollLineUp", WhenClauses.TextInputFocus);
        Add(KeyModifiers.Alt, KeyCode.PageUp, "scrollPageUp", WhenClauses.TextInputFocus);
        Add(KeyModifiers.Ctrl, KeyCode.DownArrow, "scrollLineDown", WhenClauses.TextInputFocus);
        Add(KeyModifiers.Alt, KeyCode.PageDown, "scrollPageDown", WhenClauses.TextInputFocus);

        // Escape: cancelSelection (primary Esc, secondary Shift+Esc) and removeSecondaryCursors, which is one weight heavier.
        Add(KeyModifiers.None, KeyCode.Escape, "cancelSelection", WhenClauses.CancelSelection);
        Add(KeyModifiers.Shift, KeyCode.Escape, "cancelSelection", WhenClauses.CancelSelection);
        Add(KeyModifiers.None, KeyCode.Escape, "removeSecondaryCursors", WhenClauses.RemoveSecondaryCursors, KeybindingWeight.EditorCore + 1);
        Add(KeyModifiers.Shift, KeyCode.Escape, "removeSecondaryCursors", WhenClauses.RemoveSecondaryCursors, KeybindingWeight.EditorCore + 1);

        Add(KeyModifiers.None, KeyCode.Tab, "tab", WhenClauses.Tab);
        Add(KeyModifiers.Shift, KeyCode.Tab, "outdent", WhenClauses.Tab);
        Add(KeyModifiers.None, KeyCode.Backspace, "deleteLeft", WhenClauses.TextInputFocus);
        Add(KeyModifiers.Shift, KeyCode.Backspace, "deleteLeft", WhenClauses.TextInputFocus);
        Add(KeyModifiers.None, KeyCode.Delete, "deleteRight", WhenClauses.TextInputFocus);

        // editorExtensions.ts: no kbExpr, no precondition.
        Add(KeyModifiers.Ctrl, KeyCode.KeyZ, "undo", null);
        Add(KeyModifiers.Ctrl, KeyCode.KeyY, "redo", null);
        Add(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.KeyZ, "redo", null);
        Add(KeyModifiers.Ctrl, KeyCode.KeyA, "editor.action.selectAll", null);

        // wordOperations.ts (Contrib). Monaco binds Ctrl+Left to cursorWordLeft and Ctrl+Right to cursorWordEndRight.
        Add(KeyModifiers.Ctrl, KeyCode.LeftArrow, "cursorWordLeft", WhenClauses.WordMove, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.LeftArrow, "cursorWordLeftSelect", WhenClauses.WordMove, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl, KeyCode.RightArrow, "cursorWordEndRight", WhenClauses.WordMove, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.RightArrow, "cursorWordEndRightSelect", WhenClauses.WordMove, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl, KeyCode.Backspace, "deleteWordLeft", WhenClauses.WritableTextInput, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl, KeyCode.Delete, "deleteWordRight", WhenClauses.WritableTextInput, KeybindingWeight.EditorContrib);

        // multicursor.ts (Contrib): on Windows only the primary keys apply (the Linux override does not).
        Add(KeyModifiers.Ctrl | KeyModifiers.Alt, KeyCode.UpArrow, "editor.action.insertCursorAbove", WhenClauses.EditorTextFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl | KeyModifiers.Alt, KeyCode.DownArrow, "editor.action.insertCursorBelow", WhenClauses.EditorTextFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Shift | KeyModifiers.Alt, KeyCode.KeyI, "editor.action.insertCursorAtEndOfEachLineSelected", WhenClauses.EditorTextFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl, KeyCode.KeyD, "editor.action.addSelectionToNextFindMatch", WhenClauses.EditorFocus, KeybindingWeight.EditorContrib);
        rules.Add(new KeybindingRule(
            new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyK), "editor.action.moveSelectionToNextFindMatch", WhenClauses.EditorFocus, KeybindingWeight.EditorContrib, new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyD)));
        Add(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.KeyL, "editor.action.selectHighlights", WhenClauses.EditorFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl, KeyCode.F2, "editor.action.changeAll", WhenClauses.WritableEditorTextFocus, KeybindingWeight.EditorContrib);

        // bracketMatching.ts (Contrib): jumpToBracket and removeBrackets (selectToBracket has no keybinding).
        Add(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.Backslash, "editor.action.jumpToBracket", WhenClauses.EditorTextFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl | KeyModifiers.Alt, KeyCode.Backspace, "editor.action.removeBrackets", WhenClauses.EditorTextFocus, KeybindingWeight.EditorContrib);

        // comment.ts (Contrib): the chords are Ctrl+K then Ctrl+C or Ctrl+U; the block comment has a Linux override that does not apply.
        rules.Add(new KeybindingRule(new Keybinding(KeyModifiers.Ctrl, KeyCode.Slash), "editor.action.commentLine", WhenClauses.WritableEditorTextFocus, KeybindingWeight.EditorContrib));
        rules.Add(new KeybindingRule(
            new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyK), "editor.action.addCommentLine", WhenClauses.WritableEditorTextFocus, KeybindingWeight.EditorContrib, new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyC)));
        rules.Add(new KeybindingRule(
            new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyK), "editor.action.removeCommentLine", WhenClauses.WritableEditorTextFocus, KeybindingWeight.EditorContrib, new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyU)));
        Add(KeyModifiers.Shift | KeyModifiers.Alt, KeyCode.KeyA, "editor.action.blockComment", WhenClauses.WritableEditorTextFocus, KeybindingWeight.EditorContrib);

        // linesOperations.ts (Contrib): copy and move lines (the Linux override of the copy keys does not apply) and trim trailing whitespace (Ctrl+K Ctrl+X).
        Add(KeyModifiers.Shift | KeyModifiers.Alt, KeyCode.UpArrow, "editor.action.copyLinesUpAction", WhenClauses.WritableEditorTextFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Shift | KeyModifiers.Alt, KeyCode.DownArrow, "editor.action.copyLinesDownAction", WhenClauses.WritableEditorTextFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Alt, KeyCode.UpArrow, "editor.action.moveLinesUpAction", WhenClauses.WritableEditorTextFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Alt, KeyCode.DownArrow, "editor.action.moveLinesDownAction", WhenClauses.WritableEditorTextFocus, KeybindingWeight.EditorContrib);
        rules.Add(new KeybindingRule(
            new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyK), "editor.action.trimTrailingWhitespace", WhenClauses.WritableEditorTextFocus, KeybindingWeight.EditorContrib, new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyX)));

        // linesOperations.ts (Contrib): delete, indent, outdent and insert lines.
        Add(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.KeyK, "editor.action.deleteLines", WhenClauses.WritableTextInput, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl, KeyCode.BracketRight, "editor.action.indentLines", WhenClauses.WritableEditorTextFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl, KeyCode.BracketLeft, "editor.action.outdentLines", WhenClauses.WritableEditorTextFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.Enter, "editor.action.insertLineBefore", WhenClauses.WritableEditorTextFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl, KeyCode.Enter, "editor.action.insertLineAfter", WhenClauses.WritableEditorTextFocus, KeybindingWeight.EditorContrib);

        // toggleTabFocusMode.ts: no kbExpr
        Add(KeyModifiers.Ctrl, KeyCode.KeyM, TabFocusCommands.ToggleTabFocusMode, null, KeybindingWeight.EditorContrib);

        // clipboard.ts (Contrib): no when. Windows overrides cut and copy with a secondary key; paste has Shift+Insert only on Linux.
        Add(KeyModifiers.Ctrl, KeyCode.KeyX, ClipboardCut, null, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Shift, KeyCode.Delete, ClipboardCut, null, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl, KeyCode.KeyC, ClipboardCopy, null, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl, KeyCode.Insert, ClipboardCopy, null, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl, KeyCode.KeyV, ClipboardPaste, null, KeybindingWeight.EditorContrib);

        // findController.ts (Contrib): Ctrl+F and Ctrl+H have no when (the control only gets keys when it or its widget has the focus).
        // The commands of the widget are 5 heavier (Monaco: EditorContrib + 5).
        const int FindWidgetWeight = KeybindingWeight.EditorContrib + 5;
        Add(KeyModifiers.Ctrl, KeyCode.KeyF, "actions.find", null, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.None, KeyCode.F3, "editor.action.nextMatchFindAction", WhenClauses.EditorFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.None, KeyCode.Enter, "editor.action.nextMatchFindAction", WhenClauses.FindInputFocused, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Shift, KeyCode.F3, "editor.action.previousMatchFindAction", WhenClauses.EditorFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Shift, KeyCode.Enter, "editor.action.previousMatchFindAction", WhenClauses.FindInputFocused, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl, KeyCode.F3, "editor.action.nextSelectionMatchFindAction", WhenClauses.EditorFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.F3, "editor.action.previousSelectionMatchFindAction", WhenClauses.EditorFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl, KeyCode.KeyH, "editor.action.startFindReplaceAction", null, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.None, KeyCode.Escape, "closeFindWidget", WhenClauses.FindWidgetVisible, FindWidgetWeight);
        Add(KeyModifiers.Shift, KeyCode.Escape, "closeFindWidget", WhenClauses.FindWidgetVisible, FindWidgetWeight);
        Add(KeyModifiers.Alt, KeyCode.KeyC, "toggleFindCaseSensitive", WhenClauses.EditorFocus, FindWidgetWeight);
        Add(KeyModifiers.Alt, KeyCode.KeyW, "toggleFindWholeWord", WhenClauses.EditorFocus, FindWidgetWeight);
        Add(KeyModifiers.Alt, KeyCode.KeyR, "toggleFindRegex", WhenClauses.EditorFocus, FindWidgetWeight);
        Add(KeyModifiers.Alt, KeyCode.KeyL, "toggleFindInSelection", WhenClauses.EditorFocus, FindWidgetWeight);
        Add(KeyModifiers.Alt, KeyCode.KeyP, "togglePreserveCase", WhenClauses.EditorFocus, FindWidgetWeight);
        Add(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.Digit1, "editor.action.replaceOne", WhenClauses.FindWidgetVisible, FindWidgetWeight);
        Add(KeyModifiers.None, KeyCode.Enter, "editor.action.replaceOne", WhenClauses.ReplaceInputFocused, FindWidgetWeight);
        Add(KeyModifiers.Ctrl | KeyModifiers.Alt, KeyCode.Enter, "editor.action.replaceAll", WhenClauses.FindWidgetVisible, FindWidgetWeight);
        Add(KeyModifiers.Alt, KeyCode.Enter, "editor.action.selectAllMatches", WhenClauses.FindWidgetVisible, FindWidgetWeight);

        // folding.ts (Contrib): unfold and fold have a Mac override that does not apply; the others are Ctrl+K chords. editor.toggleImportFold is not ported.
        Add(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.BracketRight, FoldingCommands.Unfold, WhenClauses.EditorTextFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.BracketLeft, FoldingCommands.Fold, WhenClauses.EditorTextFocus, KeybindingWeight.EditorContrib);

        void FoldChord(KeyModifiers modifiers, KeyCode key, string id) =>
            rules.Add(new KeybindingRule(
                new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyK), id, WhenClauses.EditorTextFocus, KeybindingWeight.EditorContrib, new Keybinding(modifiers, key)));

        FoldChord(KeyModifiers.Ctrl, KeyCode.BracketRight, FoldingCommands.UnfoldRecursively);
        FoldChord(KeyModifiers.Ctrl, KeyCode.KeyL, FoldingCommands.ToggleFold);
        FoldChord(KeyModifiers.Ctrl, KeyCode.BracketLeft, FoldingCommands.FoldRecursively);
        FoldChord(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.KeyL, FoldingCommands.ToggleFoldRecursively);
        FoldChord(KeyModifiers.Ctrl, KeyCode.Slash, FoldingCommands.FoldAllBlockComments);
        FoldChord(KeyModifiers.Ctrl, KeyCode.Digit8, FoldingCommands.FoldAllMarkerRegions);
        FoldChord(KeyModifiers.Ctrl, KeyCode.Digit9, FoldingCommands.UnfoldAllMarkerRegions);
        FoldChord(KeyModifiers.Ctrl, KeyCode.Minus, FoldingCommands.FoldAllExcept);
        FoldChord(KeyModifiers.Ctrl, KeyCode.Equal, FoldingCommands.UnfoldAllExcept);
        FoldChord(KeyModifiers.Ctrl, KeyCode.Digit0, FoldingCommands.FoldAll);
        FoldChord(KeyModifiers.Ctrl, KeyCode.KeyJ, FoldingCommands.UnfoldAll);
        FoldChord(KeyModifiers.Ctrl, KeyCode.Comma, FoldingCommands.CreateFoldingRangeFromSelection);
        FoldChord(KeyModifiers.Ctrl, KeyCode.Period, FoldingCommands.RemoveManualFoldingRanges);
        for (int level = 1; level <= 7; level++)
        {
            FoldChord(KeyModifiers.Ctrl, KeyCode.Digit0 + level, FoldingCommands.FoldLevel(level));
        }

        // hoverActions.ts (Contrib): Ctrl+K Ctrl+I shows the hover; Escape hides it, above cancelSelection.
        FoldChord(KeyModifiers.Ctrl, KeyCode.KeyI, HoverCommands.ShowHover);
        Add(KeyModifiers.None, KeyCode.Escape, HoverCommands.HideHover, WhenClauses.HoverVisible, FindWidgetWeight);

        // suggestController.ts (Contrib, +90): acceptSuggestionOnEnter is always on and a suggestion always makes a text edit in phase 8 (no smart mode),
        // so Enter has the same condition as Tab. selectFirstSuggestion and selectLastSuggestion have no key; Shift+Tab and Shift+Enter are not bound.
        const int SuggestWeight = KeybindingWeight.EditorContrib + 90;
        Add(KeyModifiers.None, KeyCode.Tab, SuggestCommands.AcceptSelectedSuggestion, WhenClauses.SuggestVisible, SuggestWeight);
        Add(KeyModifiers.None, KeyCode.Enter, SuggestCommands.AcceptSelectedSuggestion, WhenClauses.SuggestVisible, SuggestWeight);
        Add(KeyModifiers.None, KeyCode.Escape, SuggestCommands.HideSuggestWidget, WhenClauses.SuggestVisible, SuggestWeight);
        Add(KeyModifiers.Shift, KeyCode.Escape, SuggestCommands.HideSuggestWidget, WhenClauses.SuggestVisible, SuggestWeight);

        // snippetController2.ts (Contrib, +30): Tab and Shift+Tab move between the tab stops, Escape leaves snippet mode. The suggest rules (+90) win
        // on Tab and Escape. Not ported: the Shift+Tab rule of snippet choices (+91).
        const int SnippetWeight = KeybindingWeight.EditorContrib + 30;
        Add(KeyModifiers.None, KeyCode.Tab, SnippetCommands.JumpToNextSnippetPlaceholder, WhenClauses.SnippetNext, SnippetWeight);
        Add(KeyModifiers.Shift, KeyCode.Tab, SnippetCommands.JumpToPrevSnippetPlaceholder, WhenClauses.SnippetPrev, SnippetWeight);
        Add(KeyModifiers.None, KeyCode.Escape, SnippetCommands.LeaveSnippet, WhenClauses.InSnippet, SnippetWeight);
        Add(KeyModifiers.Shift, KeyCode.Escape, SnippetCommands.LeaveSnippet, WhenClauses.InSnippet, SnippetWeight);

        // gotoError.ts: F8 / Shift+F8 (in files) and Alt+F8 / Shift+Alt+F8 behave the same with one model; Escape ends the session (+50, above the snippet Escape).
        Add(KeyModifiers.None, KeyCode.F8, GotoErrorCommands.NextInFiles, WhenClauses.EditorFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Shift, KeyCode.F8, GotoErrorCommands.PrevInFiles, WhenClauses.EditorFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Alt, KeyCode.F8, GotoErrorCommands.Next, WhenClauses.EditorFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Shift | KeyModifiers.Alt, KeyCode.F8, GotoErrorCommands.Prev, WhenClauses.EditorFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.None, KeyCode.Escape, GotoErrorCommands.Close, WhenClauses.MarkersNavigationVisible, KeybindingWeight.EditorContrib + 50);
        Add(KeyModifiers.Shift, KeyCode.Escape, GotoErrorCommands.Close, WhenClauses.MarkersNavigationVisible, KeybindingWeight.EditorContrib + 50);

        // anchorSelect.ts and lineSelection.ts: Ctrl+K Ctrl+B, Ctrl+K Ctrl+K and Ctrl+L. Escape has the lowest weight of the Escape rules (Monaco: EditorContrib) so the widgets close first.
        rules.Add(new KeybindingRule(
            new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyK), SelectionAnchorCommands.SetSelectionAnchor, WhenClauses.EditorTextFocus, KeybindingWeight.EditorContrib, new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyB)));
        rules.Add(new KeybindingRule(
            new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyK), SelectionAnchorCommands.SelectFromAnchorToCursor, WhenClauses.SelectionAnchorSet, KeybindingWeight.EditorContrib, new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyK)));
        Add(KeyModifiers.None, KeyCode.Escape, SelectionAnchorCommands.CancelSelectionAnchor, WhenClauses.SelectionAnchorSet, KeybindingWeight.EditorCore);
        Add(KeyModifiers.Ctrl, KeyCode.KeyL, LineSelectionCommands.ExpandLineSelection, WhenClauses.TextInputFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl, KeyCode.KeyU, CursorUndoCommands.CursorUndo, WhenClauses.TextInputFocus, KeybindingWeight.EditorContrib);
        // smartSelect.ts: Shift+Alt+Right / Left (the Windows binding; the macOS ones are not ported).
        Add(KeyModifiers.Shift | KeyModifiers.Alt, KeyCode.RightArrow, SmartSelectCommands.Expand, WhenClauses.EditorTextFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Shift | KeyModifiers.Alt, KeyCode.LeftArrow, SmartSelectCommands.Shrink, WhenClauses.EditorTextFocus, KeybindingWeight.EditorContrib);

        void Navigate(KeyCode key, string id)
        {
            Add(KeyModifiers.None, key, id, WhenClauses.SuggestNavigation, SuggestWeight);
            Add(KeyModifiers.Ctrl, key, id, WhenClauses.SuggestNavigation, SuggestWeight);
        }

        Navigate(KeyCode.DownArrow, SuggestCommands.SelectNextSuggestion);
        Navigate(KeyCode.UpArrow, SuggestCommands.SelectPrevSuggestion);
        Navigate(KeyCode.PageDown, SuggestCommands.SelectNextPageSuggestion);
        Navigate(KeyCode.PageUp, SuggestCommands.SelectPrevPageSuggestion);
        Add(KeyModifiers.Ctrl, KeyCode.Space, SuggestCommands.TriggerSuggest, WhenClauses.TriggerSuggest, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Ctrl, KeyCode.KeyI, SuggestCommands.TriggerSuggest, WhenClauses.TriggerSuggest, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.None, KeyCode.F12, GotoDefinitionCommands.RevealDefinition, WhenClauses.GotoDefinition, KeybindingWeight.EditorContrib);
        // The workbench owns the Insert key (no Monaco id): it toggles overtype while the editor text has the focus.
        Add(KeyModifiers.None, KeyCode.Insert, OvertypeCommands.ToggleOvertype, WhenClauses.EditorTextFocus, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Alt, KeyCode.F5, DiffNavigationCommands.NextChange, WhenClauses.InDiffEditor, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Shift | KeyModifiers.Alt, KeyCode.F5, DiffNavigationCommands.PreviousChange, WhenClauses.InDiffEditor, KeybindingWeight.EditorContrib);
        // commands.ts (AccessibleDiffViewerNext, AccessibleDiffViewerPrev): after the word highlighter F7 rules, which win only outside a diff editor.
        Add(KeyModifiers.None, KeyCode.F7, DiffNavigationCommands.AccessibleViewerNext, WhenClauses.InDiffEditor, KeybindingWeight.EditorContrib);
        Add(KeyModifiers.Shift, KeyCode.F7, DiffNavigationCommands.AccessibleViewerPrevious, WhenClauses.InDiffEditor, KeybindingWeight.EditorContrib);

        return rules;
    }
}

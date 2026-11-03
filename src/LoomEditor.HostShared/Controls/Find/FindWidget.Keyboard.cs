using LoomEditor.View.Features.Find;
using LoomEditor.View.Features.MultiCursor;

#if LOOM_WINUI
namespace LoomEditor.WinUI.Controls.Find;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls.Find;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif

// Keyboard and focus (Monaco: `findWidget.ts` focus trackers, `focusFindInput`, `_onFindInputKeyDown`): the keys that belong to
// the find commands go through the editor's keybindings, the others stay with the text boxes.
public sealed partial class FindWidget
{
    /// <summary>The commands the widget runs for a key press; undo, redo, select all and the clipboard stay with the text boxes.</summary>
    internal static readonly HashSet<string> RoutedCommandIds = new(StringComparer.Ordinal)
    {
        FindCommands.StartFindAction,
        FindCommands.StartFindWithSelection,
        FindCommands.NextMatchFindAction,
        FindCommands.PreviousMatchFindAction,
        FindCommands.NextSelectionMatchFindAction,
        FindCommands.PreviousSelectionMatchFindAction,
        FindCommands.StartFindReplaceAction,
        FindCommands.CloseFindWidgetCommand,
        FindCommands.ToggleCaseSensitiveCommand,
        FindCommands.ToggleWholeWordCommand,
        FindCommands.ToggleRegexCommand,
        FindCommands.ToggleSearchScopeCommand,
        FindCommands.TogglePreserveCaseCommand,
        FindCommands.ReplaceOneAction,
        FindCommands.ReplaceAllAction,
        FindCommands.SelectAllMatchesAction,
        MultiCursorCommands.AddSelectionToNextFindMatch,
        MultiCursorCommands.MoveSelectionToNextFindMatch,
        MultiCursorCommands.SelectHighlights,
        MultiCursorCommands.ChangeAll,
    };

    private bool _replaceInputLastFocused;

    /// <inheritdoc />
    bool IFindWidget.IsFindInputFocused => IsFindInputFocused;

    /// <inheritdoc />
    bool IFindWidget.WasReplaceInputLastFocused => _replaceInputLastFocused;

    // Monaco: `focusFindInput` and `focusReplaceInput` (findWidget.ts:840): select all, then focus.
    void IFindWidget.ApplyStartFocus(FindStartFocusAction focus)
    {
        if (focus == FindStartFocusAction.NoFocusChange)
        {
            return;
        }

        // A collapsed replace row cannot take the focus.
        var box = focus == FindStartFocusAction.FocusReplaceInput && IsReplaceRowVisible ? ReplaceTextBox : FindTextBox;
        box.SelectAll();
        FocusElement(box);
    }

    // Monaco: `focusLastElement` (findWidget.ts:341).
    void IFindWidget.FocusLastElement()
    {
        var target = _lastFocusedElement is { IsLoaded: true } last
            ? last
            : _replaceInputLastFocused && IsReplaceRowVisible ? ReplaceTextBox : FindTextBox;
        FocusElement(target);
    }
}

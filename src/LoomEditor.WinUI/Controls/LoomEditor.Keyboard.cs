using LoomEditor.View.Commands;
using LoomEditor.View.Controller;
using LoomEditor.View.Features;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace LoomEditor.WinUI.Controls;

// Keyboard: shortcuts go through the keybinding resolver to the commands, typed characters are inserted at every cursor
// (ADR 0006, decision 11: the plain typing path; IME comes with P3-48 and later).
public sealed partial class LoomEditor
{
    private readonly TextInputBuffer _textInput = new();

    // Set by the diff control for its two editors (Monaco: the `isInDiffEditor` context key).
    internal bool IsInDiffEditor { get; set; }

    // The state the `when` conditions of the keybindings read (Monaco: the editor's context keys).
    private KeybindingContext CreateKeybindingContext()
    {
        var selections = _viewModel.GetSelections();
        return new KeybindingContext(
            TextInputFocus: HasFocus,
            EditorTextFocus: HasFocus,
            HasNonEmptySelection: selections.Any(selection => !selection.IsEmpty),
            HasMultipleSelections: selections.Count > 1,
            ColumnSelection: _viewModel.CursorOptions.ColumnSelection,
            EditorReadOnly: IsReadOnly,
            TabDoesNotMoveFocus: !TabMovesFocus,
            AccessibilityModeEnabled: false,
            EditorFocus: HasFocus || (_findWidget?.IsFocusWithin ?? false),
            FindWidgetVisible: _context.FindController?.State.IsRevealed ?? false,
            FindInputFocused: _findWidget?.IsFindInputFocused ?? false,
            ReplaceInputFocused: _findWidget?.IsReplaceInputFocused ?? false,
            HoverVisible: _context.HoverVisible,
            SuggestWidgetVisible: _context.SuggestWidgetVisible,
            SuggestWidgetMultipleSuggestions: _context.SuggestWidgetMultipleSuggestions,
            HasCompletionItemProvider: _context.LanguageFeatures.CompletionProviders.Has(_context.GetLanguageId()),
            HasDefinitionProvider: _context.LanguageFeatures.DefinitionProviders.Has(_context.GetLanguageId()),
            IsInDiffEditor: IsInDiffEditor,
            SelectionAnchorSet: _context.SelectionAnchorSet);
    }

    private void OnCharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs e)
    {
        // The Tab key moves the focus in this mode: its character must not be inserted.
        if (TabMovesFocus && e.Character == '	')
        {
            return;
        }

        if (IsComposing || !HasFocus)
        {
            return;
        }

        string? text = _textInput.Accept(e.Character);
        if (text is null)
        {
            return;
        }

        _viewModel.Type(text, "keyboard");
        e.Handled = true;
    }
}

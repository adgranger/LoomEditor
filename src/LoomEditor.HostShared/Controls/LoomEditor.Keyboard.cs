using LoomEditor.View.Commands;
using LoomEditor.View.Features;
#if !LOOM_AVALONIA
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#endif

public sealed partial class LoomEditor
{
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
            SelectionAnchorSet: _context.SelectionAnchorSet,
            InSnippetMode: _context.InSnippetMode,
            HasNextTabstop: _context.HasNextTabstop,
            HasPrevTabstop: _context.HasPrevTabstop,
            MarkersNavigationVisible: _context.MarkersNavigationVisible);
    }
}

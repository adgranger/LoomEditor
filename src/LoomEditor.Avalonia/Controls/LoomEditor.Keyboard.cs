using Avalonia.Input;
using LoomEditor.Avalonia.Input;
using LoomEditor.View.Commands;
using LoomEditor.View.Controller;
using LoomEditor.View.Features;

namespace LoomEditor.Avalonia.Controls;

// Keyboard: shortcuts go through the keybinding resolver to the commands, typed characters are inserted at every cursor
// (ADR 0006, decision 11: the plain typing path; the IME is in LoomEditor.Ime.cs).
public sealed partial class LoomEditor
{
    private readonly TextInputBuffer _textInput = new();

    // Set by the diff control for its two editors (Monaco: the `isInDiffEditor` context key).
    internal bool IsInDiffEditor { get; set; }

    // The state the `when` conditions of the keybindings read (Monaco: the editor's context keys).
    // The find widget keys follow with P15-17.
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
            EditorFocus: HasFocus,
            FindWidgetVisible: _context.FindController?.State.IsRevealed ?? false,
            FindInputFocused: false,
            ReplaceInputFocused: false,
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

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        // Monaco: `KEY_IN_COMPOSITION` stops the keybindings.
        if (IsComposing)
        {
            return;
        }

        // A key that bubbles up from a widget: the widget already routed what belongs to the editor.
        if (!HasFocus)
        {
            base.OnKeyDown(e);
            return;
        }

        View.Commands.KeyModifiers modifiers = KeyMapping.ToModifiers(e.KeyModifiers, OperatingSystem.IsMacOS());
        _context.UserInputEvents.EmitKeyDown(CreateKeyEventArgs(e.Key, modifiers));

        if (e.Key == Key.Escape && _mouseDown is { IsDragAndDrop: true })
        {
            _mouseDown.OnKeyDownWhileDragging();
            ReleaseCapture();
            e.Handled = true;
            return;
        }

        if (_keyboard.TryHandle(e.Key, modifiers, CreateKeybindingContext()))
        {
            e.Handled = true;
            return;
        }

        // Enter types a line break (WinUI gets it as a carriage return character; Avalonia platforms disagree, so it is typed here and
        // OnTextInput ignores line breaks).
        if (e.Key == Key.Enter && modifiers is View.Commands.KeyModifiers.None or View.Commands.KeyModifiers.Shift)
        {
            _viewModel.Type("\n", "keyboard");
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    /// <inheritdoc />
    protected override void OnKeyUp(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (HasFocus)
        {
            _context.UserInputEvents.EmitKeyUp(CreateKeyEventArgs(e.Key, KeyMapping.ToModifiers(e.KeyModifiers, OperatingSystem.IsMacOS())));
        }

        base.OnKeyUp(e);
    }

    /// <inheritdoc />
    protected override void OnTextInput(TextInputEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnTextInput(e);

        if (e.Text is not { Length: > 0 } characters || !HasFocus)
        {
            return;
        }

        // The result of a composition replaces the preedit text.
        if (_ime.TryCommit(characters))
        {
            e.Handled = true;
            return;
        }

        // The buffer drops control characters (Enter, Tab and the like come through the key bindings) and pairs surrogates.
        var text = new System.Text.StringBuilder();
        foreach (char character in characters)
        {
            if (character is '\r' or '\n')
            {
                continue;
            }

            text.Append(_textInput.Accept(character));
        }

        if (text.Length == 0)
        {
            return;
        }

        _viewModel.Type(text.ToString(), "keyboard");
        e.Handled = true;
    }

    private static EditorKeyEventArgs CreateKeyEventArgs(Key key, View.Commands.KeyModifiers modifiers) =>
        new(modifiers.HasFlag(View.Commands.KeyModifiers.Ctrl), key is Key.LeftCtrl or Key.RightCtrl || (OperatingSystem.IsMacOS() && key is Key.LWin or Key.RWin));
}

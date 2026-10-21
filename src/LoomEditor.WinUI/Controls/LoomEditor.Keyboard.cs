using LoomEditor.View.Commands;
using LoomEditor.View.Controller;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace LoomEditor.WinUI.Controls;

// Keyboard: shortcuts go through the keybinding resolver to the commands, typed characters are inserted at every cursor
// (ADR 0006, decision 11: the plain typing path; IME comes with P3-48 and later).
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="IsReadOnly"/> dependency property.</summary>
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly), typeof(bool), typeof(LoomEditor), new PropertyMetadata(false, OnIsReadOnlyChanged));

    private readonly TextInputBuffer _textInput = new();

    /// <summary>
    /// Whether the text cannot be edited from the keyboard (Monaco: <c>readOnly</c>). The cursor can still move and select.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    private static void OnIsReadOnlyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var editor = (LoomEditor)sender;
        bool readOnly = (bool)e.NewValue;
        editor._viewModel.SetCursorOptions(editor._viewModel.CursorOptions with { ReadOnly = readOnly });
        editor._cursorBlinking.Update(editor.HasFocus, readOnly);
    }

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
            Writable: !IsReadOnly,
            TabDoesNotMoveFocus: true,
            AccessibilityModeEnabled: false,
            EditorFocus: HasFocus,
            FindWidgetVisible: _context.FindController?.State.IsRevealed ?? false);
    }

    private void OnCharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs e)
    {
        if (IsComposing)
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

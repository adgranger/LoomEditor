using LoomEditor.View.Controller;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace LoomEditor.WinUI.Controls;

// Keyboard: shortcuts go through the keybinding resolver to the commands, typed characters are inserted at every cursor
// (ADR 0006, decision 11: the plain typing path; IME comes with P3-48 and later).
public sealed partial class LoomEditor
{
    private readonly TextInputBuffer _textInput = new();

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

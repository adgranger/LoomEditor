using LoomEditor.View.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace LoomEditor.WinUI.Controls.Comments;

internal sealed partial class CommentGutterController
{
    private EditorLayoutInfo EditorLayout => _editor.LayoutInfo;

    private void ConnectButton()
    {
        _button.Click += OnClick;
        _button.PointerEntered += OnButtonPointerEntered;
        _button.PointerExited += OnButtonPointerExited;
    }

    private void DisconnectButton()
    {
        _button.Click -= OnClick;
        _button.PointerEntered -= OnButtonPointerEntered;
        _button.PointerExited -= OnButtonPointerExited;
    }

    private void OnClick(object sender, RoutedEventArgs e) => ButtonClicked();

    private void OnButtonPointerEntered(object sender, PointerRoutedEventArgs e) => ButtonPointerEntered();

    private void OnButtonPointerExited(object sender, PointerRoutedEventArgs e) => ButtonPointerExited();
}

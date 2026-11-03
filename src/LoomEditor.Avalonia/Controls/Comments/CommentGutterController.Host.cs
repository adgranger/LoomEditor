using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Controls.Comments;

internal sealed partial class CommentGutterController
{
    private bool _arrangePending;

    /// <summary>The button, for the tests.</summary>
    internal CommentGutterButton Button => _button;

    private EditorLayoutInfo EditorLayout => _editor.ViewModel.LayoutInfo;

    /// <summary>
    /// Called after each draw: a scroll under a still pointer changes the hovered line and the button's place. The work is posted: Avalonia refuses a layout
    /// change during the render pass.
    /// </summary>
    public void ArrangeAfterDraw()
    {
        if (_arrangePending || (_shownLine == 0 && !_overGutter))
        {
            return;
        }

        _arrangePending = true;
        Dispatcher.UIThread.Post(() =>
        {
            _arrangePending = false;
            Arrange();
        });
    }

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

    private void OnClick(object? sender, RoutedEventArgs e) => ButtonClicked();

    private void OnButtonPointerEntered(object? sender, PointerEventArgs e) => ButtonPointerEntered();

    private void OnButtonPointerExited(object? sender, PointerEventArgs e) => ButtonPointerExited();
}

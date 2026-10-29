using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace LoomEditor.WinUI.Controls.Comments;

// Temporary composer (P11-14): a text box and two buttons so the focus and key routing can be checked. P11-17 replaces this file.
internal sealed partial class CommentThreadWidget
{
    private readonly TextBox _textBox = new() { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 64 };
    private readonly StackPanel _composer = new() { Spacing = 4 };
    private bool _focusOnLoaded;

    /// <summary>Raised when the composer closed from the keyboard or the Cancel button: the editor takes the focus back.</summary>
    public event EventHandler? FocusReturnRequested;

    /// <summary>Moves the focus into the composer's text box, once it is loaded if it is not yet.</summary>
    public void FocusComposer()
    {
        if (_textBox.IsLoaded)
        {
            _textBox.Focus(FocusState.Programmatic);
        }
        else if (!_focusOnLoaded)
        {
            _focusOnLoaded = true;
            _textBox.Loaded += OnTextBoxLoaded;
        }
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private StackPanel CreateComposer()
    {
        var cancel = new Button { Content = "Cancel" };
        var submit = new Button { Content = "Comment" };
        cancel.Click += (_, _) =>
        {
            _viewModel.Cancel();
            FocusReturnRequested?.Invoke(this, EventArgs.Empty);
        };
        submit.Click += (_, _) => _viewModel.Submit();
        _textBox.TextChanged += (_, _) => _viewModel.SetDraft(_textBox.Text);
        // Preview: the text box handles Enter itself (a new line) before KeyDown would reach this handler.
        _textBox.PreviewKeyDown += OnTextBoxPreviewKeyDown;
        _composer.Children.Add(_textBox);
        _composer.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { cancel, submit } });
        return _composer;
    }

    /// <summary>True when the focused element is this widget or one of its descendants.</summary>
    public bool HasFocusWithin()
    {
        var element = XamlRoot is null ? null : FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
        while (element is not null)
        {
            if (ReferenceEquals(element, this))
            {
                return true;
            }

            element = VisualTreeHelper.GetParent(element);
        }

        return false;
    }

    private void UpdateComposer()
    {
        // A composer that closes with the focus inside (submitted, cancelled) gives it back to the editor, not to the window.
        if (!_viewModel.IsComposing && _composer.Visibility == Visibility.Visible && HasFocusWithin())
        {
            FocusReturnRequested?.Invoke(this, EventArgs.Empty);
        }

        _composer.Visibility = _viewModel.IsComposing ? Visibility.Visible : Visibility.Collapsed;
        // The text box writes the draft with '\r' line breaks: compare normalized, or each key press would reset the caret.
        if (Normalize(_textBox.Text) != _viewModel.Draft)
        {
            _textBox.Text = _viewModel.Draft;
        }
    }

    private void OnTextBoxLoaded(object sender, RoutedEventArgs e)
    {
        _textBox.Loaded -= OnTextBoxLoaded;
        _focusOnLoaded = false;
        _textBox.Focus(FocusState.Programmatic);
    }

    // Only these two keys are handled, and only when they did something; everything else stays the text box's.
    private void OnTextBoxPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            if (_viewModel.TryEscape())
            {
                e.Handled = true;
                FocusReturnRequested?.Invoke(this, EventArgs.Empty);
            }
        }
        else if (e.Key == VirtualKey.Enter && IsCtrlDown())
        {
            e.Handled = _viewModel.Submit() is not null;
        }
    }

    private static bool IsCtrlDown() =>
        Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
}

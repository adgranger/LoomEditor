using LoomEditor.View.Comments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace LoomEditor.WinUI.Controls.Comments;

// The composer (visible while the view model composes): a slot for the toolbar (P11-18), the text box or its preview, then Preview, Cancel and
// Comment. The Reply button sits under the comments while the thread is open and not composing.
internal sealed partial class CommentThreadWidget
{
    private readonly StackPanel _composer = new() { Spacing = 6 };
    private readonly StackPanel _toolbarSlot = new() { Orientation = Orientation.Horizontal };
    private readonly TextBox _textBox = new()
    {
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        MinHeight = 96,
        MaxHeight = 280,
        PlaceholderText = CommentStrings.CommentPlaceholder,
        IsSpellCheckEnabled = false,
        IsTextPredictionEnabled = false,
    };

    private readonly ToggleButton _previewButton = new() { Content = CommentStrings.Preview };
    private readonly Button _cancelButton = new() { Content = CommentStrings.Cancel };
    private readonly Button _submitButton = new() { Content = CommentStrings.Comment };
    private readonly Button _replyButton = new() { Content = CommentStrings.Reply, HorizontalAlignment = HorizontalAlignment.Left };
    private bool _focusOnLoaded;

    /// <summary>Raised when the composer closed from the keyboard or the Cancel button: the editor takes the focus back.</summary>
    public event EventHandler? FocusReturnRequested;

    /// <summary>Moves the focus into the composer's text box, once it is loaded and shown if it is not yet.</summary>
    public void FocusComposer()
    {
        if (!_textBox.IsLoaded)
        {
            if (!_focusOnLoaded)
            {
                _focusOnLoaded = true;
                _textBox.Loaded += OnTextBoxLoaded;
            }

            return;
        }

        // A composer that has just been made visible is not laid out yet and refuses the focus: try again once, after the layout.
        if (!_textBox.Focus(FocusState.Programmatic))
        {
            DispatcherQueue.TryEnqueue(() => _textBox.Focus(FocusState.Programmatic));
        }
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

    // WinUI's TextBox uses one '\r' per line break, the view model '\n': one character each, so offsets are unchanged.
    private static string FromTextBox(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static string ToTextBox(string text) => text.Replace('\n', '\r');

    private StackPanel CreateComposer()
    {
        _cancelButton.Click += (_, _) =>
        {
            _viewModel.Cancel();
            FocusReturnRequested?.Invoke(this, EventArgs.Empty);
        };
        _submitButton.Click += (_, _) => _viewModel.Submit();
        if (Application.Current?.Resources.TryGetValue("AccentButtonStyle", out object? style) == true && style is Style accent)
        {
            _submitButton.Style = accent;
        }

        _previewButton.Click += (_, _) => _viewModel.TogglePreview();
        _replyButton.Click += (_, _) =>
        {
            _viewModel.StartComposing();
            FocusComposer();
        };
        _textBox.TextChanged += (_, _) => _viewModel.SetDraft(FromTextBox(_textBox.Text));
        // Preview: the text box handles Enter itself (a new line) before KeyDown would reach this handler.
        _textBox.PreviewKeyDown += OnTextBoxPreviewKeyDown;

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(_previewButton);
        buttons.Children.Add(_cancelButton);
        buttons.Children.Add(_submitButton);
        _composer.Children.Add(_toolbarSlot);
        _composer.Children.Add(_textBox);
        _composer.Children.Add(_preview);
        _composer.Children.Add(buttons);

        var area = new StackPanel { Spacing = 6 };
        area.Children.Add(_replyButton);
        area.Children.Add(_composer);
        return area;
    }

    private void UpdateComposer()
    {
        // A composer that closes with the focus inside (submitted, cancelled) gives it back to the editor, not to the window.
        if (!_viewModel.IsComposing && _composer.Visibility == Visibility.Visible && HasFocusWithin())
        {
            FocusReturnRequested?.Invoke(this, EventArgs.Empty);
        }

        _composer.Visibility = _viewModel.IsComposing ? Visibility.Visible : Visibility.Collapsed;
        _replyButton.Visibility = _viewModel.ReplyVisible ? Visibility.Visible : Visibility.Collapsed;
        _submitButton.IsEnabled = _viewModel.CanSubmit;
        _previewButton.IsChecked = _viewModel.IsPreview;
        // Compare in the view model's form, or each key press would write the text back and reset the caret. Covers the draft cleared on post.
        if (FromTextBox(_textBox.Text) != _viewModel.Draft)
        {
            _textBox.Text = ToTextBox(_viewModel.Draft);
        }

        UpdatePreview();
    }

    private void OnTextBoxLoaded(object sender, RoutedEventArgs e)
    {
        _textBox.Loaded -= OnTextBoxLoaded;
        _focusOnLoaded = false;
        FocusComposer();
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

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using LoomEditor.Avalonia.Input;
using LoomEditor.View.Comments;
using ViewKeyModifiers = LoomEditor.View.Commands.KeyModifiers;

namespace LoomEditor.Avalonia.Controls.Comments;

// The composer (visible while the view model composes): a slot for the toolbar, the text box or its preview, then Preview, Cancel and
// Comment. The Reply button sits under the comments while the thread is open and not composing.
internal sealed partial class CommentThreadWidget
{
    private readonly StackPanel _composer = new() { Spacing = 6 };
    private readonly StackPanel _toolbarSlot = new() { Orientation = Orientation.Horizontal };
    private CommentComposerToolbar _toolbar = null!;

    // One character per line break, like the view model: offsets are unchanged.
    private readonly TextBox _textBox = new()
    {
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        MinHeight = 96,
        MaxHeight = 280,
        PlaceholderText = CommentStrings.CommentPlaceholder,
        NewLine = "\n",
    };

    private readonly ToggleButton _previewButton = new() { Content = CommentStrings.Preview };
    private readonly Button _cancelButton = new() { Content = CommentStrings.Cancel };
    private readonly Button _submitButton = new() { Content = CommentStrings.Comment };
    private readonly Button _replyButton = new() { Content = CommentStrings.Reply, HorizontalAlignment = HorizontalAlignment.Left };
    private bool _focusOnLoaded;

    /// <summary>Raised when the composer closed from the keyboard or the Cancel button: the editor takes the focus back.</summary>
    public event EventHandler? FocusReturnRequested;

    /// <summary>The text box of the composer, for the tests.</summary>
    internal TextBox TextBox => _textBox;

    /// <summary>The toolbar of the composer, for the tests.</summary>
    internal CommentComposerToolbar Toolbar => _toolbar;

    /// <summary>Moves the focus into the text box of the composer, once it is loaded and shown if it is not yet.</summary>
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
        if (!_textBox.Focus())
        {
            Dispatcher.UIThread.Post(() => _textBox.Focus());
        }
    }

    /// <summary>True when the focused element is this widget or one of its descendants.</summary>
    public bool HasFocusWithin() => IsKeyboardFocusWithin;

    // The text box normalizes its own line breaks to NewLine; pasted text may still bring "\r".
    private static string FromTextBox(string? text) => (text ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private StackPanel CreateComposer()
    {
        _toolbar = new CommentComposerToolbar(ApplyToolbarAction);
        _toolbarSlot.Children.Add(_toolbar);
        AutomationProperties.SetName(_textBox, CommentStrings.CommentText);
        _cancelButton.Click += (_, _) =>
        {
            // Escape only cancels an empty draft: only Cancel can discard text.
            bool discarded = !string.IsNullOrWhiteSpace(_viewModel.Draft);
            _viewModel.Cancel();
            if (discarded)
            {
                Announce?.Invoke(CommentStrings.CommentDiscarded);
            }

            FocusReturnRequested?.Invoke(this, EventArgs.Empty);
        };
        _submitButton.Click += (_, _) => _viewModel.Submit();
        _submitButton.Classes.Add("accent");
        _previewButton.Click += (_, _) => _viewModel.TogglePreview();
        _replyButton.Click += (_, _) =>
        {
            _viewModel.StartComposing();
            FocusComposer();
        };
        _textBox.TextChanged += (_, _) => _viewModel.SetDraft(FromTextBox(_textBox.Text));
        // Tunnel: the text box handles Enter itself (a new line) before the bubbling KeyDown would reach a handler.
        _textBox.AddHandler(KeyDownEvent, OnTextBoxPreviewKeyDown, RoutingStrategies.Tunnel);

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
        if (!_viewModel.IsComposing && _composer.IsVisible && HasFocusWithin())
        {
            FocusReturnRequested?.Invoke(this, EventArgs.Empty);
        }

        _composer.IsVisible = _viewModel.IsComposing;
        _replyButton.IsVisible = _viewModel.ReplyVisible;
        _submitButton.IsEnabled = _viewModel.CanSubmit;
        _previewButton.IsChecked = _viewModel.IsPreview;
        _toolbar.SetEnabled(!_viewModel.IsPreview);
        // Compare in the form of the view model, or each key press would write the text back and reset the caret. Covers the draft cleared on post.
        if (FromTextBox(_textBox.Text) != _viewModel.Draft)
        {
            _textBox.Text = _viewModel.Draft;
        }

        UpdatePreview();
    }

    /// <summary>Applies a toolbar action to the text and selection of the text box, then gives the focus back to the box.</summary>
    /// <remarks>Known limit: assigning <see cref="TextBox.Text"/> clears the undo history of the text box.</remarks>
    private void ApplyToolbarAction(MarkdownToolbarAction action)
    {
        string text = FromTextBox(_textBox.Text);
        int start = Math.Min(_textBox.SelectionStart, _textBox.SelectionEnd);
        int end = Math.Max(_textBox.SelectionStart, _textBox.SelectionEnd);
        ToolbarEdit edit = MarkdownToolbar.Apply(action, text, new ToolbarSelection(start, end), _viewModel.SuggestionSeed);
        _textBox.Text = edit.Text;
        _textBox.SelectionStart = edit.Selection.Start;
        _textBox.SelectionEnd = edit.Selection.Start + edit.Selection.Length;
        _textBox.Focus();
    }

    private void OnTextBoxLoaded(object? sender, RoutedEventArgs e)
    {
        _textBox.Loaded -= OnTextBoxLoaded;
        _focusOnLoaded = false;
        FocusComposer();
    }

    // Escape and Ctrl+Enter are handled only when they did something; Ctrl+B / I / K run their toolbar button; everything else stays with the text box.
    private void OnTextBoxPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        bool ctrl = KeyMapping.ToModifiers(e.KeyModifiers, OperatingSystem.IsMacOS()) == ViewKeyModifiers.Ctrl;
        if (e.Key == Key.Escape)
        {
            if (_viewModel.TryEscape())
            {
                e.Handled = true;
                FocusReturnRequested?.Invoke(this, EventArgs.Empty);
            }
        }
        else if (e.Key == Key.Enter && ctrl)
        {
            e.Handled = _viewModel.Submit() is not null;
        }
        else if (ctrl && e.Key switch
        {
            Key.B => MarkdownToolbarAction.Bold,
            Key.I => MarkdownToolbarAction.Italic,
            Key.K => MarkdownToolbarAction.Link,
            _ => (MarkdownToolbarAction?)null,
        } is { } action)
        {
            ApplyToolbarAction(action);
            e.Handled = true;
        }
    }

    /// <summary>A transparent button with this text color; <see langword="null"/> clears both (the defaults apply).</summary>
    internal static void SetPlain(TemplatedControl button, IBrush? foreground)
    {
        if (foreground is null)
        {
            button.ClearValue(TemplatedControl.BackgroundProperty);
            button.ClearValue(TemplatedControl.ForegroundProperty);
        }
        else
        {
            button.Background = Brushes.Transparent;
            button.Foreground = foreground;
        }
    }
}

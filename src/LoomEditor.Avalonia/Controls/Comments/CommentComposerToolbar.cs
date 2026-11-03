using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using LoomEditor.View.Comments;

namespace LoomEditor.Avalonia.Controls.Comments;

/// <summary>
/// The Markdown buttons above the text box of the composer, one per <see cref="MarkdownToolbarAction"/>, in enum order. One tab stop: the arrow keys,
/// <c>Home</c> and <c>End</c> move between the buttons (roving focus).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class CommentComposerToolbar : StackPanel
{
    private readonly List<Button> _buttons = [];

    public CommentComposerToolbar(Action<MarkdownToolbarAction> apply)
    {
        ArgumentNullException.ThrowIfNull(apply);
        Orientation = Orientation.Horizontal;
        foreach (MarkdownToolbarAction action in Enum.GetValues<MarkdownToolbarAction>())
        {
            string name = CommentStrings.ToolbarName(action);
            var button = new Button
            {
                Content = Label(action),
                Width = 28,
                Height = 28,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                IsTabStop = _buttons.Count == 0,
            };
            ToolTip.SetTip(button, name);
            AutomationProperties.SetName(button, name);
            button.Click += (_, _) => apply(action);
            button.GotFocus += (_, _) => MakeTabStop(button);
            _buttons.Add(button);
            Children.Add(button);
        }

        KeyDown += OnKeyDown;
    }

    /// <summary>The buttons, in enum order, for the tests.</summary>
    internal IReadOnlyList<Button> Buttons => _buttons;

    /// <summary>Enables or disables every button (a panel has no <c>IsEnabled</c> of its own).</summary>
    public void SetEnabled(bool enabled)
    {
        foreach (Button button in _buttons)
        {
            button.IsEnabled = enabled;
        }
    }

    /// <summary>Transparent buttons with this label color; <see langword="null"/> (high contrast) leaves the defaults.</summary>
    public void SetForeground(IBrush? foreground)
    {
        foreach (Button button in _buttons)
        {
            CommentThreadWidget.SetPlain(button, foreground);
        }
    }

    private static string Label(MarkdownToolbarAction action) => action switch
    {
        MarkdownToolbarAction.Heading => "H",
        MarkdownToolbarAction.Bold => "B",
        MarkdownToolbarAction.Italic => "I",
        MarkdownToolbarAction.Underline => "U",
        MarkdownToolbarAction.Strikethrough => "S",
        MarkdownToolbarAction.InlineCode => "<>",
        MarkdownToolbarAction.CodeBlock => "{ }",
        MarkdownToolbarAction.Link => "Link",
        MarkdownToolbarAction.BulletList => "•",
        MarkdownToolbarAction.NumberedList => "1.",
        MarkdownToolbarAction.Quote => "\"",
        MarkdownToolbarAction.Suggestion => "±",
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    // The focused button becomes the only tab stop, so Shift+Tab back into the toolbar lands on it.
    private void MakeTabStop(Button focused)
    {
        foreach (Button button in _buttons)
        {
            button.IsTabStop = ReferenceEquals(button, focused);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        int current = _buttons.FindIndex(b => b.IsFocused);
        if (current < 0)
        {
            return;
        }

        int last = _buttons.Count - 1;
        int? target = e.Key switch
        {
            Key.Left => current == 0 ? last : current - 1,
            Key.Right => current == last ? 0 : current + 1,
            Key.Home => 0,
            Key.End => last,
            _ => null,
        };
        if (target is { } index)
        {
            MakeTabStop(_buttons[index]);
            _buttons[index].Focus(NavigationMethod.Directional);
            e.Handled = true;
        }
    }
}

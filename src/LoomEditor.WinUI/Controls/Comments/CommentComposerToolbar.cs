using LoomEditor.View.Comments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace LoomEditor.WinUI.Controls.Comments;

/// <summary>
/// The Markdown buttons above the composer's text box, one per <see cref="MarkdownToolbarAction"/>, in enum order. One tab stop: the arrow keys,
/// <c>Home</c> and <c>End</c> move between the buttons (roving focus).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class CommentComposerToolbar : StackPanel
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
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                IsTabStop = _buttons.Count == 0,
            };
            ToolTipService.SetToolTip(button, name);
            AutomationProperties.SetName(button, name);
            button.Click += (_, _) => apply(action);
            button.GotFocus += (_, _) => MakeTabStop(button);
            _buttons.Add(button);
            Children.Add(button);
        }

        KeyDown += OnKeyDown;
    }

    /// <summary>Enables or disables every button (a panel has no <c>IsEnabled</c> of its own).</summary>
    public void SetEnabled(bool enabled)
    {
        foreach (Button button in _buttons)
        {
            button.IsEnabled = enabled;
        }
    }

    /// <summary>Sets the text color of the labels.</summary>
    public void SetForeground(Brush foreground)
    {
        foreach (Button button in _buttons)
        {
            button.Foreground = foreground;
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

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        int current = _buttons.FindIndex(b => b.FocusState != FocusState.Unfocused);
        if (current < 0)
        {
            return;
        }

        int last = _buttons.Count - 1;
        int? target = e.Key switch
        {
            VirtualKey.Left => current == 0 ? last : current - 1,
            VirtualKey.Right => current == last ? 0 : current + 1,
            VirtualKey.Home => 0,
            VirtualKey.End => last,
            _ => null,
        };
        if (target is { } index)
        {
            MakeTabStop(_buttons[index]);
            _buttons[index].Focus(FocusState.Keyboard);
            e.Handled = true;
        }
    }
}

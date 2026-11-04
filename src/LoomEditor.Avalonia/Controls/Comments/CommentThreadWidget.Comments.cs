using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using LoomEditor.Core.Comments;
using LoomEditor.View.Comments;
using LoomEditor.View.Markdown;

namespace LoomEditor.Avalonia.Controls.Comments;

// One row per comment: the avatar, then the author, the relative time and the body as rendered Markdown. A 1 px line between comments.
internal sealed partial class CommentThreadWidget
{
    private void AddCommentRows()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (Comment comment in _viewModel.Thread.Comments)
        {
            if (_rows.Children.Count > 1)
            {
                _rows.Children.Add(new Rectangle { Height = 1, Fill = _borderBrush, Margin = new Thickness(0, 2, 0, 2) });
            }

            _rows.Children.Add(CommentRow(comment, now));
        }
    }

    private Grid CommentRow(Comment comment, DateTimeOffset now)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 8 };

        Border avatar = CommentAvatar.Create(comment.Author.Name, _fontSize);
        grid.Children.Add(avatar);

        var content = new StackPanel { Spacing = 2 };
        Grid.SetColumn(content, 1);
        grid.Children.Add(content);

        var line = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        TextBlock author = Text(comment.Author.Name, _foreground);
        author.FontWeight = FontWeight.SemiBold;
        line.Children.Add(author);
        string time = RelativeTime.Format(comment.UpdatedAt ?? comment.CreatedAt, now);
        line.Children.Add(Text(comment.UpdatedAt is null ? time : time + CommentStrings.Edited, _dim));
        content.Children.Add(line);
        // The row is a named group for UI Automation, as the CommentRowPanel of the WinUI twin.
        AutomationProperties.SetControlTypeOverride(grid, AutomationControlType.Group);
        AutomationProperties.SetName(grid, CommentStrings.CommentAutomationName(comment.Author.Name, time));

        if (comment.Body.Length > 0)
        {
            var body = new StackPanel();
            MarkdownDocument document = MarkdownParser.Parse(comment.Body, MarkdownParserOptions.Extended);
            AutomationProperties.SetHelpText(grid, document.ToPlainText());
            foreach (Control row in _markdown.Build(document, _fontSize, _codeFont, SuggestionOptions(document, comment.Id)))
            {
                body.Children.Add(row);
            }

            content.Children.Add(body);
        }

        return grid;
    }
}

using LoomEditor.Core.Comments;
using LoomEditor.View.Comments;
using LoomEditor.View.Markdown;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;

namespace LoomEditor.WinUI.Controls.Comments;

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
        var grid = new Grid { ColumnSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Border avatar = CommentAvatar.Create(comment.Author.Name, _fontSize);
        grid.Children.Add(avatar);

        var content = new StackPanel { Spacing = 2 };
        Grid.SetColumn(content, 1);
        grid.Children.Add(content);

        var line = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        TextBlock author = Text(comment.Author.Name, _foreground);
        author.FontWeight = FontWeights.SemiBold;
        line.Children.Add(author);
        string time = RelativeTime.Format(comment.UpdatedAt ?? comment.CreatedAt, now);
        line.Children.Add(Text(comment.UpdatedAt is null ? time : time + CommentStrings.Edited, _dim));
        content.Children.Add(line);

        if (comment.Body.Length > 0)
        {
            var body = new StackPanel();
            foreach (UIElement row in _markdown.Build(MarkdownParser.Parse(comment.Body, MarkdownParserOptions.Extended), _fontSize, _codeFont))
            {
                if (row is RichTextBlock text)
                {
                    text.IsTextSelectionEnabled = true;
                }

                body.Children.Add(row);
            }

            content.Children.Add(body);
        }

        return grid;
    }
}

using LoomEditor.Core.Comments;
using LoomEditor.View.Comments;
using LoomEditor.View.Markdown;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Comments;

// The header: the title, and for a resolved thread a badge, a ▸ / ▾ glyph and (collapsed) the first line of the first comment and the count.
internal sealed partial class CommentThreadWidget
{
    private UIElement Header()
    {
        bool resolved = _viewModel.Thread.State == CommentThreadState.Resolved;
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        if (resolved)
        {
            row.Children.Add(Text(_viewModel.IsCollapsed ? "▸" : "▾", _foreground));
        }

        TextBlock title = Text(_viewModel.Title, _foreground);
        title.FontWeight = FontWeights.Bold;
        row.Children.Add(title);
        if (!resolved)
        {
            return row;
        }

        row.Children.Add(new Border
        {
            BorderThickness = new Thickness(1),
            BorderBrush = _dim,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(6, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = CommentStrings.Resolved, Foreground = _dim, FontSize = Math.Max(9, _fontSize - 2) },
        });
        if (_viewModel.IsCollapsed)
        {
            TextBlock summary = Text(CollapsedSummary(), _dim);
            summary.TextTrimming = TextTrimming.CharacterEllipsis;
            summary.TextWrapping = TextWrapping.NoWrap;
            row.Children.Add(summary);
        }

        // Only a resolved thread's header is interactive: it expands and collapses the thread.
        var toggle = new Button
        {
            Content = row,
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
        };
        toggle.Click += (_, _) => _viewModel.ToggleCollapsed();
        return toggle;
    }

    private string CollapsedSummary()
    {
        IReadOnlyList<Comment> comments = _viewModel.Thread.Comments;
        if (comments.Count == 0)
        {
            return "";
        }

        string plain = MarkdownParser.Parse(comments[0].Body, MarkdownParserOptions.Extended).ToPlainText();
        int end = plain.IndexOf('\n', StringComparison.Ordinal);
        string firstLine = end < 0 ? plain : plain[..end];
        return $"{firstLine} · {CommentStrings.CommentCount(comments.Count)}";
    }

    private TextBlock Text(string text, Brush foreground) => new()
    {
        Text = text,
        Foreground = foreground,
        FontSize = _fontSize,
        FontFamily = Markdown.MarkdownElementBuilder.ProseFont,
        TextWrapping = TextWrapping.Wrap,
        VerticalAlignment = VerticalAlignment.Center,
    };
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LoomEditor.Avalonia.Controls.Markdown;
using LoomEditor.Core.Comments;
using LoomEditor.View.Comments;

namespace LoomEditor.Avalonia.Controls.Comments;

// The header: the title, and for a resolved thread a badge, a right or down triangle glyph and (collapsed) the first line of the first comment and the count.
internal sealed partial class CommentThreadWidget
{
    private Control Header()
    {
        bool resolved = _viewModel.Thread.State == CommentThreadState.Resolved;
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        if (resolved)
        {
            row.Children.Add(Text(_viewModel.IsCollapsed ? "▸" : "▾", _foreground));
        }

        TextBlock title = Text(_viewModel.Title, _foreground);
        title.FontWeight = FontWeight.Bold;
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

        // Only the header of a resolved thread is interactive: it expands and collapses the thread.
        var toggle = new Button
        {
            Content = row,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
        };
        toggle.Click += (_, _) => _viewModel.ToggleCollapsed();
        return toggle;
    }

    private TextBlock Text(string text, IBrush foreground) => new()
    {
        Text = text,
        Foreground = foreground,
        FontSize = _fontSize,
        FontFamily = MarkdownElementBuilder.ProseFont,
        TextWrapping = TextWrapping.Wrap,
        VerticalAlignment = VerticalAlignment.Center,
    };
}

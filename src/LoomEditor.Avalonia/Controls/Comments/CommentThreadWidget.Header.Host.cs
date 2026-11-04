using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LoomEditor.Avalonia.Controls.Markdown;
using LoomEditor.View.Comments;

namespace LoomEditor.Avalonia.Controls.Comments;

internal sealed partial class CommentThreadWidget
{
    private static StackPanel NewHeaderRow() => new() { Orientation = Orientation.Horizontal, Spacing = 6 };

    private TextBlock BoldText(string text, IBrush foreground)
    {
        TextBlock title = Text(text, foreground);
        title.FontWeight = FontWeight.Bold;
        return title;
    }

    private Border ResolvedBadge(double fontSize) => new()
    {
        BorderThickness = new Thickness(1),
        BorderBrush = _dim,
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(6, 0),
        VerticalAlignment = VerticalAlignment.Center,
        Child = new TextBlock { Text = CommentStrings.Resolved, Foreground = _dim, FontSize = fontSize },
    };

    private Button HeaderToggle(StackPanel row)
    {
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

    private TextBlock SummaryText(string text)
    {
        TextBlock summary = Text(text, _dim);
        summary.TextTrimming = TextTrimming.CharacterEllipsis;
        summary.TextWrapping = TextWrapping.NoWrap;
        return summary;
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

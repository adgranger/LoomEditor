using LoomEditor.View.Comments;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Comments;

internal sealed partial class CommentThreadWidget
{
    private static StackPanel NewHeaderRow() => new() { Orientation = Orientation.Horizontal, Spacing = 6 };

    private TextBlock BoldText(string text, Brush foreground)
    {
        TextBlock title = Text(text, foreground);
        title.FontWeight = FontWeights.Bold;
        return title;
    }

    private Border ResolvedBadge(double fontSize) => new()
    {
        BorderThickness = new Thickness(1),
        BorderBrush = _dim,
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(6, 0, 6, 0),
        VerticalAlignment = VerticalAlignment.Center,
        Child = new TextBlock { Text = CommentStrings.Resolved, Foreground = _dim, FontSize = fontSize },
    };

    private Button HeaderToggle(StackPanel row)
    {
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

    private TextBlock SummaryText(string text)
    {
        TextBlock summary = Text(text, _dim);
        summary.TextTrimming = TextTrimming.CharacterEllipsis;
        summary.TextWrapping = TextWrapping.NoWrap;
        return summary;
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

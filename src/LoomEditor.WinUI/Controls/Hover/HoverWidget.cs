using LoomEditor.View.Hover;
using LoomEditor.WinUI.Controls.Markdown;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace LoomEditor.WinUI.Controls.Hover;

/// <summary>The hover popup (Monaco: <c>ContentHoverWidget</c>, look of <c>hover.css</c>). Code only: no XAML file, no bindings.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class HoverWidget : UserControl
{
    private readonly SolidColorBrush _background = new();
    private readonly SolidColorBrush _foreground = new();
    private readonly SolidColorBrush _border = new();
    private readonly SolidColorBrush _link = new();
    private readonly StackPanel _rows = new() { Orientation = Orientation.Vertical };
    private readonly MarkdownElementBuilder _markdown;

    public HoverWidget()
    {
        _markdown = new MarkdownElementBuilder(new MarkdownTheme { Foreground = _foreground, LinkBrush = _link, QuoteBar = _border, Separator = _border });
        IsTabStop = false;
        AllowFocusOnInteraction = false;
        Visibility = Visibility.Collapsed;
        Content = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4, 8, 4),
            Background = _background,
            BorderBrush = _border,
            Child = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MaxWidth = 600,
                MaxHeight = 250,
                Content = _rows,
            },
        };
        PointerEntered += (_, _) => PointerOverChanged?.Invoke(true);
        PointerExited += (_, _) => PointerOverChanged?.Invoke(false);
    }

    /// <summary>Raised with <c>true</c> when the pointer enters the widget and <c>false</c> when it leaves.</summary>
    public event Action<bool>? PointerOverChanged;

    /// <summary>Rebuilds the rows for these parts.</summary>
    public void Update(IReadOnlyList<IHoverPart> parts, FontFamily codeFont, double fontSize)
    {
        ArgumentNullException.ThrowIfNull(parts);
        ArgumentNullException.ThrowIfNull(codeFont);

        _rows.Children.Clear();
        foreach (IHoverPart part in parts)
        {
            switch (part)
            {
                case MarkerHoverPart marker:
                    AddRow(MarkerRow(marker, fontSize));
                    break;
                case MarkdownHoverPart markdown:
                    foreach (var content in markdown.Contents)
                    {
                        AddMarkdownRows(content.Value, codeFont, fontSize);
                    }

                    break;
                case LoadingHoverPart:
                    var paragraph = new Paragraph();
                    paragraph.Inlines.Add(new Run { Text = "Loading…", FontStyle = Windows.UI.Text.FontStyle.Italic });
                    RichTextBlock loading = NewText(fontSize);
                    loading.Blocks.Add(paragraph);
                    loading.Opacity = 0.6;
                    AddRow(loading);
                    break;
            }
        }
    }

    private void AddRow(UIElement row)
    {
        if (_rows.Children.Count > 0)
        {
            _rows.Children.Add(Separator());
        }

        _rows.Children.Add(row);
    }

    private Rectangle Separator() => _markdown.Separator();

    private RichTextBlock NewText(double fontSize) => _markdown.NewText(fontSize);
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using LoomEditor.Avalonia.Controls.Markdown;
using LoomEditor.View.Hover;

namespace LoomEditor.Avalonia.Controls.Hover;

/// <summary>The hover popup (Monaco: <c>ContentHoverWidget</c>, look of <c>hover.css</c>). Code only: no AXAML file, no bindings.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class HoverWidget : UserControl
{
    private readonly SolidColorBrush _background = new();
    private readonly SolidColorBrush _foreground = new();
    private readonly SolidColorBrush _border = new();
    private readonly SolidColorBrush _link = new();
    /// <summary>Raised with <c>true</c> when the pointer enters the widget and <c>false</c> when it leaves.</summary>
    public event Action<bool>? PointerOverChanged;

    private readonly StackPanel _rows = new() { Orientation = Orientation.Vertical };
    private readonly MarkdownElementBuilder _markdown;

    public HoverWidget()
    {
        _markdown = new MarkdownElementBuilder(new MarkdownTheme { Foreground = _foreground, LinkBrush = _link, QuoteBar = _border, Separator = _border });
        Focusable = false;
        IsVisible = false;
        Content = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4),
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

    /// <summary>The rows, for the tests.</summary>
    internal IReadOnlyList<Control> Rows => _rows.Children;

    /// <summary>Rebuilds the rows for these parts.</summary>
    public void Update(IReadOnlyList<IHoverPart> parts, FontFamily codeFont, double fontSize)
    {
        ArgumentNullException.ThrowIfNull(codeFont);
        ArgumentNullException.ThrowIfNull(parts);

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
                    SelectableTextBlock loading = NewText(fontSize);
                    loading.Inlines!.Add(new Run { Text = "Loading…", FontStyle = FontStyle.Italic });
                    loading.Opacity = 0.6;
                    AddRow(loading);
                    break;
            }
        }
    }

    private void AddRow(Control row)
    {
        if (_rows.Children.Count > 0)
        {
            _rows.Children.Add(_markdown.Separator());
        }

        _rows.Children.Add(row);
    }

    private SelectableTextBlock NewText(double fontSize) => _markdown.NewText(fontSize);
}

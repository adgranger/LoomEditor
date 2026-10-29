using LoomEditor.View.Markdown;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace LoomEditor.WinUI.Controls.Markdown;

/// <summary>Options of one <see cref="MarkdownElementBuilder.Build"/> call.</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed record MarkdownBuildOptions
{
    /// <summary>Called for every code block: a returned element replaces the default code paragraph; <see langword="null"/> keeps it.</summary>
    public Func<MarkdownCodeBlock, UIElement?>? CustomCodeBlock { get; init; }
}

/// <summary>
/// Turns a <see cref="MarkdownDocument"/> into XAML rows (Monaco: the Markdown renderer of the hover), one row per block, rules as separator lines.
/// Used by the hover and the comment widget; the caller stacks the rows.
/// </summary>
/// <remarks>Holds no per-call state: one instance can be reused. Threading: UI thread only.</remarks>
internal sealed partial class MarkdownElementBuilder(MarkdownTheme theme)
{
    /// <summary>The font of prose text.</summary>
    public static readonly FontFamily ProseFont = new("Segoe UI Variable, Segoe UI");

    /// <summary>The rows of the document, in order.</summary>
    public IReadOnlyList<UIElement> Build(MarkdownDocument document, double fontSize, FontFamily codeFont, MarkdownBuildOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(codeFont);

        var rows = new List<UIElement>(document.Blocks.Count);
        AddRows(rows, document.Blocks, fontSize, codeFont, options);
        return rows;
    }

    /// <summary>A text element with the prose font, wrapping, not selectable.</summary>
    public RichTextBlock NewText(double fontSize) => new()
    {
        IsTextSelectionEnabled = false,
        TextWrapping = TextWrapping.Wrap,
        FontFamily = ProseFont,
        FontSize = fontSize,
        Foreground = theme.Foreground,
    };

    /// <summary>The line drawn for a horizontal rule (and between hover rows).</summary>
    public Rectangle Separator() => new() { Height = 1, Fill = theme.Separator, Margin = new Thickness(0, 4, 0, 4) };

    private void AddRows(List<UIElement> rows, IReadOnlyList<MarkdownBlock> blocks, double fontSize, FontFamily codeFont, MarkdownBuildOptions? options)
    {
        foreach (MarkdownBlock block in blocks)
        {
            switch (block)
            {
                case MarkdownRule:
                    rows.Add(Separator());
                    continue;
                case MarkdownQuote q:
                    rows.Add(Quote(q, fontSize, codeFont, options));
                    continue;
                case MarkdownCodeBlock c when options?.CustomCodeBlock?.Invoke(c) is { } custom:
                    rows.Add(custom);
                    continue;
            }

            RichTextBlock text = NewText(fontSize);
            switch (block)
            {
                case MarkdownParagraph p:
                    text.Blocks.Add(InlineParagraph(p.Inlines, codeFont, 0));
                    break;
                case MarkdownHeading h:
                    Paragraph heading = InlineParagraph(h.Inlines, codeFont, 0, bold: true);
                    heading.FontSize = fontSize + Math.Clamp(6 - (2 * h.Level), 0, 4);
                    text.Blocks.Add(heading);
                    break;
                case MarkdownCodeBlock c:
                    var code = new Paragraph { FontFamily = codeFont, Margin = new Thickness(0, 4, 0, 4) };
                    code.Inlines.Add(new Run { Text = c.Code });
                    text.Blocks.Add(code);
                    break;
                case MarkdownList l:
                    for (int i = 0; i < l.Items.Count; i++)
                    {
                        Paragraph item = InlineParagraph(l.Items[i], codeFont, 12);
                        item.Inlines.Insert(0, new Run { Text = l.Ordered ? $"{i + 1}. " : "• " });
                        text.Blocks.Add(item);
                    }

                    break;
            }

            rows.Add(text);
        }
    }

    // A 3 px bar on the left, 8 px padding, the rows of the quote dimmed.
    private Border Quote(MarkdownQuote quote, double fontSize, FontFamily codeFont, MarkdownBuildOptions? options)
    {
        var inner = new List<UIElement>(quote.Blocks.Count);
        AddRows(inner, quote.Blocks, fontSize, codeFont, options);
        var panel = new StackPanel { Opacity = 0.8 };
        foreach (UIElement row in inner)
        {
            panel.Children.Add(row);
        }

        return new Border
        {
            BorderBrush = theme.QuoteBar,
            BorderThickness = new Thickness(3, 0, 0, 0),
            Padding = new Thickness(8, 0, 0, 0),
            Child = panel,
        };
    }
}

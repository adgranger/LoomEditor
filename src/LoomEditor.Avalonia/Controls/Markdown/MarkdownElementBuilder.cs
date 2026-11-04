using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using LoomEditor.View.Markdown;

namespace LoomEditor.Avalonia.Controls.Markdown;

/// <summary>Options of one <see cref="MarkdownElementBuilder.Build"/> call.</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed record MarkdownBuildOptions
{
    /// <summary>Called for every code block: a returned control replaces the default code block; <see langword="null"/> keeps it.</summary>
    public Func<MarkdownCodeBlock, Control?>? CustomCodeBlock { get; init; }
}

/// <summary>
/// Turns a <see cref="MarkdownDocument"/> into Avalonia rows (Monaco: the Markdown renderer of the hover), one row per block, rules as separator lines.
/// Used by the hover and the comment widget; the caller stacks the rows. A paragraph or a heading is one <see cref="SelectableTextBlock"/>, a list
/// is a stack of them (one per item), a code block is a <see cref="Border"/> around one.
/// </summary>
/// <remarks>Holds no per-call state: one instance can be reused. Threading: UI thread only.</remarks>
internal sealed partial class MarkdownElementBuilder(MarkdownTheme theme)
{
    /// <summary>The font of prose text.</summary>
    public static readonly FontFamily ProseFont = new("Segoe UI Variable, Segoe UI");

    /// <summary>The rows of the document, in order.</summary>
    public IReadOnlyList<Control> Build(MarkdownDocument document, double fontSize, FontFamily codeFont, MarkdownBuildOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(codeFont);

        var rows = new List<Control>(document.Blocks.Count);
        AddRows(rows, document.Blocks, fontSize, codeFont, options);
        return rows;
    }

    /// <summary>A text control with the prose font and wrapping.</summary>
    public SelectableTextBlock NewText(double fontSize) => new()
    {
        TextWrapping = TextWrapping.Wrap,
        FontFamily = ProseFont,
        FontSize = fontSize,
        Foreground = theme.Foreground,
    };

    /// <summary>The line drawn for a horizontal rule (and between hover rows).</summary>
    public Rectangle Separator() => new() { Height = 1, Fill = theme.Separator, Margin = new Thickness(0, 4, 0, 4) };

    private void AddRows(List<Control> rows, IReadOnlyList<MarkdownBlock> blocks, double fontSize, FontFamily codeFont, MarkdownBuildOptions? options)
    {
        foreach (MarkdownBlock block in blocks)
        {
            switch (block)
            {
                case MarkdownRule:
                    rows.Add(Separator());
                    break;
                case MarkdownQuote q:
                    rows.Add(Quote(q, fontSize, codeFont, options));
                    break;
                case MarkdownCodeBlock c when options?.CustomCodeBlock?.Invoke(c) is { } custom:
                    rows.Add(custom);
                    break;
                case MarkdownCodeBlock c:
                    rows.Add(CodeBlock(c, fontSize, codeFont));
                    break;
                case MarkdownParagraph p:
                    rows.Add(Text(p.Inlines, fontSize, codeFont, 0, bold: false));
                    break;
                case MarkdownHeading h:
                    SelectableTextBlock heading = Text(h.Inlines, fontSize, codeFont, 0, bold: true);
                    heading.FontSize = fontSize + Math.Clamp(6 - (2 * h.Level), 0, 4);
                    rows.Add(heading);
                    break;
                case MarkdownList l:
                    rows.Add(List(l, fontSize, codeFont));
                    break;
            }
        }
    }

    private Border CodeBlock(MarkdownCodeBlock block, double fontSize, FontFamily codeFont)
    {
        SelectableTextBlock text = NewText(fontSize);
        text.FontFamily = codeFont;
        text.Inlines!.Add(new Run { Text = block.Code });
        return new Border { Margin = new Thickness(0, 4, 0, 4), Background = theme.CodeBackground, Child = text };
    }

    private StackPanel List(MarkdownList list, double fontSize, FontFamily codeFont)
    {
        var panel = new StackPanel();
        for (int i = 0; i < list.Items.Count; i++)
        {
            SelectableTextBlock item = Text(list.Items[i], fontSize, codeFont, 12, bold: false);
            item.Inlines!.Insert(0, new Run { Text = list.Ordered ? $"{i + 1}. " : "• " });
            panel.Children.Add(item);
        }

        return panel;
    }

    // A 3 px bar on the left, 8 px padding, the rows of the quote dimmed.
    private Border Quote(MarkdownQuote quote, double fontSize, FontFamily codeFont, MarkdownBuildOptions? options)
    {
        var inner = new List<Control>(quote.Blocks.Count);
        AddRows(inner, quote.Blocks, fontSize, codeFont, options);
        var panel = new StackPanel { Opacity = 0.8 };
        panel.Children.AddRange(inner);

        return new Border
        {
            BorderBrush = theme.QuoteBar,
            BorderThickness = new Thickness(3, 0, 0, 0),
            Padding = new Thickness(8, 0, 0, 0),
            Child = panel,
        };
    }
}

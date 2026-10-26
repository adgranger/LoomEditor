using LoomEditor.View.Hover;
using LoomEditor.View.Markdown;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Hover;

// The rows of a marker and of Markdown: Monaco `_renderMarkerHover` and the Markdown renderer, drawn as RichTextBlocks.
internal sealed partial class HoverWidget
{
    private RichTextBlock MarkerRow(MarkerHoverPart part, double fontSize)
    {
        var marker = part.Marker;
        var paragraph = new Paragraph();
        string[] lines = marker.Message.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                paragraph.Inlines.Add(new LineBreak());
            }

            paragraph.Inlines.Add(new Run { Text = lines[i].TrimEnd('\r') });
        }

        string? detail = (marker.Source, marker.Code) switch
        {
            ({ Length: > 0 } s, { Length: > 0 } c) => $"{s}({c})",
            ({ Length: > 0 } s, _) => s,
            (_, { Length: > 0 } c) => $"({c})",
            _ => null,
        };
        if (detail is not null)
        {
            // Monaco: the `.details` span is dimmed and has a left margin.
            var inline = new InlineUIContainer
            {
                Child = new TextBlock { Text = detail, Opacity = 0.6, Margin = new Thickness(6, 0, 0, 0), FontSize = fontSize, Foreground = _foreground },
            };
            paragraph.Inlines.Add(inline);
        }

        RichTextBlock text = NewText(fontSize);
        text.Blocks.Add(paragraph);
        return text;
    }

    private void AddMarkdownRows(string value, FontFamily codeFont, double fontSize)
    {
        foreach (MarkdownBlock block in MarkdownParser.Parse(value).Blocks)
        {
            if (block is MarkdownRule)
            {
                AddRow(Separator());
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

            AddRow(text);
        }
    }

    private Paragraph InlineParagraph(IReadOnlyList<MarkdownInline> inlines, FontFamily codeFont, double leftMargin, bool bold = false)
    {
        var paragraph = new Paragraph { Margin = new Thickness(leftMargin, 0, 0, 0) };
        foreach (MarkdownInline inline in inlines)
        {
            if (inline.IsLineBreak)
            {
                paragraph.Inlines.Add(new LineBreak());
                continue;
            }

            var run = new Run { Text = inline.Text };
            if (bold)
            {
                run.FontWeight = FontWeights.Bold;
            }
            else if (inline.Bold)
            {
                run.FontWeight = FontWeights.SemiBold;
            }

            if (inline.Italic)
            {
                run.FontStyle = Windows.UI.Text.FontStyle.Italic;
            }

            if (inline.Code)
            {
                run.FontFamily = codeFont;
            }

            if (inline.LinkUrl is not null)
            {
                run.Foreground = _link;
                var underline = new Underline();
                underline.Inlines.Add(run);
                paragraph.Inlines.Add(underline);
            }
            else
            {
                paragraph.Inlines.Add(run);
            }
        }

        return paragraph;
    }
}

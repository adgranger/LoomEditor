using LoomEditor.View.Markdown;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Markdown;

// The inline runs of a paragraph, heading or list item.
internal sealed partial class MarkdownElementBuilder
{
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

            // A link is already underlined by its Underline span.
            Windows.UI.Text.TextDecorations decorations = Windows.UI.Text.TextDecorations.None;
            if (inline.Strikethrough)
            {
                decorations |= Windows.UI.Text.TextDecorations.Strikethrough;
            }

            if (inline.Underline && inline.LinkUrl is null)
            {
                decorations |= Windows.UI.Text.TextDecorations.Underline;
            }

            if (decorations != Windows.UI.Text.TextDecorations.None)
            {
                run.TextDecorations = decorations;
            }

            if (inline.LinkUrl is not null)
            {
                run.Foreground = theme.LinkBrush;
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

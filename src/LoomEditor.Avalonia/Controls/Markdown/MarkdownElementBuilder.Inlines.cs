using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using LoomEditor.View.Markdown;

namespace LoomEditor.Avalonia.Controls.Markdown;

// The inline runs of a paragraph, heading or list item.
internal sealed partial class MarkdownElementBuilder
{
    private SelectableTextBlock Text(IReadOnlyList<MarkdownInline> inlines, double fontSize, FontFamily codeFont, double leftMargin, bool bold)
    {
        SelectableTextBlock text = NewText(fontSize);
        text.Margin = new Thickness(leftMargin, 0, 0, 0);
        foreach (MarkdownInline inline in inlines)
        {
            if (inline.IsLineBreak)
            {
                text.Inlines!.Add(new LineBreak());
                continue;
            }

            var run = new Run { Text = inline.Text };
            if (bold)
            {
                run.FontWeight = FontWeight.Bold;
            }
            else if (inline.Bold)
            {
                run.FontWeight = FontWeight.SemiBold;
            }

            if (inline.Italic)
            {
                run.FontStyle = FontStyle.Italic;
            }

            if (inline.Code)
            {
                run.FontFamily = codeFont;
            }

            // A link is underlined with its own color; other underlines come from the document.
            var decorations = new TextDecorationCollection();
            if (inline.Strikethrough)
            {
                decorations.AddRange(TextDecorations.Strikethrough);
            }

            if (inline.Underline || inline.LinkUrl is not null)
            {
                decorations.AddRange(TextDecorations.Underline);
            }

            if (decorations.Count > 0)
            {
                run.TextDecorations = decorations;
            }

            if (inline.LinkUrl is not null)
            {
                run.Foreground = theme.LinkBrush;
            }

            text.Inlines!.Add(run);
        }

        return text;
    }
}

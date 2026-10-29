using LoomEditor.View.Hover;
using LoomEditor.View.Markdown;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Hover;

// The rows of a marker (Monaco `_renderMarkerHover`) and of Markdown (built by MarkdownElementBuilder), drawn as RichTextBlocks.
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
        foreach (UIElement row in _markdown.Build(MarkdownParser.Parse(value), fontSize, codeFont))
        {
            AddRow(row);
        }
    }
}

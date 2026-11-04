using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using LoomEditor.View.Hover;
using LoomEditor.View.Markdown;

namespace LoomEditor.Avalonia.Controls.Hover;

// The rows of a marker (Monaco `_renderMarkerHover`) and of Markdown (built by MarkdownElementBuilder), drawn as SelectableTextBlocks.
internal sealed partial class HoverWidget
{
    private SelectableTextBlock MarkerRow(MarkerHoverPart part, double fontSize)
    {
        var marker = part.Marker;
        SelectableTextBlock text = NewText(fontSize);
        string[] lines = marker.Message.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                text.Inlines!.Add(new LineBreak());
            }

            text.Inlines!.Add(new Run { Text = lines[i].TrimEnd('\r') });
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
            text.Inlines!.Add(new InlineUIContainer
            {
                Child = new TextBlock { Text = detail, Opacity = 0.6, Margin = new Thickness(6, 0, 0, 0), FontSize = fontSize, Foreground = _foreground },
            });
        }

        return text;
    }

    private void AddMarkdownRows(string value, FontFamily codeFont, double fontSize)
    {
        foreach (Control row in _markdown.Build(MarkdownParser.Parse(value), fontSize, codeFont))
        {
            AddRow(row);
        }
    }
}

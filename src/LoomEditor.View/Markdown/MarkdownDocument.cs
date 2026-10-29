using System.Text;

namespace LoomEditor.View.Markdown;

/// <summary>A parsed Markdown document: a list of blocks a UI can draw without an HTML engine.</summary>
/// <remarks>Threading: immutable, safe to share between threads. Note: list members compare by reference, not by content.</remarks>
/// <param name="Blocks">The blocks, in order.</param>
internal sealed record MarkdownDocument(IReadOnlyList<MarkdownBlock> Blocks)
{
    /// <summary>
    /// The text of every block: paragraphs and headings as their inline texts (line breaks as <c>\n</c>), code blocks as
    /// their code, list items as <c>- text</c> / <c>1. text</c> lines, rules as an empty line; blocks are separated by a blank line.
    /// </summary>
    public string ToPlainText()
    {
        var parts = new List<string>(Blocks.Count);
        foreach (MarkdownBlock block in Blocks)
        {
            parts.Add(block switch
            {
                MarkdownParagraph p => InlinesToText(p.Inlines),
                MarkdownHeading h => InlinesToText(h.Inlines),
                MarkdownCodeBlock c => c.Code,
                MarkdownList l => ListToText(l),
                MarkdownQuote q => new MarkdownDocument(q.Blocks).ToPlainText(),
                _ => string.Empty,
            });
        }

        return string.Join("\n\n", parts);
    }

    private static string ListToText(MarkdownList list)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < list.Items.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('\n');
            }

            sb.Append(list.Ordered ? $"{i + 1}. " : "- ").Append(InlinesToText(list.Items[i]));
        }

        return sb.ToString();
    }

    private static string InlinesToText(IReadOnlyList<MarkdownInline> inlines)
    {
        var sb = new StringBuilder();
        foreach (MarkdownInline inline in inlines)
        {
            sb.Append(inline.IsLineBreak ? "\n" : inline.Text);
        }

        return sb.ToString();
    }
}

/// <summary>A block of a <see cref="MarkdownDocument"/>.</summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
internal abstract record MarkdownBlock;

/// <summary>A paragraph.</summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
/// <param name="Inlines">The inline runs.</param>
internal sealed record MarkdownParagraph(IReadOnlyList<MarkdownInline> Inlines) : MarkdownBlock;

/// <summary>A heading.</summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
/// <param name="Level">1 to 6.</param>
/// <param name="Inlines">The inline runs.</param>
internal sealed record MarkdownHeading(int Level, IReadOnlyList<MarkdownInline> Inlines) : MarkdownBlock;

/// <summary>A fenced code block.</summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
/// <param name="Language">The first word of the info string, or null.</param>
/// <param name="Code">The code, without the trailing newline.</param>
internal sealed record MarkdownCodeBlock(string? Language, string Code) : MarkdownBlock;

/// <summary>A flat list.</summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
/// <param name="Ordered">True when the first item's marker is a number.</param>
/// <param name="Items">The inline runs of each item.</param>
internal sealed record MarkdownList(bool Ordered, IReadOnlyList<IReadOnlyList<MarkdownInline>> Items) : MarkdownBlock;

/// <summary>A blockquote (only produced with <see cref="MarkdownParserOptions.Extended"/>).</summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
/// <param name="Blocks">The quoted blocks.</param>
internal sealed record MarkdownQuote(IReadOnlyList<MarkdownBlock> Blocks) : MarkdownBlock;

/// <summary>A horizontal rule.</summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
internal sealed record MarkdownRule : MarkdownBlock;

/// <summary>A styled run of text. Adjacent inlines with the same flags and url are merged by the parser.</summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
/// <param name="Text">The text; empty for a line break.</param>
/// <param name="Bold">Bold.</param>
/// <param name="Italic">Italic.</param>
/// <param name="Code">Inline code.</param>
/// <param name="LinkUrl">The link target, or null.</param>
/// <param name="IsLineBreak">A hard line break (<see cref="Text"/> is empty).</param>
/// <param name="Strikethrough">Struck through (only with <see cref="MarkdownParserOptions.Extended"/>).</param>
/// <param name="Underline">Underlined (only with <see cref="MarkdownParserOptions.Extended"/>).</param>
internal sealed record MarkdownInline(string Text, bool Bold = false, bool Italic = false, bool Code = false, string? LinkUrl = null, bool IsLineBreak = false, bool Strikethrough = false, bool Underline = false);

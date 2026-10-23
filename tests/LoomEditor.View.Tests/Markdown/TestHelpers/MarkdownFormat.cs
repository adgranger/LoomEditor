using LoomEditor.View.Markdown;

namespace LoomEditor.View.Tests.Markdown.TestHelpers;

/// <summary>Builders and a string formatter so tests compare Markdown structures readably (record lists compare by reference).</summary>
internal static class MarkdownFormat
{
    public static MarkdownInline T(string text) => new(text);

    public static MarkdownInline B(string text) => new(text, Bold: true);

    public static MarkdownInline I(string text) => new(text, Italic: true);

    public static MarkdownInline C(string code) => new(code, Code: true);

    public static MarkdownInline L(string text, string url) => new(text, LinkUrl: url);

    public static MarkdownInline Br() => new(string.Empty, IsLineBreak: true);

    public static MarkdownBlock P(params MarkdownInline[] inlines) => new MarkdownParagraph(inlines);

    public static MarkdownDocument Doc(params MarkdownBlock[] blocks) => new(blocks);

    public static string Format(IEnumerable<MarkdownInline> inlines) => string.Join(" ", inlines.Select(Format));

    public static string Format(MarkdownInline i) =>
        i.IsLineBreak
            ? "<br>"
            : $"{{{(i.Bold ? "b" : "")}{(i.Italic ? "i" : "")}{(i.Code ? "c" : "")}{(i.LinkUrl is null ? "" : "@" + i.LinkUrl)}:{i.Text}}}";

    public static string Format(MarkdownDocument doc) => string.Join("\n", doc.Blocks.Select(Format));

    public static string Format(MarkdownBlock block) => block switch
    {
        MarkdownParagraph p => $"P[{Format(p.Inlines)}]",
        MarkdownHeading h => $"H{h.Level}[{Format(h.Inlines)}]",
        MarkdownCodeBlock c => $"CODE({c.Language ?? "null"})<{c.Code}>",
        MarkdownList l => $"{(l.Ordered ? "OL" : "UL")}[{string.Join(" | ", l.Items.Select(Format))}]",
        MarkdownRule => "HR",
        _ => throw new ArgumentOutOfRangeException(nameof(block)),
    };
}

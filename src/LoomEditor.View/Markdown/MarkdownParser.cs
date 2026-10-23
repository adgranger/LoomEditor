using System.Text.RegularExpressions;

namespace LoomEditor.View.Markdown;

/// <summary>Parses the Markdown subset described in README.md into a <see cref="MarkdownDocument"/> (blocks only; see <see cref="MarkdownInlineParser"/>).</summary>
/// <remarks>Threading: pure and thread-safe.</remarks>
internal static partial class MarkdownParser
{
    /// <summary>Parses <paramref name="markdown"/>; null or empty gives a document with no blocks.</summary>
    public static MarkdownDocument Parse(string? markdown)
    {
        var blocks = new List<MarkdownBlock>();
        if (string.IsNullOrEmpty(markdown))
        {
            return new MarkdownDocument(blocks);
        }

        string[] lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var paragraph = new List<string>();
        List<string>? items = null;
        bool ordered = false;

        void FlushParagraph()
        {
            if (paragraph.Count > 0)
            {
                IReadOnlyList<MarkdownInline> inlines = MarkdownInlineParser.Parse(string.Join("\n", paragraph).TrimEnd(' '));
                if (inlines.Count > 0)
                {
                    blocks.Add(new MarkdownParagraph(inlines));
                }

                paragraph.Clear();
            }
        }

        void FlushList()
        {
            if (items is not null)
            {
                var parsed = new List<IReadOnlyList<MarkdownInline>>(items.Count);
                foreach (string item in items)
                {
                    parsed.Add(MarkdownInlineParser.Parse(item));
                }

                blocks.Add(new MarkdownList(ordered, parsed));
                items = null;
            }
        }

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            Match fence = FenceRegex().Match(line);
            if (fence.Success)
            {
                FlushParagraph();
                FlushList();
                i = ReadFence(lines, i, fence, blocks);
                continue;
            }

            if (line.AsSpan().Trim(" \t").IsEmpty)
            {
                FlushParagraph();
                FlushList();
                continue;
            }

            Match m;
            if (RuleRegex().IsMatch(line))
            {
                FlushParagraph();
                FlushList();
                blocks.Add(new MarkdownRule());
            }
            else if ((m = HeadingRegex().Match(line)).Success)
            {
                FlushParagraph();
                FlushList();
                blocks.Add(new MarkdownHeading(m.Groups[1].Length, MarkdownInlineParser.Parse(m.Groups[2].Value)));
            }
            else if ((m = ListItemRegex().Match(line)).Success)
            {
                FlushParagraph();
                if (items is null)
                {
                    items = [];
                    ordered = char.IsAsciiDigit(m.Groups[1].Value[0]);
                }

                items.Add(m.Groups[2].Value);
            }
            else if (items is not null)
            {
                items[^1] += " " + line.Trim(' ', '\t');
            }
            else
            {
                paragraph.Add(line.TrimStart(' ', '\t'));
            }
        }

        FlushParagraph();
        FlushList();
        return new MarkdownDocument(blocks);
    }

    /// <summary>Reads the code of the fence opened at <paramref name="start"/>; returns the index of its last line.</summary>
    private static int ReadFence(string[] lines, int start, Match open, List<MarkdownBlock> blocks)
    {
        string marker = open.Groups[1].Value;
        string info = open.Groups[2].Value;
        var code = new List<string>();
        int i = start + 1;
        for (; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimStart(' ');
            string closing = trimmed.TrimEnd(' ', '\t');
            if (lines[i].Length - trimmed.Length <= 3 && closing.Length >= marker.Length && closing.All(c => c == marker[0]))
            {
                break;
            }

            code.Add(lines[i]);
        }

        blocks.Add(new MarkdownCodeBlock(info.Length == 0 ? null : info, string.Join("\n", code)));
        return i;
    }

    [GeneratedRegex(@"^ {0,3}(`{3,}|~{3,}) *([^\s`]*)")]
    private static partial Regex FenceRegex();

    [GeneratedRegex(@"^ {0,3}([-*_])( *\1){2,} *$")]
    private static partial Regex RuleRegex();

    [GeneratedRegex(@"^ {0,3}(#{1,6}) +(.+?) *$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^ {0,3}([-*+]|\d{1,9}[.)]) +(.*)$")]
    private static partial Regex ListItemRegex();
}

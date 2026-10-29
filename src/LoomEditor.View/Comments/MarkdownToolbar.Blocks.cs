namespace LoomEditor.View.Comments;

internal static partial class MarkdownToolbar
{
    private const string Fence = "```";

    private static int HeadingPrefixLength(string line)
    {
        int n = 0;
        while (n < line.Length && line[n] == '#')
        {
            n++;
        }

        return n is >= 1 and <= 6 && n < line.Length && line[n] == ' ' ? n + 1 : 0;
    }

    private static int NumberPrefixLength(string line)
    {
        int n = 0;
        while (n < line.Length && char.IsAsciiDigit(line[n]))
        {
            n++;
        }

        return n > 0 && n + 1 < line.Length && line[n] == '.' && line[n + 1] == ' ' ? n + 2 : 0;
    }

    // Length of the list / heading / quote prefix a line already has, for the given block action (0 = none).
    private static int ExistingPrefixLength(MarkdownToolbarAction action, string line) => action switch
    {
        MarkdownToolbarAction.Heading => HeadingPrefixLength(line),
        MarkdownToolbarAction.BulletList => line.StartsWith("- ", StringComparison.Ordinal) ? 2 : 0,
        MarkdownToolbarAction.NumberedList => NumberPrefixLength(line),
        _ => line.StartsWith("> ", StringComparison.Ordinal) ? 2 : 0,
    };

    private static ToolbarEdit ToggleLinePrefix(MarkdownToolbarAction action, string text, ToolbarSelection sel)
    {
        int regionStart = sel.Start == 0 ? 0 : text.LastIndexOf('\n', sel.Start - 1) + 1;
        int endPos = !sel.IsEmpty && sel.End > 0 && text[sel.End - 1] == '\n' ? sel.End - 1 : sel.End;
        int nl = text.IndexOf('\n', endPos);
        int regionEnd = nl < 0 ? text.Length : nl;
        if (regionEnd < regionStart)
        {
            regionEnd = regionStart;
        }

        string[] lines = text[regionStart..regionEnd].Split('\n');
        bool IsBlankLine(string l) => string.IsNullOrWhiteSpace(l);

        // Heading and numbered prefixes vary (level / number), so "has the prefix" is exact only for fixed ones.
        bool HasPrefix(string l) => action switch
        {
            MarkdownToolbarAction.Heading => l.StartsWith("### ", StringComparison.Ordinal),
            _ => ExistingPrefixLength(action, l) > 0,
        };

        bool remove = Array.TrueForAll(lines, l => IsBlankLine(l) || HasPrefix(l));
        if (remove && Array.TrueForAll(lines, IsBlankLine))
        {
            remove = false;
        }

        int number = 0;
        int caretDelta = 0;
        var result = new string[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (IsBlankLine(line))
            {
                result[i] = line;
                continue;
            }

            int old = ExistingPrefixLength(action, line);
            string prefix;
            if (remove)
            {
                prefix = string.Empty;
            }
            else
            {
                number++;
                prefix = action switch
                {
                    MarkdownToolbarAction.Heading => "### ",
                    MarkdownToolbarAction.BulletList => "- ",
                    MarkdownToolbarAction.NumberedList => number + ". ",
                    _ => "> ",
                };

                // A numbered list replaces a bullet prefix too.
                if (action == MarkdownToolbarAction.NumberedList && old == 0 && line.StartsWith("- ", StringComparison.Ordinal))
                {
                    old = 2;
                }
                else if (action != MarkdownToolbarAction.NumberedList && action != MarkdownToolbarAction.Heading && old > 0)
                {
                    prefix = line[..old];
                }
            }

            result[i] = prefix + line[old..];
            if (sel.IsEmpty)
            {
                caretDelta = prefix.Length - old;
            }
        }

        string joined = string.Join('\n', result);
        string newText = string.Concat(text.AsSpan(0, regionStart), joined, text.AsSpan(regionEnd));
        if (!sel.IsEmpty)
        {
            return new ToolbarEdit(newText, new ToolbarSelection(regionStart, regionStart + joined.Length));
        }

        int caret = Math.Max(regionStart, sel.Start + caretDelta);
        return new ToolbarEdit(newText, new ToolbarSelection(caret, caret));
    }

    // Replaces the selection by `block` at a line start; the selection then covers block[innerStart..innerStart + innerLength].
    private static ToolbarEdit InsertBlock(string text, ToolbarSelection sel, string block, int innerStart, int innerLength)
    {
        string before = sel.Start > 0 && text[sel.Start - 1] != '\n' ? "\n" : string.Empty;
        string after = sel.End < text.Length && text[sel.End] != '\n' ? "\n" : string.Empty;
        int inner = sel.Start + before.Length + innerStart;
        return new ToolbarEdit(
            text[..sel.Start] + before + block + after + text[sel.End..],
            new ToolbarSelection(inner, inner + innerLength));
    }

    private static ToolbarEdit ToggleCodeBlock(string text, ToolbarSelection sel)
    {
        string selected = text.Substring(sel.Start, sel.Length);
        if (selected.Length >= 8 && selected.StartsWith(Fence + "\n", StringComparison.Ordinal) && selected.EndsWith("\n" + Fence, StringComparison.Ordinal))
        {
            string inner = selected[4..^4];
            return new ToolbarEdit(
                string.Concat(text.AsSpan(0, sel.Start), inner, text.AsSpan(sel.End)),
                new ToolbarSelection(sel.Start, sel.Start + inner.Length));
        }

        return InsertBlock(text, sel, Fence + "\n" + selected + "\n" + Fence, 4, selected.Length);
    }

    private static ToolbarEdit InsertSuggestion(string text, ToolbarSelection sel, string? seed)
    {
        seed ??= string.Empty;
        if (seed.Contains('\n'))
        {
            throw new ArgumentException("The suggestion seed must be a single line.", nameof(seed));
        }

        string fence = seed.StartsWith(Fence, StringComparison.Ordinal) ? Fence + "`" : Fence;
        string head = fence + "suggestion\n";
        return InsertBlock(text, sel, head + seed + "\n" + fence, head.Length, seed.Length);
    }
}

using System.Text;

namespace LoomEditor.View.Markdown;

/// <summary>Parses the inline part of the Markdown subset (code spans, emphasis, links, escapes, entities).</summary>
/// <remarks>Threading: pure and thread-safe.</remarks>
internal static class MarkdownInlineParser
{
    private const string Escapable = "\\`*_{}[]()#+-.!~>";

    private static readonly (string Name, string Value)[] Entities =
    [
        ("&nbsp;", " "), ("&amp;", "&"), ("&lt;", "<"), ("&gt;", ">"), ("&quot;", "\""),
    ];

    /// <summary>Parses <paramref name="text"/>; a <c>\n</c> in it is a soft break (a space).</summary>
    public static IReadOnlyList<MarkdownInline> Parse(string text)
    {
        var result = new List<MarkdownInline>();
        ParseInto(text, new MarkdownInline(string.Empty), result);
        return result;
    }

    // style carries the flags (and link url) the produced inlines inherit.
    private static void ParseInto(string text, MarkdownInline style, List<MarkdownInline> result)
    {
        var plain = new StringBuilder();

        void Flush()
        {
            if (plain.Length > 0)
            {
                Add(result, style with { Text = plain.ToString() });
                plain.Clear();
            }
        }

        int i = 0;
        int end;
        while (i < text.Length)
        {
            char c = text[i];
            if (c == '\\' && i + 1 < text.Length)
            {
                char next = text[i + 1];
                if (next == '\n')
                {
                    Flush();
                    result.Add(new MarkdownInline(string.Empty, IsLineBreak: true));
                    i += 2;
                }
                else if (Escapable.Contains(next))
                {
                    plain.Append(next);
                    i += 2;
                }
                else
                {
                    plain.Append('\\');
                    i++;
                }
            }
            else if (c == '&' && TryEntity(text, i, out string? entity, out int entityLength))
            {
                plain.Append(entity);
                i += entityLength;
            }
            else if (c == '`')
            {
                int n = RunLength(text, i, '`');
                int close = FindBacktickClose(text, i + n, n);
                if (close < 0)
                {
                    plain.Append('`', n);
                    i += n;
                }
                else
                {
                    Flush();
                    Add(result, style with { Text = CodeContent(text.Substring(i + n, close - i - n)), Code = true });
                    i = close + n;
                }
            }
            else if ((c == '*' || c == '_') && TryEmphasis(text, i, c, style, result, Flush, out end))
            {
                i = end;
            }
            else if (c == '*' || c == '_')
            {
                int n = RunLength(text, i, c);
                plain.Append(c, n);
                i += n;
            }
            else if (c == '[' && TryLink(text, i, style, result, Flush, out end))
            {
                i = end;
            }
            else if (c == ' ' && HardBreakLength(text, i) is > 0 and int breakLength)
            {
                Flush();
                result.Add(new MarkdownInline(string.Empty, IsLineBreak: true));
                i += breakLength;
            }
            else
            {
                plain.Append(c == '\n' ? ' ' : c);
                i++;
            }
        }

        Flush();
    }

    private static void Add(List<MarkdownInline> result, MarkdownInline inline)
    {
        if (inline.Text.Length == 0 && !inline.IsLineBreak)
        {
            return;
        }

        if (result.Count > 0 && result[^1] is { IsLineBreak: false } last && !inline.IsLineBreak
            && last.Bold == inline.Bold && last.Italic == inline.Italic && last.Code == inline.Code && last.LinkUrl == inline.LinkUrl)
        {
            result[^1] = last with { Text = last.Text + inline.Text };
            return;
        }

        result.Add(inline);
    }

    private static int HardBreakLength(string text, int i)
    {
        int j = i;
        while (j < text.Length && text[j] == ' ')
        {
            j++;
        }

        return j - i >= 2 && j < text.Length && text[j] == '\n' ? j - i + 1 : 0;
    }

    private static bool TryEntity(string text, int i, out string? value, out int length)
    {
        foreach ((string name, string replacement) in Entities)
        {
            if (string.CompareOrdinal(text, i, name, 0, name.Length) == 0)
            {
                value = replacement;
                length = name.Length;
                return true;
            }
        }

        value = null;
        length = 0;
        return false;
    }

    private static int RunLength(string text, int i, char c)
    {
        int j = i;
        while (j < text.Length && text[j] == c)
        {
            j++;
        }

        return j - i;
    }

    private static int FindBacktickClose(string text, int from, int n)
    {
        int j = from;
        while (j < text.Length)
        {
            if (text[j] != '`')
            {
                j++;
                continue;
            }

            int run = RunLength(text, j, '`');
            if (run == n)
            {
                return j;
            }

            j += run;
        }

        return -1;
    }

    private static string CodeContent(string raw)
    {
        string content = raw.Replace('\n', ' ');
        if (content.Length >= 2 && content[0] == ' ' && content[^1] == ' ' && content.Trim(' ').Length > 0)
        {
            content = content[1..^1];
        }

        return content;
    }

    private static bool TryEmphasis(string text, int i, char c, MarkdownInline style, List<MarkdownInline> result, Action flush, out int end)
    {
        end = i;
        int k = Math.Min(RunLength(text, i, c), 3);
        int start = i + k;
        if (start >= text.Length || (c == '_' && i > 0 && char.IsLetterOrDigit(text[i - 1])) || (c == '*' && char.IsWhiteSpace(text[start])))
        {
            return false;
        }

        for (int j = start; j < text.Length;)
        {
            if (text[j] == '\\')
            {
                j += 2;
                continue;
            }

            if (text[j] != c)
            {
                j++;
                continue;
            }

            int run = RunLength(text, j, c);
            bool valid = run == k && j > start
                && (c == '*' ? !char.IsWhiteSpace(text[j - 1]) : j + k >= text.Length || !char.IsLetterOrDigit(text[j + k]));
            if (valid)
            {
                flush();
                MarkdownInline inner = style with { Bold = style.Bold || k >= 2, Italic = style.Italic || k != 2 };
                ParseInto(text.Substring(start, j - start), inner, result);
                end = j + k;
                return true;
            }

            j += run;
        }

        return false;
    }

    private static bool TryLink(string text, int i, MarkdownInline style, List<MarkdownInline> result, Action flush, out int end)
    {
        end = i;
        int depth = 0;
        int close = -1;
        for (int j = i; j < text.Length; j++)
        {
            if (text[j] == '\\')
            {
                j++;
            }
            else if (text[j] == '[')
            {
                depth++;
            }
            else if (text[j] == ']' && --depth == 0)
            {
                close = j;
                break;
            }
        }

        if (close < 0 || close + 1 >= text.Length || text[close + 1] != '(')
        {
            return false;
        }

        int p = close + 2;
        int urlEnd;
        string url;
        if (p < text.Length && text[p] == '<')
        {
            int gt = text.IndexOf('>', p);
            if (gt < 0)
            {
                return false;
            }

            url = text.Substring(p + 1, gt - p - 1);
            urlEnd = gt + 1;
        }
        else
        {
            urlEnd = p;
            while (urlEnd < text.Length && text[urlEnd] != ' ' && text[urlEnd] != ')')
            {
                urlEnd++;
            }

            url = text.Substring(p, urlEnd - p);
        }

        while (urlEnd < text.Length && text[urlEnd] == ' ')
        {
            urlEnd++;
        }

        if (urlEnd < text.Length && text[urlEnd] == '"')
        {
            int quote = text.IndexOf('"', urlEnd + 1);
            if (quote < 0)
            {
                return false;
            }

            urlEnd = quote + 1;
            while (urlEnd < text.Length && text[urlEnd] == ' ')
            {
                urlEnd++;
            }
        }

        if (urlEnd >= text.Length || text[urlEnd] != ')')
        {
            return false;
        }

        flush();
        ParseInto(text.Substring(i + 1, close - i - 1), style with { LinkUrl = url }, result);
        end = urlEnd + 1;
        return true;
    }
}

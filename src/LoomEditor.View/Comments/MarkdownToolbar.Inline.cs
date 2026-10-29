namespace LoomEditor.View.Comments;

internal static partial class MarkdownToolbar
{
    private static bool IsBlank(char c) => c is ' ' or '\t';

    private static ToolbarEdit Insert(string text, int at, string inserted, int selStart, int selEnd) =>
        new(string.Concat(text.AsSpan(0, at), inserted, text.AsSpan(at)), new ToolbarSelection(selStart, selEnd));

    private static ToolbarEdit ToggleInline(string text, ToolbarSelection sel, string open, string close)
    {
        if (text.AsSpan(sel.Start, sel.Length).IsWhiteSpace())
        {
            return ToggleEmpty(text, sel.End, open, close);
        }

        // One (start, end) per non-blank line of the selection, without its leading / trailing blanks.
        var segments = new List<(int Start, int End)>();
        int lineStart = sel.Start;
        while (lineStart <= sel.End)
        {
            int nl = text.IndexOf('\n', lineStart, sel.End - lineStart);
            int lineEnd = nl < 0 ? sel.End : nl;
            int s = lineStart;
            int e = lineEnd;
            while (s < e && IsBlank(text[s]))
            {
                s++;
            }

            while (e > s && IsBlank(text[e - 1]))
            {
                e--;
            }

            if (s < e)
            {
                segments.Add((s, e));
            }

            lineStart = lineEnd + 1;
        }

        bool Wrapped(int s, int e) =>
            e - s > open.Length + close.Length && text.AsSpan(s, e - s).StartsWith(open) && text.AsSpan(s, e - s).EndsWith(close);

        if (segments.Count == 1)
        {
            (int s, int e) = segments[0];
            int regionStart = s;
            int regionEnd = e;
            string replacement;
            if (Wrapped(s, e))
            {
                replacement = text.Substring(s + open.Length, e - s - open.Length - close.Length);
            }
            else if (s >= open.Length && text.AsSpan(0, s).EndsWith(open) && text.AsSpan(e).StartsWith(close))
            {
                regionStart = s - open.Length;
                regionEnd = e + close.Length;
                replacement = text[s..e];
            }
            else
            {
                replacement = string.Concat(open, text.AsSpan(s, e - s), close);
            }

            return new ToolbarEdit(
                string.Concat(text.AsSpan(0, regionStart), replacement, text.AsSpan(regionEnd)),
                new ToolbarSelection(regionStart, regionStart + replacement.Length));
        }

        bool unwrapAll = segments.TrueForAll(seg => Wrapped(seg.Start, seg.End));
        string result = text;
        int delta = 0;
        for (int i = segments.Count - 1; i >= 0; i--)
        {
            (int s, int e) = segments[i];
            if (unwrapAll)
            {
                result = string.Concat(result.AsSpan(0, e - close.Length), result.AsSpan(e));
                result = string.Concat(result.AsSpan(0, s), result.AsSpan(s + open.Length));
                delta -= open.Length + close.Length;
            }
            else if (!Wrapped(s, e))
            {
                result = string.Concat(result.AsSpan(0, e), close, result.AsSpan(e));
                result = string.Concat(result.AsSpan(0, s), open, result.AsSpan(s));
                delta += open.Length + close.Length;
            }
        }

        return new ToolbarEdit(result, new ToolbarSelection(segments[0].Start, segments[^1].End + delta));
    }

    private static ToolbarEdit ToggleEmpty(string text, int caret, string open, string close)
    {
        if (caret >= open.Length && text.AsSpan(0, caret).EndsWith(open) && text.AsSpan(caret).StartsWith(close))
        {
            int start = caret - open.Length;
            string removed = string.Concat(text.AsSpan(0, start), text.AsSpan(caret + close.Length));
            return new ToolbarEdit(removed, new ToolbarSelection(start, start));
        }

        int inner = caret + open.Length;
        return Insert(text, caret, open + close, inner, inner);
    }

    private static ToolbarEdit InsertLink(string text, ToolbarSelection sel)
    {
        string selected = text.Substring(sel.Start, sel.Length);
        string region = string.IsNullOrWhiteSpace(selected) ? string.Empty : selected;
        int at = region.Length == 0 ? sel.End : sel.Start;
        string before = text.Substring(0, at);
        string after = text.Substring(sel.End);
        if (region.Length == 0)
        {
            return new ToolbarEdit(before + "[](url)" + after, new ToolbarSelection(at + 1, at + 1));
        }

        if (region.StartsWith("http://", StringComparison.Ordinal) || region.StartsWith("https://", StringComparison.Ordinal))
        {
            return new ToolbarEdit(before + "[](" + region + ")" + after, new ToolbarSelection(at + 1, at + 1));
        }

        int url = at + 1 + region.Length + 2;
        return new ToolbarEdit(before + "[" + region + "](url)" + after, new ToolbarSelection(url, url + 3));
    }
}

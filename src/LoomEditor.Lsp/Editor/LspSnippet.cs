using System.Text;

namespace LoomEditor.Lsp.Editor;

/// <summary>Reduces LSP snippet text to the plain text a client without snippet support inserts.</summary>
/// <remarks>Threading: pure and thread-safe.</remarks>
internal static class LspSnippet
{
    /// <summary>Tab stops and variables vanish, placeholders keep their default, choices keep the first option. Never throws.</summary>
    public static string ToPlainText(string snippet)
    {
        if (string.IsNullOrEmpty(snippet))
        {
            return string.Empty;
        }

        StringBuilder sb = new();
        int i = 0;
        Parse(snippet, ref i, sb, nested: false);
        return sb.ToString();
    }

    // Stops at the end of the text, or (nested) at the '}' that closes the enclosing placeholder (left unconsumed).
    private static void Parse(string s, ref int i, StringBuilder sb, bool nested)
    {
        while (i < s.Length)
        {
            char c = s[i];
            if (nested && c == '}')
            {
                return;
            }

            if (c == '\\' && i + 1 < s.Length && s[i + 1] is '$' or '}' or '\\')
            {
                sb.Append(s[i + 1]);
                i += 2;
            }
            else if (c == '$' && TryParseDollar(s, ref i, sb))
            {
                continue;
            }
            else
            {
                sb.Append(c);
                i++;
            }
        }
    }

    private static bool TryParseDollar(string s, ref int i, StringBuilder sb)
    {
        int p = i + 1;
        if (p >= s.Length)
        {
            return false;
        }

        if (char.IsAsciiDigit(s[p]) || IsNameStart(s[p]))
        {
            p = SkipName(s, p);
            i = p;
            return true;
        }

        if (s[p] != '{')
        {
            return false;
        }

        p++;
        if (p >= s.Length || !(char.IsAsciiDigit(s[p]) || IsNameStart(s[p])))
        {
            return false;
        }

        bool tabStop = char.IsAsciiDigit(s[p]);
        p = SkipName(s, p);
        if (p >= s.Length)
        {
            return false;
        }

        if (s[p] == '}')
        {
            i = p + 1;
            return true;
        }

        if (s[p] == ':')
        {
            int q = p + 1;
            StringBuilder inner = new();
            Parse(s, ref q, inner, nested: true);
            if (q >= s.Length)
            {
                return false;
            }

            sb.Append(inner);
            i = q + 1;
            return true;
        }

        if (s[p] == '|' && tabStop)
        {
            int end = s.IndexOf("|}", p + 1, StringComparison.Ordinal);
            if (end < 0)
            {
                return false;
            }

            string choices = s[(p + 1)..end];
            int comma = choices.IndexOf(',');
            sb.Append(comma < 0 ? choices : choices[..comma]);
            i = end + 2;
            return true;
        }

        return false;
    }

    private static bool IsNameStart(char c) => char.IsAsciiLetter(c) || c == '_';

    private static int SkipName(string s, int p)
    {
        bool digits = char.IsAsciiDigit(s[p]);
        while (p < s.Length && (digits ? char.IsAsciiDigit(s[p]) : char.IsAsciiLetterOrDigit(s[p]) || s[p] == '_'))
        {
            p++;
        }

        return p;
    }
}

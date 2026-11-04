using System.Text.RegularExpressions;

namespace LoomEditor.Architecture.Tests.TestHelpers;

/// <summary>A source file and its normalized tokens.</summary>
internal sealed record TokenizedFile(string Path, string[] Tokens, int[] Lines);

/// <summary>A run of identical tokens in a file of each side (line numbers are 1-based and inclusive).</summary>
internal sealed record CopiedBlock(string LeftPath, int LeftFirst, int LeftLast, string RightPath, int RightFirst, int RightLast)
{
    public string Pair => LeftPath + " <-> " + RightPath;

    public override string ToString() => $"{LeftPath}:{LeftFirst}-{LeftLast} <-> {RightPath}:{RightFirst}-{RightLast}";
}

/// <summary>Token-based copy detector, modeled on the C# copy detection of SonarCloud (CPD).</summary>
internal static partial class CopyDetector
{
    public const int MinimumTokens = 100;

    private const ulong HashBase = 1_000_003UL;

    private static readonly string[] Operators =
        ["??=", "?.", "??", "=>", "==", "!=", "<=", ">=", "&&", "||", "++", "--", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<", "::"];

    [GeneratedRegex(@"^\s*(global\s+)?using\s+(static\s+)?[\w.]+(\s*=\s*[^;]+)?;\s*$")]
    private static partial Regex UsingDirective();

    /// <summary>Splits C# source into tokens: literals are normalized, comments, preprocessor lines and using directives are dropped.</summary>
    public static TokenizedFile Tokenize(string path, string text)
    {
        List<string> tokens = [];
        List<int> lines = [];
        int line = 1;
        int i = 0;
        bool lineStart = true;

        void Add(string token, int tokenLine)
        {
            tokens.Add(token);
            lines.Add(tokenLine);
        }

        while (i < text.Length)
        {
            char c = text[i];
            if (c == '\n')
            {
                line++;
                lineStart = true;
                i++;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (lineStart)
            {
                lineStart = false;
                int end = text.IndexOf('\n', i);
                string rest = end < 0 ? text[i..] : text[i..end];
                if (c == '#' || UsingDirective().IsMatch(rest))
                {
                    i += rest.Length;
                    continue;
                }
            }

            if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
            {
                while (i < text.Length && text[i] != '\n')
                {
                    i++;
                }

                continue;
            }

            if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
            {
                int end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                end = end < 0 ? text.Length : end + 2;
                line += CountNewLines(text, i, end);
                i = end;
                continue;
            }

            int startLine = line;
            int stringEnd = c is '"' or '$' or '@' ? SkipString(text, i) : -1;
            if (stringEnd > i)
            {
                Add("$str", startLine);
                line += CountNewLines(text, i, stringEnd);
                i = stringEnd;
            }
            else if (c == '\'')
            {
                i++;
                while (i < text.Length && text[i] != '\'')
                {
                    i += text[i] == '\\' ? 2 : 1;
                }

                i++;
                Add("$char", startLine);
            }
            else if (char.IsDigit(c))
            {
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_' || (text[i] == '.' && i + 1 < text.Length && char.IsDigit(text[i + 1]))))
                {
                    i++;
                }

                Add("$num", startLine);
            }
            else if (char.IsLetter(c) || c == '_' || (c == '@' && i + 1 < text.Length && (char.IsLetter(text[i + 1]) || text[i + 1] == '_')))
            {
                int start = c == '@' ? i + 1 : i;
                i = start;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                {
                    i++;
                }

                Add(text[start..i], startLine);
            }
            else
            {
                int at = i;
                string op = Array.Find(Operators, o => string.CompareOrdinal(text, at, o, 0, o.Length) == 0) ?? c.ToString();
                i += op.Length;
                Add(op, startLine);
            }
        }

        return new TokenizedFile(path, [.. tokens], [.. lines]);
    }

    /// <summary>Finds every run of at least <see cref="MinimumTokens"/> identical tokens between a left file and a right file.</summary>
    public static List<CopiedBlock> FindBlocks(IReadOnlyList<TokenizedFile> left, IReadOnlyList<TokenizedFile> right)
    {
        Dictionary<string, int> interned = [];
        int[][] leftIds = left.Select(f => Intern(f.Tokens, interned)).ToArray();
        int[][] rightIds = right.Select(f => Intern(f.Tokens, interned)).ToArray();

        Dictionary<ulong, List<(int File, int Offset)>> index = [];
        for (int f = 0; f < rightIds.Length; f++)
        {
            foreach ((ulong hash, int offset) in Windows(rightIds[f]))
            {
                if (!index.TryGetValue(hash, out List<(int File, int Offset)>? hits))
                {
                    index[hash] = hits = [];
                }

                hits.Add((f, offset));
            }
        }

        HashSet<(int, int, int, int)> matches = [];
        for (int f = 0; f < leftIds.Length; f++)
        {
            foreach ((ulong hash, int offset) in Windows(leftIds[f]))
            {
                if (!index.TryGetValue(hash, out List<(int File, int Offset)>? hits))
                {
                    continue;
                }

                foreach ((int rf, int ro) in hits)
                {
                    if (leftIds[f].AsSpan(offset, MinimumTokens).SequenceEqual(rightIds[rf].AsSpan(ro, MinimumTokens)))
                    {
                        matches.Add((f, offset, rf, ro));
                    }
                }
            }
        }

        List<CopiedBlock> blocks = [];
        foreach ((int f, int lo, int rf, int ro) in matches.Where(m => !matches.Contains((m.Item1, m.Item2 - 1, m.Item3, m.Item4 - 1))).Order())
        {
            int length = 1;
            while (matches.Contains((f, lo + length, rf, ro + length)))
            {
                length++;
            }

            int lastToken = length + MinimumTokens - 2;
            blocks.Add(new CopiedBlock(
                left[f].Path, left[f].Lines[lo], left[f].Lines[lo + lastToken],
                right[rf].Path, right[rf].Lines[ro], right[rf].Lines[ro + lastToken]));
        }

        return blocks;
    }

    private static int[] Intern(string[] tokens, Dictionary<string, int> interned)
    {
        int[] ids = new int[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
        {
            if (!interned.TryGetValue(tokens[i], out int id))
            {
                interned[tokens[i]] = id = interned.Count;
            }

            ids[i] = id;
        }

        return ids;
    }

    // Rolling hash (wraps around on overflow) of every window of MinimumTokens ids.
    private static IEnumerable<(ulong Hash, int Offset)> Windows(int[] ids)
    {
        if (ids.Length < MinimumTokens)
        {
            yield break;
        }

        ulong outFactor = 1;
        for (int i = 1; i < MinimumTokens; i++)
        {
            outFactor = unchecked(outFactor * HashBase);
        }

        ulong hash = 0;
        for (int i = 0; i < ids.Length; i++)
        {
            if (i >= MinimumTokens)
            {
                hash = unchecked(hash - (((ulong)ids[i - MinimumTokens] + 1) * outFactor));
            }

            hash = unchecked((hash * HashBase) + (ulong)ids[i] + 1);
            if (i >= MinimumTokens - 1)
            {
                yield return (hash, i - MinimumTokens + 1);
            }
        }
    }

    private static int CountNewLines(string text, int start, int end)
    {
        int count = 0;
        for (int i = start; i < end; i++)
        {
            if (text[i] == '\n')
            {
                count++;
            }
        }

        return count;
    }

    // Returns the end of the string literal that starts at i (any form), or -1 when i does not start one.
    private static int SkipString(string text, int i)
    {
        int p = i;
        bool verbatim = false;
        bool interpolated = false;
        while (p < text.Length && text[p] is '$' or '@')
        {
            verbatim |= text[p] == '@';
            interpolated |= text[p] == '$';
            p++;
        }

        if (p >= text.Length || text[p] != '"')
        {
            return -1;
        }

        int quotes = 0;
        while (p + quotes < text.Length && text[p + quotes] == '"')
        {
            quotes++;
        }

        if (quotes >= 3)
        {
            int close = text.IndexOf(new string('"', quotes), p + quotes, StringComparison.Ordinal);
            return close < 0 ? text.Length : close + quotes;
        }

        p++;
        int depth = 0;
        while (p < text.Length)
        {
            char c = text[p];
            if (depth == 0 && c == '"')
            {
                if (verbatim && p + 1 < text.Length && text[p + 1] == '"')
                {
                    p += 2;
                    continue;
                }

                return p + 1;
            }

            if (interpolated && c == '{')
            {
                if (depth == 0 && p + 1 < text.Length && text[p + 1] == '{')
                {
                    p += 2;
                    continue;
                }

                depth++;
            }
            else if (interpolated && c == '}' && depth > 0)
            {
                depth--;
            }
            else if (depth > 0 && c == '"')
            {
                int nested = SkipString(text, p);
                p = nested > 0 ? nested : p + 1;
                continue;
            }
            else if (!verbatim && depth == 0 && c == '\\')
            {
                p++;
            }

            p++;
        }

        return text.Length;
    }
}

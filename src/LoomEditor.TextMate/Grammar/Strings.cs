// Ported from microsoft/vscode-textmate: src/utils.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;

namespace LoomEditor.TextMate.Grammar;

/// <summary>
/// String helpers of <c>utils.ts</c>: <c>basename</c>, <c>strcmp</c>, <c>strArrCmp</c>, <c>isValidHexColor</c>,
/// <c>escapeRegExpCharacters</c>.
/// </summary>
/// <remarks>
/// <para>
/// Not ported: <c>clone</c> / <c>mergeObjects</c> (raw-grammar cloning is a method of the raw types) and
/// <c>containsRTL</c> (unused by the tokenizer). <c>OrMask&lt;T&gt;</c> is a <c>[Flags]</c> enum in C#.
/// </para>
/// <para>Threading: stateless; safe on any thread.</para>
/// </remarks>
internal static partial class Strings
{
    /// <summary><c>basename</c>: the last segment of a <c>/</c> or <c>\</c> path (a trailing separator is ignored).</summary>
    public static string Basename(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        int index = path.LastIndexOf('/');
        if (index < 0)
        {
            index = path.LastIndexOf('\\');
        }

        if (index < 0)
        {
            return path;
        }

        if (index == path.Length - 1)
        {
            return Basename(path[..^1]);
        }

        return path[(index + 1)..];
    }

    /// <summary><c>strcmp</c>: ordinal comparison (UTF-16 code units, as JavaScript's <c>&lt;</c>), as -1, 0 or 1.</summary>
    public static int Strcmp(string a, string b) => Math.Sign(string.CompareOrdinal(a, b));

    /// <summary><c>strArrCmp</c>: <see langword="null"/> first, then by length, then element by element.</summary>
    public static int StrArrCmp(IReadOnlyList<string>? a, IReadOnlyList<string>? b)
    {
        if (a is null && b is null)
        {
            return 0;
        }

        if (a is null)
        {
            return -1;
        }

        if (b is null)
        {
            return 1;
        }

        int len1 = a.Count;
        int len2 = b.Count;
        if (len1 == len2)
        {
            for (int i = 0; i < len1; i++)
            {
                int res = Strcmp(a[i], b[i]);
                if (res != 0)
                {
                    return res;
                }
            }

            return 0;
        }

        return len1 - len2;
    }

    /// <summary><c>isValidHexColor</c>: <c>#rgb</c>, <c>#rgba</c>, <c>#rrggbb</c> or <c>#rrggbbaa</c>.</summary>
    public static bool IsValidHexColor(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);
        return HexColor().IsMatch(hex);
    }

    /// <summary><c>escapeRegExpCharacters</c>: escapes regular expression characters in a given string.</summary>
    public static string EscapeRegExpCharacters(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return RegExpCharacters().Replace(value, "\\$&");
    }

    // The four regexes of isValidHexColor (/^#[0-9a-f]{n}$/i for n = 6, 8, 3, 4) in one; \z because .NET's $ also
    // matches before a final "\n".
    [GeneratedRegex("^#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{8}|[0-9a-fA-F]{3}|[0-9a-fA-F]{4})\\z", RegexOptions.CultureInvariant)]
    private static partial Regex HexColor();

    [GeneratedRegex(@"[\-\\\{\}\*\+\?\|\^\$\.\,\[\]\(\)\#\s]", RegexOptions.CultureInvariant)]
    private static partial Regex RegExpCharacters();
}

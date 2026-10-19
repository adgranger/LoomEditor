// Ported from microsoft/vscode-textmate: src/utils.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;

namespace LoomEditor.TextMate.Grammar;

/// <summary>
/// String helpers of <c>utils.ts</c>: <c>basename</c>, <c>strcmp</c>, <c>strArrCmp</c>, <c>isValidHexColor</c>,
/// <c>escapeRegExpCharacters</c>, <c>containsRTL</c>.
/// </summary>
/// <remarks>
/// <para>
/// Not ported: <c>clone</c> / <c>mergeObjects</c> (raw-grammar cloning is a method of the raw types).
/// <c>OrMask&lt;T&gt;</c> is a <c>[Flags]</c> enum in C#.
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

    /// <summary>
    /// JavaScript's <c>String.prototype.substring</c>: both ends clamped to [0, length], swapped when start &gt; end
    /// (an unmatched group (-1, -1) gives "").
    /// </summary>
    public static string JsSubstring(string value, int start, int end)
    {
        start = Math.Clamp(start, 0, value.Length);
        end = Math.Clamp(end, 0, value.Length);
        if (start > end)
        {
            (start, end) = (end, start);
        }

        return value[start..end];
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

    /// <summary>
    /// <c>containsRTL</c>: true if <paramref name="value"/> contains any Unicode character classified as "R" or "AL"
    /// (generated upstream using https://github.com/alexdima/unicode-utils/blob/main/rtl-test.js, copied verbatim).
    /// </summary>
    public static bool ContainsRtl(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return RtlRegex().IsMatch(value);
    }

    [GeneratedRegex(@"(?:[\u05BE\u05C0\u05C3\u05C6\u05D0-\u05F4\u0608\u060B\u060D\u061B-\u064A\u066D-\u066F\u0671-\u06D5\u06E5\u06E6\u06EE\u06EF\u06FA-\u0710\u0712-\u072F\u074D-\u07A5\u07B1-\u07EA\u07F4\u07F5\u07FA\u07FE-\u0815\u081A\u0824\u0828\u0830-\u0858\u085E-\u088E\u08A0-\u08C9\u200F\uFB1D\uFB1F-\uFB28\uFB2A-\uFD3D\uFD50-\uFDC7\uFDF0-\uFDFC\uFE70-\uFEFC]|\uD802[\uDC00-\uDD1B\uDD20-\uDE00\uDE10-\uDE35\uDE40-\uDEE4\uDEEB-\uDF35\uDF40-\uDFFF]|\uD803[\uDC00-\uDD23\uDE80-\uDEA9\uDEAD-\uDF45\uDF51-\uDF81\uDF86-\uDFF6]|\uD83A[\uDC00-\uDCCF\uDD00-\uDD43\uDD4B-\uDFFF]|\uD83B[\uDC00-\uDEBB])")]
    private static partial Regex RtlRegex();
}

// Ported from microsoft/vscode: src/vs/base/common/strings.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diagnostics;

namespace LoomEditor.Core.Text;

/// <summary>String helpers: lines, leading whitespace, common prefix/suffix, surrogates and code points.</summary>
/// <remarks>
/// <para>Monaco: the matching functions of <c>base/common/strings.ts</c>. Grapheme clusters are in P3-02.</para>
/// <para>Threading: pure static methods; thread-safe.</para>
/// </remarks>
internal static partial class Strings
{
    private const int Space = 0x0020;
    private const int Tab = 0x0009;
    private const int SupplementaryPlaneBegin = 0x10000;

    /// <summary>Splits on <c>\r\n</c>, <c>\r</c> or <c>\n</c> (Monaco: <c>splitLines</c>, regex <c>/\r\n|\r|\n/</c>).</summary>
    public static string[] SplitLines(string str)
    {
        var lines = new List<string>();
        var start = 0;
        for (var i = 0; i < str.Length; i++)
        {
            var ch = str[i];
            if (ch == '\r')
            {
                lines.Add(str.Substring(start, i - start));
                if (i + 1 < str.Length && str[i + 1] == '\n')
                {
                    i++;
                }

                start = i + 1;
            }
            else if (ch == '\n')
            {
                lines.Add(str.Substring(start, i - start));
                start = i + 1;
            }
        }

        lines.Add(str.Substring(start));
        return lines.ToArray();
    }

    /// <summary>Returns the leading spaces and tabs of <paramref name="str"/> within [start, end) (Monaco: <c>getLeadingWhitespace</c>).</summary>
    public static string GetLeadingWhitespace(string str, int start = 0, int? end = null)
    {
        var endIndex = end ?? str.Length;
        for (var i = start; i < endIndex; i++)
        {
            var chCode = str[i];
            if (chCode != Space && chCode != Tab)
            {
                return str.Substring(start, i - start);
            }
        }

        return str.Substring(start, endIndex - start);
    }

    /// <summary>Last index at or before <paramref name="startIndex"/> that is not a space or tab, or -1 (Monaco: <c>lastNonWhitespaceIndex</c>).</summary>
    public static int LastNonWhitespaceIndex(string str, int startIndex)
    {
        for (var i = startIndex; i >= 0; i--)
        {
            var chCode = str[i];
            if (chCode != Space && chCode != Tab)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Same as <see cref="LastNonWhitespaceIndex(string, int)"/> starting from the end of the string.</summary>
    public static int LastNonWhitespaceIndex(string str) => LastNonWhitespaceIndex(str, str.Length - 1);

    /// <summary>True for '0'..'9' (Monaco: <c>isAsciiDigit</c>).</summary>
    public static bool IsAsciiDigit(int code) => code >= '0' && code <= '9';

    /// <summary>True for 'a'..'z' (Monaco: <c>isLowerAsciiLetter</c>).</summary>
    public static bool IsLowerAsciiLetter(int code) => code >= 'a' && code <= 'z';

    /// <summary>True for 'A'..'Z' (Monaco: <c>isUpperAsciiLetter</c>).</summary>
    public static bool IsUpperAsciiLetter(int code) => code >= 'A' && code <= 'Z';

    /// <summary>Length of the common prefix of the two strings (Monaco: <c>commonPrefixLength</c>).</summary>
    public static int CommonPrefixLength(string a, string b)
    {
        var len = Math.Min(a.Length, b.Length);
        int i;
        for (i = 0; i < len; i++)
        {
            if (a[i] != b[i])
            {
                return i;
            }
        }

        return len;
    }

    /// <summary>Length of the common suffix of the two strings (Monaco: <c>commonSuffixLength</c>).</summary>
    public static int CommonSuffixLength(string a, string b)
    {
        var len = Math.Min(a.Length, b.Length);
        var aLastIndex = a.Length - 1;
        var bLastIndex = b.Length - 1;
        int i;
        for (i = 0; i < len; i++)
        {
            if (a[aLastIndex - i] != b[bLastIndex - i])
            {
                return i;
            }
        }

        return len;
    }

    /// <summary>True for UTF-16 high surrogates (Monaco: <c>isHighSurrogate</c>).</summary>
    public static bool IsHighSurrogate(int charCode) => 0xD800 <= charCode && charCode <= 0xDBFF;

    /// <summary>True for UTF-16 low surrogates (Monaco: <c>isLowSurrogate</c>).</summary>
    public static bool IsLowSurrogate(int charCode) => 0xDC00 <= charCode && charCode <= 0xDFFF;

    /// <summary>Combines a surrogate pair into a code point (Monaco: <c>computeCodePoint</c>).</summary>
    public static int ComputeCodePoint(int highSurrogate, int lowSurrogate)
        => ((highSurrogate - 0xD800) << 10) + (lowSurrogate - 0xDC00) + 0x10000;

    /// <summary>The code point that begins at <paramref name="offset"/> (Monaco: <c>getNextCodePoint</c>).</summary>
    public static int GetNextCodePoint(string str, int len, int offset)
    {
        Guard.Assert(offset >= 0 && offset < str.Length);
        int charCode = str[offset];
        if (IsHighSurrogate(charCode) && offset + 1 < len)
        {
            int nextCharCode = str[offset + 1];
            if (IsLowSurrogate(nextCharCode))
            {
                return ComputeCodePoint(charCode, nextCharCode);
            }
        }

        return charCode;
    }

    /// <summary>The code point that ends right before <paramref name="offset"/> (Monaco: <c>getPrevCodePoint</c>).</summary>
    public static int GetPrevCodePoint(string str, int offset)
    {
        Guard.Assert(offset > 0 && offset <= str.Length);
        int charCode = str[offset - 1];
        if (IsLowSurrogate(charCode) && offset > 1)
        {
            int prevCharCode = str[offset - 2];
            if (IsHighSurrogate(prevCharCode))
            {
                return ComputeCodePoint(prevCharCode, charCode);
            }
        }

        return charCode;
    }

    /// <summary>Number of UTF-16 units of a code point (2 for supplementary planes).</summary>
    internal static int CodePointLength(int codePoint) => codePoint >= SupplementaryPlaneBegin ? 2 : 1;

    /// <summary>
    /// True when every character matches JavaScript's regex <c>\s</c>, so for an empty string too (Monaco:
    /// <c>/^\s*$/.test(text)</c>). Not <see cref="char.IsWhiteSpace(char)"/>: the sets differ (for instance U+200B and U+0085).
    /// </summary>
    internal static bool IsJavaScriptWhitespaceOnly(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        foreach (char ch in text)
        {
            bool isWhitespace = ch is '\t' or '\n' or '\v' or '\f' or '\r' or ' ' or '\u00A0' or '\u1680'
                or (>= '\u2000' and <= '\u200A') or '\u2028' or '\u2029' or '\u202F' or '\u205F' or '\u3000' or '\uFEFF';
            if (!isWhitespace)
            {
                return false;
            }
        }

        return true;
    }
}

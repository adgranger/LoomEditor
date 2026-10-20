// Ported from microsoft/vscode: src/vs/editor/common/languages/supports/richEditBrackets.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Languages.Supports;

/// <summary>
/// Searches brackets in a line with the regular expressions of <see cref="RichEditBrackets"/> (Monaco:
/// <c>BracketsUtils</c>), and builds those regular expressions (Monaco's module functions of the same file).
/// </summary>
/// <remarks>Threading: pure static methods; thread-safe.</remarks>
internal static class BracketsUtils
{
    /// <summary>Monaco: <c>InternalBracket</c>.</summary>
    internal sealed record InternalBracket(IReadOnlyList<string> Open, IReadOnlyList<string> Close);

    public static Range? FindPrevBracketInRange(Regex reversedBracketRegex, int lineNumber, string lineText, int startOffset, int endOffset)
    {
        // Because JS does not support backwards regex search, we search forwards in a reversed string with a reversed regex ;)
        string reversedLineText = ToReversedString(lineText);
        string reversedSubstr = reversedLineText.Substring(lineText.Length - endOffset, endOffset - startOffset);
        return FindPrevBracketInText(reversedBracketRegex, lineNumber, reversedSubstr, startOffset);
    }

    public static Range? FindNextBracketInText(Regex bracketRegex, int lineNumber, string text, int offset)
    {
        Match m = bracketRegex.Match(text);
        if (!m.Success)
        {
            return null;
        }

        int matchOffset = m.Index;
        int matchLength = m.Length;
        if (matchLength == 0)
        {
            return null;
        }

        int absoluteMatchOffset = offset + matchOffset;

        return new Range(lineNumber, absoluteMatchOffset + 1, lineNumber, absoluteMatchOffset + 1 + matchLength);
    }

    public static Range? FindNextBracketInRange(Regex bracketRegex, int lineNumber, string lineText, int startOffset, int endOffset)
    {
        string substr = lineText.Substring(startOffset, endOffset - startOffset);
        return FindNextBracketInText(bracketRegex, lineNumber, substr, startOffset);
    }

    /// <summary>
    /// <c>(p1)|(p2)|…</c>, case-insensitive (Monaco's <c>createRegExp</c> adds the <c>i</c> flag because
    /// <c>matchCase</c> is unset). The <c>options</c> parameter is dropped: the only option Monaco passes is
    /// <c>global</c>, which a .NET regex does not need. No pieces gives <c>()</c>, which matches the empty string.
    /// </summary>
    public static Regex CreateBracketOrRegExp(IEnumerable<string> pieces)
    {
        string regexStr = $"({string.Join(")|(", pieces.Select(PrepareBracketForRegExp))})";
        return new Regex(regexStr, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    internal static Regex GetRegexForBracketPair(IReadOnlyList<string> open, IReadOnlyList<string> close, List<InternalBracket> brackets, int currentIndex) =>
        CreateBracketOrRegExp(GetPiecesForBracketPair(open, close, brackets, currentIndex));

    internal static Regex GetReversedRegexForBracketPair(IReadOnlyList<string> open, IReadOnlyList<string> close, List<InternalBracket> brackets, int currentIndex) =>
        CreateBracketOrRegExp(GetPiecesForBracketPair(open, close, brackets, currentIndex).Select(ToReversedString));

    internal static Regex GetRegexForBrackets(IReadOnlyList<RichEditBracket> brackets) =>
        CreateBracketOrRegExp(GetPiecesForBrackets(brackets));

    internal static Regex GetReversedRegexForBrackets(IReadOnlyList<RichEditBracket> brackets) =>
        CreateBracketOrRegExp(GetPiecesForBrackets(brackets).Select(ToReversedString));

    /// <summary>Reverses the UTF-16 code units, like Monaco (its one-entry cache is dropped: no static mutable state).</summary>
    internal static string ToReversedString(string str)
    {
        char[] arr = str.ToCharArray();
        Array.Reverse(arr);
        return new string(arr);
    }

    private static Range? FindPrevBracketInText(Regex reversedBracketRegex, int lineNumber, string reversedText, int offset)
    {
        Match m = reversedBracketRegex.Match(reversedText);
        if (!m.Success)
        {
            return null;
        }

        int matchOffset = reversedText.Length - m.Index;
        int matchLength = m.Length;
        int absoluteMatchOffset = offset + matchOffset;

        return new Range(lineNumber, absoluteMatchOffset - matchLength + 1, lineNumber, absoluteMatchOffset + 1);
    }

    // The common part of `getRegexForBracketPair` and `getReversedRegexForBracketPair`.
    private static List<string> GetPiecesForBracketPair(IReadOnlyList<string> open, IReadOnlyList<string> close, List<InternalBracket> brackets, int currentIndex)
    {
        // search in all brackets for other brackets that are a superstring of these brackets
        var pieces = new List<string>(open);
        pieces.AddRange(close);
        for (int i = 0, len = pieces.Count; i < len; i++)
        {
            CollectSuperstrings(pieces[i], brackets, currentIndex, pieces);
        }

        // `pieces.sort(lengthcmp); pieces.reverse()`: a stable sort by length, then reverse.
        List<string> sorted = [.. Unique(pieces).OrderBy(piece => piece.Length)];
        sorted.Reverse();
        return sorted;
    }

    // The common part of `getRegexForBrackets` and `getReversedRegexForBrackets`.
    private static List<string> GetPiecesForBrackets(IReadOnlyList<RichEditBracket> brackets)
    {
        var pieces = new List<string>();
        foreach (RichEditBracket bracket in brackets)
        {
            pieces.AddRange(bracket.Open);
            pieces.AddRange(bracket.Close);
        }

        return Unique(pieces);
    }

    private static void CollectSuperstrings(string str, List<InternalBracket> brackets, int currentIndex, List<string> dest)
    {
        for (int i = 0, len = brackets.Count; i < len; i++)
        {
            if (i == currentIndex)
            {
                continue;
            }

            InternalBracket bracket = brackets[i];
            foreach (string open in bracket.Open)
            {
                if (open.Contains(str, StringComparison.Ordinal))
                {
                    dest.Add(open);
                }
            }

            foreach (string close in bracket.Close)
            {
                if (close.Contains(str, StringComparison.Ordinal))
                {
                    dest.Add(close);
                }
            }
        }
    }

    // Keeps the first occurrence.
    private static List<string> Unique(List<string> arr)
    {
        if (arr.Count <= 1)
        {
            return arr;
        }

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string element in arr)
        {
            if (seen.Add(element))
            {
                result.Add(element);
            }
        }

        return result;
    }

    private static string PrepareBracketForRegExp(string str)
    {
        // This bracket pair uses letters like e.g. "begin" - "end". JavaScript's `\w` is ASCII.
        bool insertWordBoundaries = str.Length > 0 && str.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or ' ');
        str = Text.Strings.EscapeRegExpCharacters(str);
        return insertWordBoundaries ? $@"\b{str}\b" : str;
    }
}

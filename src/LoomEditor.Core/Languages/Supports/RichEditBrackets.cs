// Ported from microsoft/vscode: src/vs/editor/common/languages/supports/richEditBrackets.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;

namespace LoomEditor.Core.Languages.Supports;

/// <summary>
/// Represents a grouping of colliding bracket pairs (Monaco: <c>RichEditBracket</c>). Most of the times this contains
/// a single bracket pair, but sometimes this contains multiple bracket pairs in cases where the same string appears as
/// a closing bracket for multiple bracket pairs, or the same string appears an opening bracket for multiple bracket
/// pairs (e.g. <c>open: ['if', 'for'], close: ['end', 'end']</c>).
/// </summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed class RichEditBracket
{
    private readonly HashSet<string> _openSet;
    private readonly HashSet<string> _closeSet;

    public RichEditBracket(string languageId, int index, IReadOnlyList<string> open, IReadOnlyList<string> close, Regex forwardRegex, Regex reversedRegex)
    {
        LanguageId = languageId;
        Index = index;
        Open = open;
        Close = close;
        ForwardRegex = forwardRegex;
        ReversedRegex = reversedRegex;
        _openSet = new HashSet<string>(open, StringComparer.Ordinal);
        _closeSet = new HashSet<string>(close, StringComparer.Ordinal);
    }

    public string LanguageId { get; }

    /// <summary>A 0-based consecutive unique identifier for this bracket pair group.</summary>
    public int Index { get; }

    /// <summary>The open sequence for each bracket pair contained in this group; <c>[Open[i], Close[i]]</c> is a pair.</summary>
    public IReadOnlyList<string> Open { get; }

    /// <summary>The close sequence for each bracket pair contained in this group.</summary>
    public IReadOnlyList<string> Close { get; }

    /// <summary>
    /// A regular expression that is useful to search for this bracket pair group in a string. It is aware of the
    /// other bracket pairs defined for the language, so it might match brackets from other groups.
    /// </summary>
    public Regex ForwardRegex { get; }

    /// <summary>A regular expression that is useful to search for this bracket pair group in a string backwards.</summary>
    public Regex ReversedRegex { get; }

    /// <summary>Check if the provided <paramref name="text"/> is an open bracket in this group.</summary>
    public bool IsOpen(string text) => _openSet.Contains(text);

    /// <summary>Check if the provided <paramref name="text"/> is a close bracket in this group.</summary>
    public bool IsClose(string text) => _closeSet.Contains(text);
}

/// <summary>The bracket groups of a language and the regular expressions to find them (Monaco: <c>RichEditBrackets</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed class RichEditBrackets
{
    public RichEditBrackets(string languageId, IReadOnlyList<CharacterPair> brackets)
    {
        List<BracketsUtils.InternalBracket> groups = GroupFuzzyBrackets(brackets);
        Brackets = [.. groups.Select((b, index) => new RichEditBracket(
            languageId,
            index,
            b.Open,
            b.Close,
            BracketsUtils.GetRegexForBracketPair(b.Open, b.Close, groups, index),
            BracketsUtils.GetReversedRegexForBracketPair(b.Open, b.Close, groups, index)))];
        ForwardRegex = BracketsUtils.GetRegexForBrackets(Brackets);
        ReversedRegex = BracketsUtils.GetReversedRegexForBrackets(Brackets);

        var textIsBracket = new Dictionary<string, RichEditBracket>(StringComparer.Ordinal);
        var textIsOpenBracket = new Dictionary<string, bool>(StringComparer.Ordinal);
        int maxBracketLength = 0;
        foreach (RichEditBracket bracket in Brackets)
        {
            foreach (string open in bracket.Open)
            {
                textIsBracket[open] = bracket;
                textIsOpenBracket[open] = true;
                maxBracketLength = Math.Max(maxBracketLength, open.Length);
            }

            foreach (string close in bracket.Close)
            {
                textIsBracket[close] = bracket;
                textIsOpenBracket[close] = false;
                maxBracketLength = Math.Max(maxBracketLength, close.Length);
            }
        }

        TextIsBracket = textIsBracket;
        TextIsOpenBracket = textIsOpenBracket;
        MaxBracketLength = maxBracketLength;
    }

    /// <summary>All groups of brackets defined for this language.</summary>
    public IReadOnlyList<RichEditBracket> Brackets { get; }

    /// <summary>A regular expression that is useful to search for all bracket pairs in a string.</summary>
    public Regex ForwardRegex { get; }

    /// <summary>A regular expression that is useful to search for all bracket pairs in a string backwards.</summary>
    public Regex ReversedRegex { get; }

    /// <summary>The length (i.e. str.length) for the longest bracket pair.</summary>
    public int MaxBracketLength { get; }

    /// <summary>A map useful for decoding a regex match and finding which bracket group was matched.</summary>
    public IReadOnlyDictionary<string, RichEditBracket> TextIsBracket { get; }

    /// <summary>A set useful for decoding if a regex match is the open bracket of a bracket pair.</summary>
    public IReadOnlyDictionary<string, bool> TextIsOpenBracket { get; }

    /// <summary>
    /// Groups together brackets that have equal open or close sequences (Monaco: <c>groupFuzzyBrackets</c>).
    /// <c>['IF','END']</c>, <c>['for','end']</c>, <c>['{','}']</c> give <c>{ open: ['if', 'for'], close: ['end', 'end'] }</c>
    /// and <c>{ open: ['{'], close: ['}'] }</c>.
    /// </summary>
    private static List<BracketsUtils.InternalBracket> GroupFuzzyBrackets(IReadOnlyList<CharacterPair> brackets)
    {
        int n = brackets.Count;

        CharacterPair[] lowered = [.. brackets.Select(b => new CharacterPair(b.Open.ToLowerInvariant(), b.Close.ToLowerInvariant()))];

        int[] group = new int[n];
        for (int i = 0; i < n; i++)
        {
            group[i] = i;
        }

        static bool AreOverlapping(CharacterPair a, CharacterPair b) =>
            a.Open == b.Open || a.Open == b.Close || a.Close == b.Open || a.Close == b.Close;

        void MergeGroups(int g1, int g2)
        {
            int newG = Math.Min(g1, g2);
            int oldG = Math.Max(g1, g2);
            for (int i = 0; i < n; i++)
            {
                if (group[i] == oldG)
                {
                    group[i] = newG;
                }
            }
        }

        // group together brackets that have the same open or the same close sequence
        for (int i = 0; i < n; i++)
        {
            CharacterPair a = lowered[i];
            for (int j = i + 1; j < n; j++)
            {
                CharacterPair b = lowered[j];
                if (AreOverlapping(a, b))
                {
                    MergeGroups(group[i], group[j]);
                }
            }
        }

        var result = new List<BracketsUtils.InternalBracket>();
        for (int g = 0; g < n; g++)
        {
            var currentOpen = new List<string>();
            var currentClose = new List<string>();
            for (int i = 0; i < n; i++)
            {
                if (group[i] == g)
                {
                    currentOpen.Add(lowered[i].Open);
                    currentClose.Add(lowered[i].Close);
                }
            }

            if (currentOpen.Count > 0)
            {
                result.Add(new BracketsUtils.InternalBracket(currentOpen, currentClose));
            }
        }

        return result;
    }
}

// Ported from microsoft/vscode: src/vs/base/test/common/filters.test.ts (assertMatches)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;
using LoomEditor.Core.Filters;

namespace LoomEditor.Core.Tests.Filters.TestHelpers;

internal static class FuzzyAssert
{
    /// <summary>Monaco: <c>assertMatches</c>. A null <paramref name="decoratedWord"/> means "no match"; <c>^</c> precedes each matched character.</summary>
    public static void Matches(string pattern, string word, string? decoratedWord, FuzzyScorer filter, int patternPos = 0, int wordPos = 0, bool firstMatchCanBeWeak = false)
    {
        var r = filter(pattern, pattern.ToLowerInvariant(), patternPos, word, word.ToLowerInvariant(), wordPos, new FuzzyScoreOptions(firstMatchCanBeWeak, BoostFullMatch: true));
        Assert.Equal(decoratedWord is null, r is null);
        if (r is null)
        {
            return;
        }

        var actual = new StringBuilder();
        var pos = 0;
        foreach (var match in Fuzzy.CreateMatches(r))
        {
            actual.Append(word, pos, match.Start - pos);
            actual.Append('^').Append(string.Join("^", word.Substring(match.Start, match.End - match.Start).ToCharArray()));
            pos = match.End;
        }

        actual.Append(word, pos, word.Length - pos);
        Assert.Equal(decoratedWord, actual.ToString());
    }
}

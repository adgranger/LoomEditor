// Ported from microsoft/vscode: src/vs/base/test/common/filters.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Filters;
using LoomEditor.Core.Tests.Filters.TestHelpers;

namespace LoomEditor.Core.Tests.Filters;

[Collection("Fuzzy")] // Fuzzy uses shared static tables: not thread-safe
public class FuzzyGracefulTests
{
    private static readonly FuzzyScorer Plain = Fuzzy.Score;
    private static readonly FuzzyScorer Graceful = Fuzzy.ScoreGraceful;
    private static readonly FuzzyScorer Aggressive = Fuzzy.ScoreGracefulAggressive;

    // Monaco: 'fuzzyScoreGraceful'
    [Fact]
    public void FuzzyScoreGraceful_Monaco()
    {
        FuzzyAssert.Matches("rlut", "result", null, Plain);
        FuzzyAssert.Matches("rlut", "result", "^res^u^l^t", Graceful);

        FuzzyAssert.Matches("cno", "console", "^co^ns^ole", Plain);
        FuzzyAssert.Matches("cno", "console", "^co^ns^ole", Graceful);
        FuzzyAssert.Matches("cno", "console", "^c^o^nsole", Aggressive);
        FuzzyAssert.Matches("cno", "co_new", "^c^o_^new", Graceful);
        FuzzyAssert.Matches("cno", "co_new", "^c^o_^new", Aggressive);
    }

    // Monaco: 'Suggestion is not highlighted #85826'
    [Fact]
    public void FuzzyScore_Issue85826()
    {
        FuzzyAssert.Matches("SemanticTokens", "SemanticTokensEdits", "^S^e^m^a^n^t^i^c^T^o^k^e^n^sEdits", Plain);
        FuzzyAssert.Matches("SemanticTokens", "SemanticTokensEdits", "^S^e^m^a^n^t^i^c^T^o^k^e^n^sEdits", Aggressive);
    }

    [Fact]
    public void ScoreGraceful_ReturnsPlainScoreWhenItMatches()
    {
        var plain = Fuzzy.Score("cno", "cno", 0, "console", "console", 0);
        Assert.Equal(plain, Fuzzy.ScoreGraceful("cno", "cno", 0, "console", "console", 0));
    }

    [Fact]
    public void ScoreGracefulAggressive_PatternShorterThanThree_NoPermutations()
        => Assert.Equal(
            Fuzzy.Score("cn", "cn", 0, "console", "console", 0),
            Fuzzy.ScoreGracefulAggressive("cn", "cn", 0, "console", "console", 0));

    [Fact]
    public void NextTypoPermutation_EqualNeighbours_Null() => Assert.Null(Fuzzy.NextTypoPermutation("foo", 1));

    [Fact]
    public void NextTypoPermutation_SwapsNeighbours() => Assert.Equal("conso", Fuzzy.NextTypoPermutation("cnoso", 1));

    [Fact]
    public void NextTypoPermutation_AtEnd_Null() => Assert.Null(Fuzzy.NextTypoPermutation("abc", 2));

    [Fact]
    public void ScoreGraceful_PermutationPenalty()
    {
        var graceful = Fuzzy.ScoreGraceful("rlut", "rlut", 0, "result", "result", 0);
        var permuted = Fuzzy.Score("rult", "rult", 0, "result", "result", 0);
        Assert.NotNull(graceful);
        Assert.NotNull(permuted);
        Assert.Equal(permuted[0] - 3, graceful[0]);
    }
}

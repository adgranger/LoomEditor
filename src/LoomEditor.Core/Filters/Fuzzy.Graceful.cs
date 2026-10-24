// Ported from microsoft/vscode: src/vs/base/common/filters.ts (fuzzyScoreGraceful*, fuzzyScoreWithPermutations, nextTypoPermutation)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Filters;

internal static partial class Fuzzy
{
    /// <summary>Monaco: <c>fuzzyScoreGracefulAggressive</c>. Always tries the permuted patterns and keeps the best.</summary>
    public static int[]? ScoreGracefulAggressive(string pattern, string lowPattern, int patternPos, string word, string lowWord, int wordPos, FuzzyScoreOptions? options = null)
        => ScoreWithPermutations(pattern, lowPattern, patternPos, word, lowWord, wordPos, true, options);

    /// <summary>Monaco: <c>fuzzyScoreGraceful</c>. Tries permuted patterns only when the pattern itself does not match.</summary>
    public static int[]? ScoreGraceful(string pattern, string lowPattern, int patternPos, string word, string lowWord, int wordPos, FuzzyScoreOptions? options = null)
        => ScoreWithPermutations(pattern, lowPattern, patternPos, word, lowWord, wordPos, false, options);

    private static int[]? ScoreWithPermutations(string pattern, string lowPattern, int patternPos, string word, string lowWord, int wordPos, bool aggressive, FuzzyScoreOptions? options)
    {
        var top = Score(pattern, lowPattern, patternPos, word, lowWord, wordPos, options);

        if (top is not null && !aggressive)
        {
            // the original pattern yields a result: return it unless we are aggressive
            return top;
        }

        if (pattern.Length >= 3)
        {
            // try a few (max 7) permutations that swap neighbouring characters
            var tries = Math.Min(7, pattern.Length - 1);
            for (var movingPatternPos = patternPos + 1; movingPatternPos < tries; movingPatternPos++)
            {
                var newPattern = NextTypoPermutation(pattern, movingPatternPos);
                if (newPattern is not null)
                {
                    var candidate = Score(newPattern, newPattern.ToLowerInvariant(), patternPos, word, lowWord, wordPos, options);
                    if (candidate is not null)
                    {
                        candidate[0] -= 3; // permutation penalty
                        if (top is null || candidate[0] > top[0])
                        {
                            top = candidate;
                        }
                    }
                }
            }
        }

        return top;
    }

    /// <summary>Monaco: <c>nextTypoPermutation</c>. Swaps the characters at <paramref name="patternPos"/> and the next one; null at the end or when they are equal.</summary>
    internal static string? NextTypoPermutation(string pattern, int patternPos)
    {
        if (patternPos + 1 >= pattern.Length)
        {
            return null;
        }

        var swap1 = pattern[patternPos];
        var swap2 = pattern[patternPos + 1];
        if (swap1 == swap2)
        {
            return null;
        }

        return pattern[..patternPos] + swap2 + swap1 + pattern[(patternPos + 2)..];
    }
}

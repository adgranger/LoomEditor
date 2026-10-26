// Ported from microsoft/vscode: src/vs/base/common/filters.ts (anyScore, createMatches, isPatternInWord)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Filters;

/// <summary>The fuzzy scorer of completion (Monaco: <c>filters.ts</c>, <c>fuzzyScore</c> and friends).</summary>
/// <remarks>
/// Threading: UI thread only. The score tables are static and shared, as in Monaco.
/// The lower-cased arguments (<c>patternLow</c>, <c>wordLow</c>) must come from <c>ToLowerInvariant()</c> (Monaco: <c>toLowerCase()</c>).
/// </remarks>
internal static partial class Fuzzy
{
    /// <summary>Monaco: <c>anyScore</c>. Returns a new array each call; <c>[0, wordPos]</c> when nothing matches.</summary>
    public static int[] AnyScore(string pattern, string lowPattern, int patternPos, string word, string lowWord, int wordPos)
    {
        var max = Math.Min(13, pattern.Length);
        var options = new FuzzyScoreOptions(FirstMatchCanBeWeak: true, BoostFullMatch: true);
        for (; patternPos < max; patternPos++)
        {
            var result = Score(pattern, lowPattern, patternPos, word, lowWord, wordPos, options);
            if (result is not null)
            {
                return result;
            }
        }

        return [0, wordPos];
    }

    /// <summary>Monaco: <c>createMatches</c>. Null gives an empty list.</summary>
    public static IReadOnlyList<FilterMatch> CreateMatches(int[]? score)
    {
        var res = new List<FilterMatch>();
        if (score is null)
        {
            return res;
        }

        var wordPos = score[1];
        for (var i = score.Length - 1; i > 1; i--)
        {
            var pos = score[i] + wordPos;
            if (res.Count > 0 && res[^1].End == pos)
            {
                res[^1] = res[^1] with { End = pos + 1 };
            }
            else
            {
                res.Add(new FilterMatch(pos, pos + 1));
            }
        }

        return res;
    }

    /// <summary>Monaco: <c>isPatternInWord</c>.</summary>
    public static bool IsPatternInWord(string patternLow, int patternPos, int patternLen, string wordLow, int wordPos, int wordLen, bool fillMinWordPosArr = false)
    {
        while (patternPos < patternLen && wordPos < wordLen)
        {
            if (patternLow[patternPos] == wordLow[wordPos])
            {
                if (fillMinWordPosArr)
                {
                    // Remember the min word position for each pattern position
                    MinWordMatchPos[patternPos] = wordPos;
                }

                patternPos += 1;
            }

            wordPos += 1;
        }

        return patternPos == patternLen; // pattern must be exhausted
    }
}

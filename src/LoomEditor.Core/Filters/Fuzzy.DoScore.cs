// Ported from microsoft/vscode: src/vs/base/common/filters.ts (fuzzyScore, _fillInMaxWordMatchPos, _doScore)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Filters;

internal static partial class Fuzzy
{
    private enum Arrow : byte { Diag = 1, Left = 2, LeftLeft = 3 }

    private const int MaxLen = 128;

    private static readonly int[] MinWordMatchPos = new int[(2 * MaxLen) + 1]; // min word position for a certain pattern position
    private static readonly int[] MaxWordMatchPos = new int[(2 * MaxLen) + 1]; // max word position for a certain pattern position
    private static readonly int[][] Diag = InitTable<int>(); // the length of a contiguous diagonal match
    private static readonly int[][] Table = InitTable<int>();
    private static readonly Arrow[][] Arrows = InitTable<Arrow>();

    private static T[][] InitTable<T>()
    {
        var table = new T[MaxLen + 1][];
        for (var i = 0; i <= MaxLen; i++)
        {
            table[i] = new T[MaxLen + 1];
        }

        return table;
    }

    // JavaScript: word[pos] !== wordLow[pos]; an out-of-range index is undefined on both sides, so false.
    private static bool IsUpperCaseAtPos(int pos, string word, string wordLow)
        => pos >= 0 && pos < word.Length && pos < wordLow.Length && word[pos] != wordLow[pos];

    /// <summary>Monaco: <c>fuzzyScore</c>. Null when there is no match.</summary>
    public static int[]? Score(string pattern, string patternLow, int patternStart, string word, string wordLow, int wordStart, FuzzyScoreOptions? options = null)
    {
        options ??= FuzzyScoreOptions.Default;
        var patternLen = pattern.Length > MaxLen ? MaxLen : pattern.Length;
        var wordLen = word.Length > MaxLen ? MaxLen : word.Length;

        if (patternStart >= patternLen || wordStart >= wordLen || (patternLen - patternStart) > (wordLen - wordStart))
        {
            return null;
        }

        // Run a simple check if the characters of pattern occur (in order) at all in word.
        if (!IsPatternInWord(patternLow, patternStart, patternLen, wordLow, wordStart, wordLen, true))
        {
            return null;
        }

        // Find the max matching word position for each pattern position
        FillInMaxWordMatchPos(patternLen, wordLen, patternStart, wordStart, patternLow, wordLow);

        var row = 1;
        var column = 1;
        var patternPos = patternStart;
        var hasStrongFirstMatch = false;

        // There will be a match, fill in tables
        for (row = 1, patternPos = patternStart; patternPos < patternLen; row++, patternPos++)
        {
            // Reduce search space to possible matching word positions and to possible access from next row
            var minWordMatchPos = MinWordMatchPos[patternPos];
            var maxWordMatchPos = MaxWordMatchPos[patternPos];
            var nextMaxWordMatchPos = patternPos + 1 < patternLen ? MaxWordMatchPos[patternPos + 1] : wordLen;

            var wordPos = minWordMatchPos;
            for (column = minWordMatchPos - wordStart + 1; wordPos < nextMaxWordMatchPos; column++, wordPos++)
            {
                var score = int.MinValue;
                var canComeDiag = false;

                if (wordPos <= maxWordMatchPos)
                {
                    score = DoScore(
                        pattern, patternLow, patternPos, patternStart,
                        word, wordLow, wordPos, wordLen, wordStart,
                        Diag[row - 1][column - 1] == 0,
                        ref hasStrongFirstMatch);
                }

                var diagScore = 0;
                if (score != int.MinValue)
                {
                    canComeDiag = true;
                    diagScore = score + Table[row - 1][column - 1];
                }

                var canComeLeft = wordPos > minWordMatchPos;
                var leftScore = canComeLeft ? Table[row][column - 1] + (Diag[row][column - 1] > 0 ? -5 : 0) : 0; // penalty for a gap start

                var canComeLeftLeft = wordPos > minWordMatchPos + 1 && Diag[row][column - 1] > 0;
                var leftLeftScore = canComeLeftLeft ? Table[row][column - 2] + (Diag[row][column - 2] > 0 ? -5 : 0) : 0; // penalty for a gap start

                if (canComeLeftLeft && (!canComeLeft || leftLeftScore >= leftScore) && (!canComeDiag || leftLeftScore >= diagScore))
                {
                    // always prefer choosing left left to jump over a diagonal because that means a match is earlier in the word
                    Table[row][column] = leftLeftScore;
                    Arrows[row][column] = Arrow.LeftLeft;
                    Diag[row][column] = 0;
                }
                else if (canComeLeft && (!canComeDiag || leftScore >= diagScore))
                {
                    // always prefer choosing left since that means a match is earlier in the word
                    Table[row][column] = leftScore;
                    Arrows[row][column] = Arrow.Left;
                    Diag[row][column] = 0;
                }
                else if (canComeDiag)
                {
                    Table[row][column] = diagScore;
                    Arrows[row][column] = Arrow.Diag;
                    Diag[row][column] = Diag[row - 1][column - 1] + 1;
                }
                else
                {
                    throw new InvalidOperationException("not possible");
                }
            }
        }

        if (!hasStrongFirstMatch && !options.FirstMatchCanBeWeak)
        {
            return null;
        }

        row--;
        column--;

        var result = new List<int> { Table[row][column], wordStart };

        var backwardsDiagLength = 0;
        var maxMatchColumn = 0;

        while (row >= 1)
        {
            // Find the column where we go diagonally up
            var diagColumn = column;
            do
            {
                var arrow = Arrows[row][diagColumn];
                if (arrow == Arrow.LeftLeft)
                {
                    diagColumn -= 2;
                }
                else if (arrow == Arrow.Left)
                {
                    diagColumn -= 1;
                }
                else
                {
                    // found the diagonal
                    break;
                }
            }
            while (diagColumn >= 1);

            // Overturn the "forwards" decision if keeping the "backwards" diagonal would give a better match
            if (backwardsDiagLength > 1 // only if we would have a contiguous match of 3 characters
                && patternLow[patternStart + row - 1] == wordLow[wordStart + column - 1] // only if we can do a contiguous match diagonally
                && !IsUpperCaseAtPos(diagColumn + wordStart - 1, word, wordLow) // only if the forwards chose diagonal is not an uppercase
                && backwardsDiagLength + 1 > Diag[row][diagColumn]) // only if our contiguous match would be longer than the "forwards" contiguous match
            {
                diagColumn = column;
            }

            backwardsDiagLength = diagColumn == column ? backwardsDiagLength + 1 : 1; // contiguous match or not

            if (maxMatchColumn == 0)
            {
                // remember the last matched column
                maxMatchColumn = diagColumn;
            }

            row--;
            column = diagColumn - 1;
            result.Add(column);
        }

        if (wordLen - wordStart == patternLen && options.BoostFullMatch)
        {
            // the word matches the pattern with all characters: total match boost
            result[0] += 2;
        }

        // Add 1 penalty for each skipped character in the word
        result[0] -= maxMatchColumn - patternLen;

        return [.. result];
    }

    private static void FillInMaxWordMatchPos(int patternLen, int wordLen, int patternStart, int wordStart, string patternLow, string wordLow)
    {
        var patternPos = patternLen - 1;
        var wordPos = wordLen - 1;
        while (patternPos >= patternStart && wordPos >= wordStart)
        {
            if (patternLow[patternPos] == wordLow[wordPos])
            {
                MaxWordMatchPos[patternPos] = wordPos;
                patternPos--;
            }

            wordPos--;
        }
    }

    private static int DoScore(
        string pattern, string patternLow, int patternPos, int patternStart,
        string word, string wordLow, int wordPos, int wordLen, int wordStart,
        bool newMatchStart,
        ref bool outFirstMatchStrong)
    {
        if (patternLow[patternPos] != wordLow[wordPos])
        {
            return int.MinValue;
        }

        var score = 1;
        var isGapLocation = false;
        if (wordPos == patternPos - patternStart)
        {
            // common prefix: `foobar <-> foobaz`
            score = pattern[patternPos] == word[wordPos] ? 7 : 5;
        }
        else if (IsUpperCaseAtPos(wordPos, word, wordLow) && (wordPos == 0 || !IsUpperCaseAtPos(wordPos - 1, word, wordLow)))
        {
            // hitting upper-case: `foo <-> forOthers`
            score = pattern[patternPos] == word[wordPos] ? 7 : 5;
            isGapLocation = true;
        }
        else if (CharacterClasses.IsSeparatorAtPos(wordLow, wordPos) && (wordPos == 0 || !CharacterClasses.IsSeparatorAtPos(wordLow, wordPos - 1)))
        {
            // hitting a separator: `. <-> foo.bar`
            score = 5;
        }
        else if (CharacterClasses.IsSeparatorAtPos(wordLow, wordPos - 1) || CharacterClasses.IsWhitespaceAtPos(wordLow, wordPos - 1))
        {
            // post separator: `foo <-> bar_foo`
            score = 5;
            isGapLocation = true;
        }

        if (score > 1 && patternPos == patternStart)
        {
            outFirstMatchStrong = true;
        }

        if (!isGapLocation)
        {
            isGapLocation = IsUpperCaseAtPos(wordPos, word, wordLow)
                || CharacterClasses.IsSeparatorAtPos(wordLow, wordPos - 1)
                || CharacterClasses.IsWhitespaceAtPos(wordLow, wordPos - 1);
        }

        if (patternPos == patternStart)
        {
            // first character in pattern
            if (wordPos > wordStart)
            {
                // the first pattern character would match a word character that is not at the word start
                // so introduce a penalty to account for the gap preceding this match
                score -= isGapLocation ? 3 : 5;
            }
        }
        else if (newMatchStart)
        {
            // this would be the beginning of a new match (i.e. there would be a gap before this location)
            score += isGapLocation ? 2 : 0;
        }
        else
        {
            // this is part of a contiguous match, so give it a slight bonus, but only if it would not be a preferred gap location
            score += isGapLocation ? 0 : 1;
        }

        if (wordPos + 1 == wordLen)
        {
            // pretend there is a gap after the last character in the word to normalize things
            score -= isGapLocation ? 3 : 5;
        }

        return score;
    }
}

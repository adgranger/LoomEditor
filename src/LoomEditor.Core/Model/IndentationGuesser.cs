// Ported from microsoft/vscode: src/vs/editor/common/model/indentationGuesser.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model;

/// <summary>The result of <see cref="IndentationGuesser.GuessIndentation"/> (Monaco: <c>IGuessedIndentation</c>).</summary>
/// <param name="InsertSpaces">Is indentation based on spaces?</param>
/// <param name="TabSize">If indentation is based on spaces, the number of spaces that make an indent.</param>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal readonly record struct GuessedIndentation(bool InsertSpaces, int TabSize);

/// <summary>Guesses whether a text is indented with tabs or spaces, and with which size (Monaco: <c>guessIndentation</c>).</summary>
/// <remarks>Threading: stateless; safe as long as the buffer is not edited during the call.</remarks>
internal static class IndentationGuesser
{
    // Prefer even guesses for `tabSize`, limit to [2, 8].
    private static readonly int[] AllowedTabSizeGuesses = [2, 4, 6, 8, 3, 5, 7];

    // max(AllowedTabSizeGuesses) = 8
    private const int MaxAllowedTabSizeGuess = 8;

    private sealed class SpacesDiffResult
    {
        public int SpacesDiff;
        public bool LooksLikeAlignment;
    }

    /// <summary>Compute the diff in spaces between two line's indentation.</summary>
    private static void SpacesDiff(string a, int aLength, string b, int bLength, SpacesDiffResult result)
    {
        result.SpacesDiff = 0;
        result.LooksLikeAlignment = false;

        // This can go both ways (e.g.):
        //  - a: "\t"
        //  - b: "\t    "
        //  => This should count 1 tab and 4 spaces

        int i;

        for (i = 0; i < aLength && i < bLength; i++)
        {
            if (a[i] != b[i])
            {
                break;
            }
        }

        int aSpacesCnt = 0, aTabsCount = 0;
        for (int j = i; j < aLength; j++)
        {
            if (a[j] == ' ')
            {
                aSpacesCnt++;
            }
            else
            {
                aTabsCount++;
            }
        }

        int bSpacesCnt = 0, bTabsCount = 0;
        for (int j = i; j < bLength; j++)
        {
            if (b[j] == ' ')
            {
                bSpacesCnt++;
            }
            else
            {
                bTabsCount++;
            }
        }

        if (aSpacesCnt > 0 && aTabsCount > 0)
        {
            return;
        }

        if (bSpacesCnt > 0 && bTabsCount > 0)
        {
            return;
        }

        int tabsDiff = Math.Abs(aTabsCount - bTabsCount);
        int spacesDiff = Math.Abs(aSpacesCnt - bSpacesCnt);

        if (tabsDiff == 0)
        {
            // check if the indentation difference might be caused by alignment reasons
            // sometime folks like to align their code, but this should not be used as a hint
            result.SpacesDiff = spacesDiff;

            if (spacesDiff > 0 && 0 <= bSpacesCnt - 1 && bSpacesCnt - 1 < a.Length && bSpacesCnt < b.Length)
            {
                if (b[bSpacesCnt] != ' ' && a[bSpacesCnt - 1] == ' ')
                {
                    if (a[^1] == ',')
                    {
                        // This looks like an alignment desire: e.g.
                        // const a = b + c,
                        //       d = b - c;
                        result.LooksLikeAlignment = true;
                    }
                }
            }

            return;
        }

        if (spacesDiff % tabsDiff == 0)
        {
            result.SpacesDiff = spacesDiff / tabsDiff;
        }
    }

    /// <summary>Guesses the indentation of <paramref name="source"/> from its first 10k lines.</summary>
    /// <returns>The guess; the defaults when the text gives no clue.</returns>
    public static GuessedIndentation GuessIndentation(ITextBuffer source, int defaultTabSize, bool defaultInsertSpaces)
    {
        // Look at most at the first 10k lines
        int linesCount = Math.Min(source.GetLineCount(), 10000);

        int linesIndentedWithTabsCount = 0;   // number of lines that contain at least one tab in indentation
        int linesIndentedWithSpacesCount = 0; // number of lines that contain only spaces in indentation

        string previousLineText = string.Empty; // content of latest line that contained non-whitespace chars
        int previousLineIndentation = 0;        // index at which latest line contained the first non-whitespace char

        int[] spacesDiffCount = new int[MaxAllowedTabSizeGuess + 1]; // `tabSize` scores
        var tmp = new SpacesDiffResult();

        for (int lineNumber = 1; lineNumber <= linesCount; lineNumber++)
        {
            int currentLineLength = source.GetLineLength(lineNumber);
            string currentLineText = source.GetLineContent(lineNumber);

            // Monaco reads long lines with getLineCharCode to avoid flattening V8 cons-strings; a .NET string is flat.
            bool currentLineHasContent = false; // does `currentLineText` contain non-whitespace chars
            int currentLineIndentation = 0;     // index at which `currentLineText` contains the first non-whitespace char
            int currentLineSpacesCount = 0;     // count of spaces found in `currentLineText` indentation
            int currentLineTabsCount = 0;       // count of tabs found in `currentLineText` indentation
            for (int j = 0; j < currentLineLength; j++)
            {
                char charCode = currentLineText[j];

                if (charCode == '\t')
                {
                    currentLineTabsCount++;
                }
                else if (charCode == ' ')
                {
                    currentLineSpacesCount++;
                }
                else
                {
                    // Hit non whitespace character on this line
                    currentLineHasContent = true;
                    currentLineIndentation = j;
                    break;
                }
            }

            // Ignore empty or only whitespace lines
            if (!currentLineHasContent)
            {
                continue;
            }

            if (currentLineTabsCount > 0)
            {
                linesIndentedWithTabsCount++;
            }
            else if (currentLineSpacesCount > 1)
            {
                linesIndentedWithSpacesCount++;
            }

            SpacesDiff(previousLineText, previousLineIndentation, currentLineText, currentLineIndentation, tmp);

            if (tmp.LooksLikeAlignment)
            {
                // if defaultInsertSpaces === true && the spaces count == tabSize, we may want to count it as valid indentation
                //
                // - item1
                //   - item2
                //
                // otherwise skip this line entirely
                //
                // const a = 1,
                //       b = 2;
                if (!(defaultInsertSpaces && defaultTabSize == tmp.SpacesDiff))
                {
                    continue;
                }
            }

            int currentSpacesDiff = tmp.SpacesDiff;
            if (currentSpacesDiff <= MaxAllowedTabSizeGuess)
            {
                spacesDiffCount[currentSpacesDiff]++;
            }

            previousLineText = currentLineText;
            previousLineIndentation = currentLineIndentation;
        }

        bool insertSpaces = defaultInsertSpaces;
        if (linesIndentedWithTabsCount != linesIndentedWithSpacesCount)
        {
            insertSpaces = linesIndentedWithTabsCount < linesIndentedWithSpacesCount;
        }

        int tabSize = defaultTabSize;

        // Guess tabSize only if inserting spaces...
        if (insertSpaces)
        {
            int tabSizeScore = 0;
            foreach (int possibleTabSize in AllowedTabSizeGuesses)
            {
                int possibleTabSizeScore = spacesDiffCount[possibleTabSize];
                if (possibleTabSizeScore > tabSizeScore)
                {
                    tabSizeScore = possibleTabSizeScore;
                    tabSize = possibleTabSize;
                }
            }

            // Let a tabSize of 2 win over 4 only if it has at least 2/3 of the occurrences of 4
            // This helps detect 2-space indentation in cases like YAML files where there might be
            // some 4-space diffs from deeper nesting, while still preferring 4 when it's clearly predominant
            // Monaco: spacesDiffCount[2] >= spacesDiffCount[4] * 2 / 3 (float); multiplied out to stay in integers.
            if (tabSize == 4 && spacesDiffCount[4] > 0 && spacesDiffCount[2] > 0 && spacesDiffCount[2] * 3 >= spacesDiffCount[4] * 2)
            {
                tabSize = 2;
            }
        }

        return new GuessedIndentation(insertSpaces, tabSize);
    }
}

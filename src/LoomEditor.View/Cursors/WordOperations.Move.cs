// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorWordOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

internal static partial class WordOperations
{
    public static Position MoveWordLeft(WordCharacterClassifier wordSeparators, ICursorSimpleModel model, Position position, WordNavigationType wordNavigationType, bool hasMulticursor)
    {
        int lineNumber = position.LineNumber;
        int column = position.Column;

        if (column == 1 && lineNumber > 1)
        {
            lineNumber--;
            column = model.GetLineMaxColumn(lineNumber);
        }

        FindWordResult? prevWordOnLine = FindPreviousWordOnLine(wordSeparators, model, new Position(lineNumber, column));

        if (wordNavigationType == WordNavigationType.WordStart)
        {
            return new Position(lineNumber, prevWordOnLine is { } start ? start.Start + 1 : 1);
        }

        if (wordNavigationType == WordNavigationType.WordStartFast)
        {
            if (!hasMulticursor // avoid having multiple cursors stop at different locations when doing word start
                && prevWordOnLine is { } single
                && single.WordType == WordType.Separator
                && single.End - single.Start == 1
                && single.NextCharClass == WordCharacterClass.Regular)
            {
                // Skip over a word made up of one single separator and followed by a regular character
                prevWordOnLine = FindPreviousWordOnLine(wordSeparators, model, new Position(lineNumber, single.Start + 1));
            }

            return new Position(lineNumber, prevWordOnLine is { } fast ? fast.Start + 1 : 1);
        }

        if (wordNavigationType == WordNavigationType.WordAccessibility)
        {
            while (prevWordOnLine is { } separatorWord && separatorWord.WordType == WordType.Separator)
            {
                // Skip over words made up of only separators
                prevWordOnLine = FindPreviousWordOnLine(wordSeparators, model, new Position(lineNumber, separatorWord.Start + 1));
            }

            return new Position(lineNumber, prevWordOnLine is { } accessible ? accessible.Start + 1 : 1);
        }

        // We are stopping at the ending of words
        if (prevWordOnLine is { } ending && column <= ending.End + 1)
        {
            prevWordOnLine = FindPreviousWordOnLine(wordSeparators, model, new Position(lineNumber, ending.Start + 1));
        }

        return new Position(lineNumber, prevWordOnLine is { } result ? result.End + 1 : 1);
    }

    public static Position MoveWordPartLeft(ICursorSimpleModel model, Position position)
    {
        int lineNumber = position.LineNumber;
        int maxColumn = model.GetLineMaxColumn(lineNumber);

        if (position.Column == 1)
        {
            return lineNumber > 1 ? new Position(lineNumber - 1, model.GetLineMaxColumn(lineNumber - 1)) : position;
        }

        string lineContent = model.GetLineContent(lineNumber);
        for (int column = position.Column - 1; column > 1; column--)
        {
            int left = lineContent[column - 2];
            int right = lineContent[column - 1];

            if (left == '_' && right != '_')
            {
                // snake_case_variables
                return new Position(lineNumber, column);
            }

            if (left == '-' && right != '-')
            {
                // kebab-case-variables
                return new Position(lineNumber, column);
            }

            if ((Strings.IsLowerAsciiLetter(left) || Strings.IsAsciiDigit(left)) && Strings.IsUpperAsciiLetter(right))
            {
                // camelCaseVariables
                return new Position(lineNumber, column);
            }

            if (Strings.IsUpperAsciiLetter(left) && Strings.IsUpperAsciiLetter(right))
            {
                // thisIsACamelCaseWithOneLetterWords
                if (column + 1 < maxColumn)
                {
                    int rightRight = lineContent[column];
                    if (Strings.IsLowerAsciiLetter(rightRight) || Strings.IsAsciiDigit(rightRight))
                    {
                        return new Position(lineNumber, column);
                    }
                }
            }
        }

        return new Position(lineNumber, 1);
    }

    public static Position MoveWordRight(WordCharacterClassifier wordSeparators, ICursorSimpleModel model, Position position, WordNavigationType wordNavigationType)
    {
        int lineNumber = position.LineNumber;

        // Monaco: 'column' can be 0 with WordAccessibility, which a Position cannot hold: it is a plain int here.
        int column = position.Column;

        bool movedDown = false;
        if (column == model.GetLineMaxColumn(lineNumber) && lineNumber < model.GetLineCount())
        {
            movedDown = true;
            lineNumber++;
            column = 1;
        }

        FindWordResult? nextWordOnLine = FindNextWordOnLine(wordSeparators, model, new Position(lineNumber, column));

        if (wordNavigationType == WordNavigationType.WordEnd)
        {
            if (nextWordOnLine is { } separatorWord && separatorWord.WordType == WordType.Separator)
            {
                if (separatorWord.End - separatorWord.Start == 1 && separatorWord.NextCharClass == WordCharacterClass.Regular)
                {
                    // Skip over a word made up of one single separator and followed by a regular character
                    nextWordOnLine = FindNextWordOnLine(wordSeparators, model, new Position(lineNumber, separatorWord.End + 1));
                }
            }

            column = nextWordOnLine is { } end ? end.End + 1 : model.GetLineMaxColumn(lineNumber);
        }
        else if (wordNavigationType == WordNavigationType.WordAccessibility)
        {
            if (movedDown)
            {
                // If we move to the next line, pretend that the cursor is right before the first character.
                // This is needed when the first word starts right at the first character - and in order not to miss it,
                // we need to start before.
                column = 0;
            }

            while (nextWordOnLine is { } skipped && (skipped.WordType == WordType.Separator || skipped.Start + 1 <= column))
            {
                // Skip over a word made up of one single separator
                // Also skip over word if it begins before current cursor position to ascertain we're moving forward at least 1 character.
                nextWordOnLine = FindNextWordOnLine(wordSeparators, model, new Position(lineNumber, skipped.End + 1));
            }

            column = nextWordOnLine is { } accessible ? accessible.Start + 1 : model.GetLineMaxColumn(lineNumber);
        }
        else
        {
            if (nextWordOnLine is { } current && !movedDown && column >= current.Start + 1)
            {
                nextWordOnLine = FindNextWordOnLine(wordSeparators, model, new Position(lineNumber, current.End + 1));
            }

            column = nextWordOnLine is { } start ? start.Start + 1 : model.GetLineMaxColumn(lineNumber);
        }

        return new Position(lineNumber, column);
    }

    public static Position MoveWordPartRight(ICursorSimpleModel model, Position position)
    {
        int lineNumber = position.LineNumber;
        int maxColumn = model.GetLineMaxColumn(lineNumber);

        if (position.Column == maxColumn)
        {
            return lineNumber < model.GetLineCount() ? new Position(lineNumber + 1, 1) : position;
        }

        string lineContent = model.GetLineContent(lineNumber);
        for (int column = position.Column + 1; column < maxColumn; column++)
        {
            int left = lineContent[column - 2];
            int right = lineContent[column - 1];

            if (left != '_' && right == '_')
            {
                // snake_case_variables
                return new Position(lineNumber, column);
            }

            if (left != '-' && right == '-')
            {
                // kebab-case-variables
                return new Position(lineNumber, column);
            }

            if ((Strings.IsLowerAsciiLetter(left) || Strings.IsAsciiDigit(left)) && Strings.IsUpperAsciiLetter(right))
            {
                // camelCaseVariables
                return new Position(lineNumber, column);
            }

            if (Strings.IsUpperAsciiLetter(left) && Strings.IsUpperAsciiLetter(right))
            {
                // thisIsACamelCaseWithOneLetterWords
                if (column + 1 < maxColumn)
                {
                    int rightRight = lineContent[column];
                    if (Strings.IsLowerAsciiLetter(rightRight) || Strings.IsAsciiDigit(rightRight))
                    {
                        return new Position(lineNumber, column);
                    }
                }
            }
        }

        return new Position(lineNumber, maxColumn);
    }
}

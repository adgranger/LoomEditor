// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorWordOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

/// <summary>Word scanning and word moves.</summary>
/// <remarks>
/// <para>
/// The <c>Intl</c> paths are ported, but they never find a word in phase 3 (<see cref="WordCharacterClassifier"/> has no
/// <c>Intl.Segmenter</c>). Threading: pure functions; thread-safe as long as the model is not modified concurrently.
/// </para>
/// </remarks>
internal static partial class WordOperations
{
    private static FindWordResult CreateWord(WordType wordType, WordCharacterClass nextCharClass, int start, int end) =>
        new(start, end, wordType, nextCharClass);

    private static FindWordResult CreateIntlWord(IntlWordSegment intlWord, WordCharacterClass nextCharClass) =>
        new(intlWord.Index, intlWord.Index + intlWord.Segment.Length, WordType.Regular, nextCharClass);

    // Monaco's 'lineContent.charCodeAt(i)' is NaN out of range; the callers only read inside the string.
    private static FindWordResult? FindPreviousWordOnLine(WordCharacterClassifier wordSeparators, ICursorSimpleModel model, Position position)
    {
        string lineContent = model.GetLineContent(position.LineNumber);
        return DoFindPreviousWordOnLine(lineContent, wordSeparators, position);
    }

    private static FindWordResult? DoFindPreviousWordOnLine(string lineContent, WordCharacterClassifier wordSeparators, Position position)
    {
        WordType wordType = WordType.None;

        IntlWordSegment? previousIntlWord = wordSeparators.FindPrevIntlWordBeforeOrAtOffset(lineContent, position.Column - 2);

        for (int chIndex = position.Column - 2; chIndex >= 0; chIndex--)
        {
            int chCode = lineContent[chIndex];
            WordCharacterClass chClass = wordSeparators.Get(chCode);

            if (previousIntlWord is { } intlWord && chIndex == intlWord.Index)
            {
                return CreateIntlWord(intlWord, chClass);
            }

            if (chClass == WordCharacterClass.Regular)
            {
                if (wordType == WordType.Separator)
                {
                    return CreateWord(wordType, chClass, chIndex + 1, FindEndOfWord(lineContent, wordSeparators, wordType, chIndex + 1));
                }

                wordType = WordType.Regular;
            }
            else if (chClass == WordCharacterClass.WordSeparator)
            {
                if (wordType == WordType.Regular)
                {
                    return CreateWord(wordType, chClass, chIndex + 1, FindEndOfWord(lineContent, wordSeparators, wordType, chIndex + 1));
                }

                wordType = WordType.Separator;
            }
            else if (chClass == WordCharacterClass.Whitespace)
            {
                if (wordType != WordType.None)
                {
                    return CreateWord(wordType, chClass, chIndex + 1, FindEndOfWord(lineContent, wordSeparators, wordType, chIndex + 1));
                }
            }
        }

        if (wordType != WordType.None)
        {
            return CreateWord(wordType, WordCharacterClass.Whitespace, 0, FindEndOfWord(lineContent, wordSeparators, wordType, 0));
        }

        return null;
    }

    private static int FindEndOfWord(string lineContent, WordCharacterClassifier wordSeparators, WordType wordType, int startIndex)
    {
        IntlWordSegment? nextIntlWord = wordSeparators.FindNextIntlWordAtOrAfterOffset(lineContent, startIndex);

        int len = lineContent.Length;
        for (int chIndex = startIndex; chIndex < len; chIndex++)
        {
            int chCode = lineContent[chIndex];
            WordCharacterClass chClass = wordSeparators.Get(chCode);

            if (nextIntlWord is { } intlWord && chIndex == intlWord.Index + intlWord.Segment.Length)
            {
                return chIndex;
            }

            if (chClass == WordCharacterClass.Whitespace)
            {
                return chIndex;
            }

            if (wordType == WordType.Regular && chClass == WordCharacterClass.WordSeparator)
            {
                return chIndex;
            }

            if (wordType == WordType.Separator && chClass == WordCharacterClass.Regular)
            {
                return chIndex;
            }
        }

        return len;
    }

    private static FindWordResult? FindNextWordOnLine(WordCharacterClassifier wordSeparators, ICursorSimpleModel model, Position position)
    {
        string lineContent = model.GetLineContent(position.LineNumber);
        return DoFindNextWordOnLine(lineContent, wordSeparators, position);
    }

    private static FindWordResult? DoFindNextWordOnLine(string lineContent, WordCharacterClassifier wordSeparators, Position position)
    {
        WordType wordType = WordType.None;
        int len = lineContent.Length;

        IntlWordSegment? nextIntlWord = wordSeparators.FindNextIntlWordAtOrAfterOffset(lineContent, position.Column - 1);

        for (int chIndex = position.Column - 1; chIndex < len; chIndex++)
        {
            int chCode = lineContent[chIndex];
            WordCharacterClass chClass = wordSeparators.Get(chCode);

            if (nextIntlWord is { } intlWord && chIndex == intlWord.Index)
            {
                return CreateIntlWord(intlWord, chClass);
            }

            if (chClass == WordCharacterClass.Regular)
            {
                if (wordType == WordType.Separator)
                {
                    return CreateWord(wordType, chClass, FindStartOfWord(lineContent, wordSeparators, wordType, chIndex - 1), chIndex);
                }

                wordType = WordType.Regular;
            }
            else if (chClass == WordCharacterClass.WordSeparator)
            {
                if (wordType == WordType.Regular)
                {
                    return CreateWord(wordType, chClass, FindStartOfWord(lineContent, wordSeparators, wordType, chIndex - 1), chIndex);
                }

                wordType = WordType.Separator;
            }
            else if (chClass == WordCharacterClass.Whitespace)
            {
                if (wordType != WordType.None)
                {
                    return CreateWord(wordType, chClass, FindStartOfWord(lineContent, wordSeparators, wordType, chIndex - 1), chIndex);
                }
            }
        }

        if (wordType != WordType.None)
        {
            return CreateWord(wordType, WordCharacterClass.Whitespace, FindStartOfWord(lineContent, wordSeparators, wordType, len - 1), len);
        }

        return null;
    }

    private static int FindStartOfWord(string lineContent, WordCharacterClassifier wordSeparators, WordType wordType, int startIndex)
    {
        IntlWordSegment? previousIntlWord = wordSeparators.FindPrevIntlWordBeforeOrAtOffset(lineContent, startIndex);

        for (int chIndex = startIndex; chIndex >= 0; chIndex--)
        {
            int chCode = lineContent[chIndex];
            WordCharacterClass chClass = wordSeparators.Get(chCode);

            if (previousIntlWord is { } intlWord && chIndex == intlWord.Index)
            {
                return chIndex;
            }

            if (chClass == WordCharacterClass.Whitespace)
            {
                return chIndex + 1;
            }

            if (wordType == WordType.Regular && chClass == WordCharacterClass.WordSeparator)
            {
                return chIndex + 1;
            }

            if (wordType == WordType.Separator && chClass == WordCharacterClass.Regular)
            {
                return chIndex + 1;
            }
        }

        return 0;
    }
}

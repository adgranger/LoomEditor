// Ported from microsoft/vscode: src/vs/editor/common/model/textModelSearch.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Model.Search;

/// <summary>Walks the matches of one string with JavaScript <c>lastIndex</c> semantics (Monaco: <c>Searcher</c>).</summary>
/// <remarks>
/// Behavior difference: the JavaScript <c>regex.lastIndex</c> is a private index here, and matching starts at it with
/// <c>Regex.Match(text, startat)</c> so that lookbehinds and <c>^</c> still see the text before it.
/// Threading: not thread-safe (it holds the search position).
/// </remarks>
internal sealed class Searcher(WordCharacterClassifier? wordSeparators, Regex searchRegex)
{
    private int _lastIndex;
    private int _prevMatchStartIndex = -1;
    private int _prevMatchLength;

    /// <summary>The word separator classifier (Monaco: <c>_wordSeparators</c>).</summary>
    public WordCharacterClassifier? WordSeparators { get; } = wordSeparators;

    /// <summary>Restarts the search at <paramref name="lastIndex"/>.</summary>
    public void Reset(int lastIndex)
    {
        _lastIndex = lastIndex;
        _prevMatchStartIndex = -1;
        _prevMatchLength = 0;
    }

    /// <summary>The next valid match in <paramref name="text"/>, or <c>null</c>.</summary>
    public Match? Next(string text)
    {
        var textLength = text.Length;

        while (true)
        {
            if (_prevMatchStartIndex + _prevMatchLength == textLength)
            {
                // Reached the end of the line
                return null;
            }

            var m = Exec(text);
            if (m is null)
            {
                return null;
            }

            var matchStartIndex = m.Index;
            var matchLength = m.Length;
            if (matchStartIndex == _prevMatchStartIndex && matchLength == _prevMatchLength)
            {
                if (matchLength == 0)
                {
                    // an empty match does not advance the index: advance by two on a surrogate pair, by one otherwise
                    _lastIndex += Strings.GetNextCodePoint(text, textLength, _lastIndex) > 0xFFFF ? 2 : 1;
                    continue;
                }

                // Exit early if the regex matches the same range twice
                return null;
            }

            _prevMatchStartIndex = matchStartIndex;
            _prevMatchLength = matchLength;

            if (WordSeparators is null || IsValidMatch(WordSeparators, text, textLength, matchStartIndex, matchLength))
            {
                return m;
            }
        }
    }

    private Match? Exec(string text)
    {
        if (_lastIndex > text.Length)
        {
            _lastIndex = 0;
            return null;
        }

        var m = searchRegex.Match(text, _lastIndex);
        if (!m.Success)
        {
            _lastIndex = 0;
            return null;
        }

        _lastIndex = m.Index + m.Length;
        return m;
    }

    /// <summary>Whether the match starts and ends on a word boundary (Monaco: <c>isValidMatch</c>).</summary>
    public static bool IsValidMatch(WordCharacterClassifier wordSeparators, string text, int textLength, int matchStartIndex, int matchLength)
        => LeftIsWordBoundary(wordSeparators, text, matchStartIndex, matchLength)
            && RightIsWordBoundary(wordSeparators, text, textLength, matchStartIndex, matchLength);

    private static bool LeftIsWordBoundary(WordCharacterClassifier wordSeparators, string text, int matchStartIndex, int matchLength)
    {
        if (matchStartIndex == 0)
        {
            return true;
        }

        var charBefore = text[matchStartIndex - 1];
        if (wordSeparators.Get(charBefore) != WordCharacterClass.Regular || charBefore is '\r' or '\n')
        {
            return true;
        }

        return matchLength > 0 && wordSeparators.Get(text[matchStartIndex]) != WordCharacterClass.Regular;
    }

    private static bool RightIsWordBoundary(WordCharacterClassifier wordSeparators, string text, int textLength, int matchStartIndex, int matchLength)
    {
        if (matchStartIndex + matchLength == textLength)
        {
            return true;
        }

        var charAfter = text[matchStartIndex + matchLength];
        if (wordSeparators.Get(charAfter) != WordCharacterClass.Regular || charAfter is '\r' or '\n')
        {
            return true;
        }

        return matchLength > 0 && wordSeparators.Get(text[matchStartIndex + matchLength - 1]) != WordCharacterClass.Regular;
    }
}

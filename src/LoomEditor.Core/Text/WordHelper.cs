// Ported from microsoft/vscode: src/vs/editor/common/core/wordHelper.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace LoomEditor.Core.Text;

/// <summary>Word inside a model (Monaco: <c>IWordAtPosition</c>).</summary>
/// <param name="Word">The word.</param>
/// <param name="StartColumn">The column where the word starts.</param>
/// <param name="EndColumn">The column where the word ends.</param>
internal readonly record struct WordAtPosition(string Word, int StartColumn, int EndColumn);

/// <summary>Constants and word regular expressions of Monaco's <c>wordHelper.ts</c>.</summary>
/// <remarks>Threading: immutable; thread-safe (.NET regexes are stateless).</remarks>
internal static class WordHelper
{
    /// <summary>The default value of the <c>wordSeparators</c> editor option (Monaco: <c>USUAL_WORD_SEPARATORS</c>).</summary>
    public const string UsualWordSeparators = "`~!@#$%^&*()-=+[{]}\\|;:'\",.<>/?";

    /// <summary>
    /// Catches numbers (including floating numbers) in the first group, and alphanum in the second
    /// (Monaco: <c>DEFAULT_WORD_REGEXP</c>).
    /// </summary>
    public static readonly Regex DefaultWordRegExp = CreateWordRegExp();

    /// <summary>
    /// Creates a word definition regular expression based on default word separators. Optionally provide allowed
    /// separators that should be included in words (Monaco: <c>createWordRegExp</c>).
    /// </summary>
    /// <remarks>
    /// <c>\d</c> is written <c>[0-9]</c> and <c>\w</c> <c>[a-zA-Z0-9_]</c>: .NET's classes are Unicode, JavaScript's are ASCII.
    /// There is no <c>g</c> flag in .NET.
    /// </remarks>
    public static Regex CreateWordRegExp(string allowInWords = "")
    {
        var source = new StringBuilder(@"(-?[0-9]*\.[0-9][a-zA-Z0-9_]*)|([^");
        foreach (char sep in UsualWordSeparators)
        {
            if (allowInWords.Contains(sep, StringComparison.Ordinal))
            {
                continue;
            }

            source.Append('\\').Append(sep);
        }

        source.Append(@"\s]+)");
        return new Regex(source.ToString(), RegexOptions.CultureInvariant);
    }

    /// <summary>
    /// Returns <paramref name="wordDefinition"/>, or <see cref="DefaultWordRegExp"/> when it is <see langword="null"/>
    /// (Monaco: <c>ensureValidWordDefinition</c>; a .NET regex has no <c>global</c> flag and no <c>lastIndex</c> to reset).
    /// </summary>
    public static Regex EnsureValidWordDefinition(Regex? wordDefinition) => wordDefinition ?? DefaultWordRegExp;

    /// <summary>
    /// The word of <paramref name="wordDefinition"/> that encloses <paramref name="column"/>, searched in windows that
    /// grow to the left of the column within a time budget (Monaco: <c>getWordAtText</c>).
    /// </summary>
    /// <param name="column">The 1-based column, in the coordinates of the whole line.</param>
    /// <param name="wordDefinition">The word regular expression.</param>
    /// <param name="text">The text to search: the line, or a part of it starting at <paramref name="textOffset"/>.</param>
    /// <param name="textOffset">The offset of <paramref name="text"/> in the line.</param>
    /// <param name="config">The limits of the search; <see langword="null"/> for <see cref="GetWordAtTextConfig.Default"/>.</param>
    /// <remarks>
    /// Differences from Monaco: <c>setDefaultGetWordAtTextConfig</c> (global mutable state) is not ported; after an
    /// empty match the search moves one character further, where Monaco would loop forever.
    /// </remarks>
    public static WordAtPosition? GetWordAtText(int column, Regex wordDefinition, string text, int textOffset, GetWordAtTextConfig? config = null)
    {
        wordDefinition = EnsureValidWordDefinition(wordDefinition);
        GetWordAtTextConfig resolvedConfig = config ?? GetWordAtTextConfig.Default;

        if (text.Length > resolvedConfig.MaxLen)
        {
            // don't throw strings that long at the regexp
            // but use a sub-string in which a word must occur
            int start = column - (resolvedConfig.MaxLen / 2);
            if (start < 0)
            {
                start = 0;
            }
            else
            {
                textOffset += start;
            }

            // JavaScript's substring(start, end) clamps end to the length
            text = text[start..Math.Min(text.Length, column + (resolvedConfig.MaxLen / 2))];
            return GetWordAtText(column, wordDefinition, text, textOffset, resolvedConfig);
        }

        long t1 = Stopwatch.GetTimestamp();
        int pos = column - 1 - textOffset;

        int prevRegexIndex = -1;
        Match? match = null;

        for (int i = 1; ; i++)
        {
            // check time budget
            if (Stopwatch.GetElapsedTime(t1).TotalMilliseconds >= resolvedConfig.TimeBudget)
            {
                break;
            }

            // reset the index at which the regexp should start matching, also know where it
            // should stop so that subsequent search don't repeat previous searches
            int regexIndex = pos - (resolvedConfig.WindowSize * i);
            Match? thisMatch = FindRegexMatchEnclosingPosition(wordDefinition, text, pos, prevRegexIndex, Math.Max(0, regexIndex));

            if (thisMatch is null && match is not null)
            {
                // stop: we have something
                break;
            }

            match = thisMatch;

            // stop: searched at start
            if (regexIndex <= 0)
            {
                break;
            }

            prevRegexIndex = regexIndex;
        }

        if (match is not null)
        {
            return new WordAtPosition(
                match.Value,
                textOffset + 1 + match.Index,
                textOffset + 1 + match.Index + match.Length);
        }

        return null;
    }

    // Monaco: _findRegexMatchEnclosingPosition; lastIndex is the regex's lastIndex, kept in a local.
    private static Match? FindRegexMatchEnclosingPosition(Regex wordDefinition, string text, int pos, int stopPos, int lastIndex)
    {
        // JavaScript's exec fails when lastIndex is past the end of the text
        while (lastIndex <= text.Length)
        {
            Match match = wordDefinition.Match(text, lastIndex);
            if (!match.Success)
            {
                return null;
            }

            int matchIndex = match.Index;
            lastIndex = match.Index + match.Length;
            if (matchIndex <= pos && lastIndex >= pos)
            {
                return match;
            }
            else if (stopPos > 0 && matchIndex > stopPos)
            {
                return null;
            }

            if (match.Length == 0)
            {
                // Monaco loops forever on an empty match (exec does not move lastIndex): move one character further.
                lastIndex++;
            }
        }

        return null;
    }
}

/// <summary>The limits of <see cref="WordHelper.GetWordAtText"/> (Monaco: <c>IGetWordAtTextConfig</c>).</summary>
/// <param name="MaxLen">Longer texts are cut to this length around the column.</param>
/// <param name="WindowSize">How far each search window grows to the left of the column.</param>
/// <param name="TimeBudget">The time budget of the search, in milliseconds.</param>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct GetWordAtTextConfig(int MaxLen, int WindowSize, int TimeBudget)
{
    /// <summary>Monaco's default configuration: <c>maxLen</c> 1000, <c>windowSize</c> 15, <c>timeBudget</c> 150.</summary>
    public static readonly GetWordAtTextConfig Default = new(1000, 15, 150);
}

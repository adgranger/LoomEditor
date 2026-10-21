// Ported from microsoft/vscode: src/vs/editor/common/model/textModelSearch.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Model.Search;

/// <summary>A search request (Monaco: <c>SearchParams</c>).</summary>
/// <remarks>
/// Behavior differences: the regex uses .NET syntax and options (see <see cref="Strings.CreateRegExp"/>), so
/// <c>\u{…}</c>, <c>\d</c>, <c>\w</c> and <c>.</c> behave as .NET does; case-insensitive matching uses invariant case folding.
/// Threading: immutable; thread-safe.
/// </remarks>
internal sealed class SearchParams(string searchString, bool isRegex, bool matchCase, string? wordSeparators)
{
    /// <summary>The maximum number of matches a search returns (Monaco: <c>LIMIT_FIND_COUNT</c>).</summary>
    public const int LimitFindCount = 999;

    /// <summary>The text (or pattern) to search for.</summary>
    public string SearchString { get; } = searchString;

    /// <summary>Whether <see cref="SearchString"/> is a regular expression.</summary>
    public bool IsRegex { get; } = isRegex;

    /// <summary>Whether the search is case sensitive.</summary>
    public bool MatchCase { get; } = matchCase;

    /// <summary>The word separators for whole-word matching, or <c>null</c>.</summary>
    public string? WordSeparators { get; } = wordSeparators;

    /// <summary>Parses the request; <c>null</c> when the search string is empty or not a valid pattern.</summary>
    public SearchData? ParseSearchRequest()
    {
        if (SearchString.Length == 0)
        {
            return null;
        }

        var multiline = IsRegex ? IsMultilineRegexSource(SearchString) : SearchString.Contains('\n');

        System.Text.RegularExpressions.Regex regex;
        try
        {
            regex = Strings.CreateRegExp(SearchString, IsRegex, MatchCase, multiline);
        }
        catch (ArgumentException)
        {
            return null;
        }

        var canUseSimpleSearch = !IsRegex && !multiline;
        if (canUseSimpleSearch && !string.Equals(SearchString.ToLowerInvariant(), SearchString.ToUpperInvariant(), StringComparison.Ordinal))
        {
            // casing might make a difference
            canUseSimpleSearch = MatchCase;
        }

        var wordClassifier = string.IsNullOrEmpty(WordSeparators)
            ? null
            : WordCharacterClassifierCache.Get(WordSeparators, []);
        return new SearchData(regex, wordClassifier, canUseSimpleSearch ? SearchString : null);
    }

    /// <summary>Whether a regex source can match across lines (Monaco: <c>isMultilineRegexSource</c>).</summary>
    public static bool IsMultilineRegexSource(string searchString)
    {
        for (var i = 0; i < searchString.Length; i++)
        {
            var ch = searchString[i];
            if (ch == '\n')
            {
                return true;
            }

            if (ch == '\\')
            {
                i++;
                if (i >= searchString.Length)
                {
                    break;
                }

                if (searchString[i] is 'n' or 'r' or 'W')
                {
                    return true;
                }
            }
        }

        return false;
    }
}

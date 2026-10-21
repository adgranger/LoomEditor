// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Search;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model;

public sealed partial class TextModel
{
    /// <summary>The largest "small" integer, a limit used by later features (Monaco: <c>Constants.MAX_SAFE_SMALL_INTEGER</c>).</summary>
    internal const int MaxSafeSmallInteger = 1 << 30;

    /// <summary>Finds the matches of a search in the whole model (Monaco: <c>findMatches</c> with a <c>boolean</c> scope).</summary>
    internal List<FindMatch> FindMatches(
        string searchString,
        bool searchOnlyEditableRange,
        bool isRegex,
        bool matchCase,
        string? wordSeparators,
        bool captureMatches,
        int limitResultCount = SearchParams.LimitFindCount)
        => FindMatchesCore(searchString, [FullModelRange], isRegex, matchCase, wordSeparators, captureMatches, limitResultCount);

    /// <summary>Finds the matches of a search inside some ranges, each searched once even if they overlap.</summary>
    internal List<FindMatch> FindMatches(
        string searchString,
        IReadOnlyList<Range> searchScopes,
        bool isRegex,
        bool matchCase,
        string? wordSeparators,
        bool captureMatches,
        int limitResultCount = SearchParams.LimitFindCount)
        => FindMatchesCore(
            searchString,
            searchScopes.Count == 0 ? [FullModelRange] : searchScopes.Select(ValidateRange).ToList(),
            isRegex,
            matchCase,
            wordSeparators,
            captureMatches,
            limitResultCount);

    private List<FindMatch> FindMatchesCore(
        string searchString,
        List<Range> searchRanges,
        bool isRegex,
        bool matchCase,
        string? wordSeparators,
        bool captureMatches,
        int limitResultCount)
    {
        // A stable sort, like Monaco's.
        var sorted = searchRanges.OrderBy(r => r.StartLineNumber).ThenBy(r => r.StartColumn).ToList();

        List<Range> uniqueSearchRanges = [];
        Range prev = sorted[0];
        for (var i = 1; i < sorted.Count; i++)
        {
            if (prev.Intersects(sorted[i]))
            {
                prev = prev.Plus(sorted[i]);
            }
            else
            {
                uniqueSearchRanges.Add(prev);
                prev = sorted[i];
            }
        }

        uniqueSearchRanges.Add(prev);

        var searchParams = new SearchParams(searchString, isRegex, matchCase, wordSeparators);
        List<FindMatch> result = [];
        if (!isRegex && !searchString.Contains('\n'))
        {
            // not regex, not multi line
            SearchData? searchData = searchParams.ParseSearchRequest();
            if (searchData is null)
            {
                return [];
            }

            foreach (Range searchRange in uniqueSearchRanges)
            {
                result.AddRange(_buffer.FindMatchesLineByLine(searchRange, searchData, captureMatches, limitResultCount));
            }

            return result;
        }

        foreach (Range searchRange in uniqueSearchRanges)
        {
            result.AddRange(TextModelSearch.FindMatches(this, searchParams, searchRange, captureMatches, limitResultCount));
        }

        return result;
    }

    /// <summary>The first match at or after a position, wrapping around the model (Monaco: <c>findNextMatch</c>).</summary>
    /// <remarks>
    /// Monaco also calls <c>TextModelSearch.findNextMatch</c> in the plain-text branch and discards the result; it has no
    /// side effect, so that call is not ported.
    /// </remarks>
    internal FindMatch? FindNextMatch(string searchString, Position rawSearchStart, bool isRegex, bool matchCase, string? wordSeparators, bool captureMatches)
    {
        Position searchStart = ValidatePosition(rawSearchStart);
        var searchParams = new SearchParams(searchString, isRegex, matchCase, wordSeparators);

        if (!isRegex && !searchString.Contains('\n'))
        {
            SearchData? searchData = searchParams.ParseSearchRequest();
            if (searchData is null)
            {
                return null;
            }

            int lineCount = LineCount;
            var searchRange = new Range(searchStart.LineNumber, searchStart.Column, lineCount, GetLineMaxColumn(lineCount));
            List<FindMatch> ret = _buffer.FindMatchesLineByLine(searchRange, searchData, captureMatches, 1);
            if (ret.Count > 0)
            {
                return ret[0];
            }

            searchRange = new Range(1, 1, searchStart.LineNumber, GetLineMaxColumn(searchStart.LineNumber));
            ret = _buffer.FindMatchesLineByLine(searchRange, searchData, captureMatches, 1);
            return ret.Count > 0 ? ret[0] : null;
        }

        return TextModelSearch.FindNextMatch(this, searchParams, searchStart, captureMatches);
    }

    /// <summary>The last match ending at or before a position, wrapping around the model (Monaco: <c>findPreviousMatch</c>).</summary>
    internal FindMatch? FindPreviousMatch(string searchString, Position rawSearchStart, bool isRegex, bool matchCase, string? wordSeparators, bool captureMatches)
    {
        Position searchStart = ValidatePosition(rawSearchStart);
        return TextModelSearch.FindPreviousMatch(this, new SearchParams(searchString, isRegex, matchCase, wordSeparators), searchStart, captureMatches);
    }
}

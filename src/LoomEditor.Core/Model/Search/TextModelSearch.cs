// Ported from microsoft/vscode: src/vs/editor/common/model/textModelSearch.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.Search;

/// <summary>Finds matches in a <see cref="TextModel"/>, line by line or, for multiline regexes, over the joined text.</summary>
/// <remarks>
/// Behavior differences: see <see cref="SearchParams"/> (.NET regex syntax and options).
/// Threading: static methods; call on the thread that owns the model.
/// </remarks>
internal static partial class TextModelSearch
{
    /// <summary>All matches inside <paramref name="searchRange"/>, at most <paramref name="limitResultCount"/>.</summary>
    public static List<FindMatch> FindMatches(TextModel model, SearchParams searchParams, Range searchRange, bool captureMatches, int limitResultCount)
    {
        var searchData = searchParams.ParseSearchRequest();
        if (searchData is null)
        {
            return [];
        }

        if (searchData.IsMultiline)
        {
            return DoFindMatchesMultiline(model, searchRange, new Searcher(searchData.WordSeparators, searchData.Regex), captureMatches, limitResultCount);
        }

        return DoFindMatchesLineByLine(model, searchRange, searchData, captureMatches, limitResultCount);
    }

    /// <summary>
    /// Multiline search always executes on the lines concatenated with \n.
    /// We must therefore compensate for the count of \n in case the model is CRLF.
    /// </summary>
    private static Range GetMultilineMatchRange(TextModel model, int deltaOffset, LineFeedCounter? lfCounter, int matchIndex, string match0)
    {
        int startOffset;
        var lineFeedCountBeforeMatch = 0;
        if (lfCounter is not null)
        {
            lineFeedCountBeforeMatch = lfCounter.FindLineFeedCountBeforeOffset(matchIndex);
            startOffset = deltaOffset + matchIndex + lineFeedCountBeforeMatch; // add as many \r as there were \n
        }
        else
        {
            startOffset = deltaOffset + matchIndex;
        }

        int endOffset;
        if (lfCounter is not null)
        {
            var lineFeedCountBeforeEndOfMatch = lfCounter.FindLineFeedCountBeforeOffset(matchIndex + match0.Length);
            var lineFeedCountInMatch = lineFeedCountBeforeEndOfMatch - lineFeedCountBeforeMatch;
            endOffset = startOffset + match0.Length + lineFeedCountInMatch; // add as many \r as there were \n
        }
        else
        {
            endOffset = startOffset + match0.Length;
        }

        return new Range(model.GetPositionAt(startOffset), model.GetPositionAt(endOffset));
    }

    private static List<FindMatch> DoFindMatchesMultiline(TextModel model, Range searchRange, Searcher searcher, bool captureMatches, int limitResultCount)
    {
        var deltaOffset = model.GetOffsetAt(searchRange.Start);
        // We always execute multiline search over the lines joined with \n
        // This makes it that \n will match the EOL for both CRLF and LF models
        // We compensate for offset errors in `GetMultilineMatchRange`
        var text = model.GetValueInRange(searchRange, EndOfLinePreference.LF);
        var lfCounter = model.Eol == "\r\n" ? new LineFeedCounter(text) : null;

        List<FindMatch> result = [];
        Match? m;
        searcher.Reset(0);
        while ((m = searcher.Next(text)) is not null)
        {
            result.Add(FindMatch.Create(GetMultilineMatchRange(model, deltaOffset, lfCounter, m.Index, m.Value), m, captureMatches));
            if (result.Count >= limitResultCount)
            {
                return result;
            }
        }

        return result;
    }

    private static List<FindMatch> DoFindMatchesLineByLine(TextModel model, Range searchRange, SearchData searchData, bool captureMatches, int limitResultCount)
    {
        List<FindMatch> result = [];

        // Early case for a search range that starts & stops on the same line number
        if (searchRange.StartLineNumber == searchRange.EndLineNumber)
        {
            var single = model.GetLineContent(searchRange.StartLineNumber).Substring(searchRange.StartColumn - 1, searchRange.EndColumn - searchRange.StartColumn);
            FindMatchesInLine(searchData, single, searchRange.StartLineNumber, searchRange.StartColumn - 1, result, captureMatches, limitResultCount);
            return result;
        }

        // Collect results from first line
        var first = model.GetLineContent(searchRange.StartLineNumber)[(searchRange.StartColumn - 1)..];
        FindMatchesInLine(searchData, first, searchRange.StartLineNumber, searchRange.StartColumn - 1, result, captureMatches, limitResultCount);

        // Collect results from middle lines
        for (var lineNumber = searchRange.StartLineNumber + 1; lineNumber < searchRange.EndLineNumber && result.Count < limitResultCount; lineNumber++)
        {
            FindMatchesInLine(searchData, model.GetLineContent(lineNumber), lineNumber, 0, result, captureMatches, limitResultCount);
        }

        // Collect results from last line
        if (result.Count < limitResultCount)
        {
            var last = model.GetLineContent(searchRange.EndLineNumber).Substring(0, searchRange.EndColumn - 1);
            FindMatchesInLine(searchData, last, searchRange.EndLineNumber, 0, result, captureMatches, limitResultCount);
        }

        return result;
    }

    private static void FindMatchesInLine(SearchData searchData, string text, int lineNumber, int deltaOffset, List<FindMatch> result, bool captureMatches, int limitResultCount)
    {
        var wordSeparators = searchData.WordSeparators;
        if (!captureMatches && !string.IsNullOrEmpty(searchData.SimpleSearch))
        {
            var searchString = searchData.SimpleSearch;
            var searchStringLen = searchString.Length;
            var textLength = text.Length;

            var lastMatchIndex = -searchStringLen;
            while ((lastMatchIndex = text.IndexOf(searchString, lastMatchIndex + searchStringLen, StringComparison.Ordinal)) != -1)
            {
                if (wordSeparators is null || Searcher.IsValidMatch(wordSeparators, text, textLength, lastMatchIndex, searchStringLen))
                {
                    result.Add(new FindMatch(new Range(lineNumber, lastMatchIndex + 1 + deltaOffset, lineNumber, lastMatchIndex + 1 + searchStringLen + deltaOffset), null));
                    if (result.Count >= limitResultCount)
                    {
                        return;
                    }
                }
            }

            return;
        }

        var searcher = new Searcher(searchData.WordSeparators, searchData.Regex);
        Match? m;
        // Reset regex to search from the beginning
        searcher.Reset(0);
        do
        {
            m = searcher.Next(text);
            if (m is not null)
            {
                result.Add(FindMatch.Create(new Range(lineNumber, m.Index + 1 + deltaOffset, lineNumber, m.Index + 1 + m.Length + deltaOffset), m, captureMatches));
                if (result.Count >= limitResultCount)
                {
                    return;
                }
            }
        }
        while (m is not null);
    }
}

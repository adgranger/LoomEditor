// Ported from microsoft/vscode: src/vs/editor/common/model/textModelSearch.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.Search;

internal static partial class TextModelSearch
{
    /// <summary>The first match at or after <paramref name="searchStart"/>, wrapping around the model.</summary>
    public static FindMatch? FindNextMatch(TextModel model, SearchParams searchParams, Position searchStart, bool captureMatches)
    {
        var searchData = searchParams.ParseSearchRequest();
        if (searchData is null)
        {
            return null;
        }

        var searcher = new Searcher(searchData.WordSeparators, searchData.Regex);
        return searchData.IsMultiline
            ? DoFindNextMatchMultiline(model, searchStart, searcher, captureMatches)
            : DoFindNextMatchLineByLine(model, searchStart, searcher, captureMatches);
    }

    private static FindMatch? DoFindNextMatchMultiline(TextModel model, Position searchStart, Searcher searcher, bool captureMatches)
    {
        var searchTextStart = new Position(searchStart.LineNumber, 1);
        var deltaOffset = model.GetOffsetAt(searchTextStart);
        var lineCount = model.LineCount;
        // We always execute multiline search over the lines joined with \n
        // This makes it that \n will match the EOL for both CRLF and LF models
        // We compensate for offset errors in `GetMultilineMatchRange`
        var text = model.GetValueInRange(new Range(searchTextStart.LineNumber, searchTextStart.Column, lineCount, model.GetLineMaxColumn(lineCount)), EndOfLinePreference.LF);
        var lfCounter = model.Eol == "\r\n" ? new LineFeedCounter(text) : null;
        searcher.Reset(searchStart.Column - 1);
        var m = searcher.Next(text);
        if (m is not null)
        {
            return FindMatch.Create(GetMultilineMatchRange(model, deltaOffset, lfCounter, m.Index, m.Value), m, captureMatches);
        }

        if (searchStart.LineNumber != 1 || searchStart.Column != 1)
        {
            // Try again from the top
            return DoFindNextMatchMultiline(model, new Position(1, 1), searcher, captureMatches);
        }

        return null;
    }

    private static FindMatch? DoFindNextMatchLineByLine(TextModel model, Position searchStart, Searcher searcher, bool captureMatches)
    {
        var lineCount = model.LineCount;
        var startLineNumber = searchStart.LineNumber;

        // Look in first line
        var r = FindFirstMatchInLine(searcher, model.GetLineContent(startLineNumber), startLineNumber, searchStart.Column, captureMatches);
        if (r is not null)
        {
            return r;
        }

        for (var i = 1; i <= lineCount; i++)
        {
            var lineIndex = (startLineNumber + i - 1) % lineCount;
            r = FindFirstMatchInLine(searcher, model.GetLineContent(lineIndex + 1), lineIndex + 1, 1, captureMatches);
            if (r is not null)
            {
                return r;
            }
        }

        return null;
    }

    private static FindMatch? FindFirstMatchInLine(Searcher searcher, string text, int lineNumber, int fromColumn, bool captureMatches)
    {
        // Set regex to search from column
        searcher.Reset(fromColumn - 1);
        var m = searcher.Next(text);
        return m is null
            ? null
            : FindMatch.Create(new Range(lineNumber, m.Index + 1, lineNumber, m.Index + 1 + m.Length), m, captureMatches);
    }

    /// <summary>The last match ending at or before <paramref name="searchStart"/>, wrapping around the model.</summary>
    public static FindMatch? FindPreviousMatch(TextModel model, SearchParams searchParams, Position searchStart, bool captureMatches)
    {
        var searchData = searchParams.ParseSearchRequest();
        if (searchData is null)
        {
            return null;
        }

        var searcher = new Searcher(searchData.WordSeparators, searchData.Regex);
        return searchData.IsMultiline
            ? DoFindPreviousMatchMultiline(model, searchStart, searcher, captureMatches)
            : DoFindPreviousMatchLineByLine(model, searchStart, searcher, captureMatches);
    }

    private static FindMatch? DoFindPreviousMatchMultiline(TextModel model, Position searchStart, Searcher searcher, bool captureMatches)
    {
        var matches = DoFindMatchesMultiline(model, new Range(1, 1, searchStart.LineNumber, searchStart.Column), searcher, captureMatches, 10 * SearchParams.LimitFindCount);
        if (matches.Count > 0)
        {
            return matches[^1];
        }

        var lineCount = model.LineCount;
        if (searchStart.LineNumber != lineCount || searchStart.Column != model.GetLineMaxColumn(lineCount))
        {
            // Try again with all content
            return DoFindPreviousMatchMultiline(model, new Position(lineCount, model.GetLineMaxColumn(lineCount)), searcher, captureMatches);
        }

        return null;
    }

    private static FindMatch? DoFindPreviousMatchLineByLine(TextModel model, Position searchStart, Searcher searcher, bool captureMatches)
    {
        var lineCount = model.LineCount;
        var startLineNumber = searchStart.LineNumber;

        // Look in first line
        var text = model.GetLineContent(startLineNumber).Substring(0, searchStart.Column - 1);
        var r = FindLastMatchInLine(searcher, text, startLineNumber, captureMatches);
        if (r is not null)
        {
            return r;
        }

        for (var i = 1; i <= lineCount; i++)
        {
            var lineIndex = (lineCount + startLineNumber - i - 1) % lineCount;
            r = FindLastMatchInLine(searcher, model.GetLineContent(lineIndex + 1), lineIndex + 1, captureMatches);
            if (r is not null)
            {
                return r;
            }
        }

        return null;
    }

    private static FindMatch? FindLastMatchInLine(Searcher searcher, string text, int lineNumber, bool captureMatches)
    {
        FindMatch? bestResult = null;
        Match? m;
        searcher.Reset(0);
        while ((m = searcher.Next(text)) is not null)
        {
            bestResult = FindMatch.Create(new Range(lineNumber, m.Index + 1, lineNumber, m.Index + 1 + m.Length), m, captureMatches);
        }

        return bestResult;
    }
}

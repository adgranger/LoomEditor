// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Model.Search;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.PieceTree;

// Searching a plain-text query directly in the pieces.
internal sealed partial class PieceTreeBase
{
    /// <summary>
    /// Searches the part of the piece of <paramref name="node"/> between two cursors (Monaco: <c>findMatchesInNode</c>).
    /// </summary>
    /// <remarks>
    /// Behavior difference: Monaco without word separators searches the whole buffer from <paramref name="startCursor"/>
    /// and can report a match that crosses the end of the range; here the text is always cut to the range first (Monaco's
    /// word-separator branch), so a match never crosses it. Only plain-text queries reach this path, so anchors and
    /// lookarounds never see the cut.
    /// </remarks>
    internal int FindMatchesInNode(
        TreeNode node,
        Searcher searcher,
        int startLineNumber,
        int startColumn,
        BufferCursor startCursor,
        BufferCursor endCursor,
        bool captureMatches,
        int limitResultCount,
        List<FindMatch> result)
    {
        StringBuffer buffer = _buffers[node.Piece.BufferIndex];
        int startOffsetInBuffer = OffsetInBuffer(node.Piece.BufferIndex, node.Piece.Start);
        int start = OffsetInBuffer(node.Piece.BufferIndex, startCursor);
        int end = OffsetInBuffer(node.Piece.BufferIndex, endCursor);

        string searchText = buffer.Substring(start, end - start);
        searcher.Reset(0);

        Match? m;
        do
        {
            m = searcher.Next(searchText);
            if (m is null)
            {
                continue;
            }

            int matchOffset = m.Index + start;
            if (matchOffset >= end)
            {
                return result.Count;
            }

            BufferCursor ret = PositionInBuffer(node, matchOffset - startOffsetInBuffer);
            int lineFeedCnt = GetLineFeedCount(node.Piece.BufferIndex, startCursor, ret);
            int retStartColumn = ret.Line == startCursor.Line ? ret.Column - startCursor.Column + startColumn : ret.Column + 1;
            int retEndColumn = retStartColumn + m.Length;
            result.Add(FindMatch.Create(
                new Range(startLineNumber + lineFeedCnt, retStartColumn, startLineNumber + lineFeedCnt, retEndColumn),
                m,
                captureMatches));

            if (matchOffset + m.Length >= end || result.Count >= limitResultCount)
            {
                return result.Count;
            }
        }
        while (m is not null);

        return result.Count;
    }

    /// <summary>Finds the matches of a plain-text query inside a range (Monaco: <c>findMatchesLineByLine</c>).</summary>
    public List<FindMatch> FindMatchesLineByLine(Range searchRange, SearchData searchData, bool captureMatches, int limitResultCount)
    {
        List<FindMatch> result = [];
        var searcher = new Searcher(searchData.WordSeparators, searchData.Regex);

        if (TryNodeAt2(searchRange.StartLineNumber, searchRange.StartColumn) is not { } startPosition
            || TryNodeAt2(searchRange.EndLineNumber, searchRange.EndColumn) is not { } endPosition)
        {
            return [];
        }

        BufferCursor start = PositionInBuffer(startPosition.Node, startPosition.Remainder);
        BufferCursor end = PositionInBuffer(endPosition.Node, endPosition.Remainder);

        if (ReferenceEquals(startPosition.Node, endPosition.Node))
        {
            FindMatchesInNode(startPosition.Node, searcher, searchRange.StartLineNumber, searchRange.StartColumn, start, end, captureMatches, limitResultCount, result);
            return result;
        }

        int startLineNumber = searchRange.StartLineNumber;
        TreeNode currentNode = startPosition.Node;
        while (!ReferenceEquals(currentNode, endPosition.Node))
        {
            int lineBreakCnt = GetLineFeedCount(currentNode.Piece.BufferIndex, start, currentNode.Piece.End);

            if (lineBreakCnt >= 1)
            {
                // last line break position
                List<int> lineStarts = _buffers[currentNode.Piece.BufferIndex].LineStarts;
                int startOffsetInBuffer = OffsetInBuffer(currentNode.Piece.BufferIndex, currentNode.Piece.Start);
                int nextLineStartOffset = lineStarts[start.Line + lineBreakCnt];
                int firstColumn = startLineNumber == searchRange.StartLineNumber ? searchRange.StartColumn : 1;
                FindMatchesInNode(
                    currentNode,
                    searcher,
                    startLineNumber,
                    firstColumn,
                    start,
                    PositionInBuffer(currentNode, nextLineStartOffset - startOffsetInBuffer),
                    captureMatches,
                    limitResultCount,
                    result);

                if (result.Count >= limitResultCount)
                {
                    return result;
                }

                startLineNumber += lineBreakCnt;
            }

            int startColumn = startLineNumber == searchRange.StartLineNumber ? searchRange.StartColumn - 1 : 0;
            // search for the remaining content
            if (startLineNumber == searchRange.EndLineNumber)
            {
                string text = GetLineContent(startLineNumber).Substring(startColumn, searchRange.EndColumn - 1 - startColumn);
                FindMatchesInLine(searchData, searcher, text, searchRange.EndLineNumber, startColumn, result, captureMatches, limitResultCount);
                return result;
            }

            FindMatchesInLine(searchData, searcher, GetLineContent(startLineNumber)[startColumn..], startLineNumber, startColumn, result, captureMatches, limitResultCount);

            if (result.Count >= limitResultCount)
            {
                return result;
            }

            startLineNumber++;
            NodePosition next = NodeAt2(startLineNumber, 1);
            currentNode = next.Node;
            start = PositionInBuffer(next.Node, next.Remainder);
        }

        if (startLineNumber == searchRange.EndLineNumber)
        {
            int startColumn = startLineNumber == searchRange.StartLineNumber ? searchRange.StartColumn - 1 : 0;
            string text = GetLineContent(startLineNumber).Substring(startColumn, searchRange.EndColumn - 1 - startColumn);
            FindMatchesInLine(searchData, searcher, text, searchRange.EndLineNumber, startColumn, result, captureMatches, limitResultCount);
            return result;
        }

        int lastColumn = startLineNumber == searchRange.StartLineNumber ? searchRange.StartColumn : 1;
        FindMatchesInNode(endPosition.Node, searcher, startLineNumber, lastColumn, start, end, captureMatches, limitResultCount, result);
        return result;
    }

    private static void FindMatchesInLine(
        SearchData searchData,
        Searcher searcher,
        string text,
        int lineNumber,
        int deltaOffset,
        List<FindMatch> result,
        bool captureMatches,
        int limitResultCount)
    {
        WordCharacterClassifier? wordSeparators = searchData.WordSeparators;
        if (!captureMatches && !string.IsNullOrEmpty(searchData.SimpleSearch))
        {
            string searchString = searchData.SimpleSearch;
            int searchStringLen = searchString.Length;
            int textLength = text.Length;

            int lastMatchIndex = -searchStringLen;
            while ((lastMatchIndex = text.IndexOf(searchString, lastMatchIndex + searchStringLen, StringComparison.Ordinal)) != -1)
            {
                if (wordSeparators is null || Searcher.IsValidMatch(wordSeparators, text, textLength, lastMatchIndex, searchStringLen))
                {
                    result.Add(new FindMatch(
                        new Range(lineNumber, lastMatchIndex + 1 + deltaOffset, lineNumber, lastMatchIndex + 1 + searchStringLen + deltaOffset),
                        null));
                    if (result.Count >= limitResultCount)
                    {
                        return;
                    }
                }
            }

            return;
        }

        Match? m;
        // Reset regex to search from the beginning
        searcher.Reset(0);
        do
        {
            m = searcher.Next(text);
            if (m is not null)
            {
                result.Add(FindMatch.Create(
                    new Range(lineNumber, m.Index + 1 + deltaOffset, lineNumber, m.Index + 1 + m.Length + deltaOffset),
                    m,
                    captureMatches));
                if (result.Count >= limitResultCount)
                {
                    return;
                }
            }
        }
        while (m is not null);
    }
}

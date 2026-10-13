// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.PieceTree;

// Offsets <-> positions, ranges and characters.
internal sealed partial class PieceTreeBase
{
    /// <summary>Offset of a (line, column) position (Monaco: <c>getOffsetAt</c>).</summary>
    /// <param name="lineNumber">1-based line number.</param>
    /// <param name="column">1-based column.</param>
    public int GetOffsetAt(int lineNumber, int column)
    {
        int leftLen = 0; // inorder

        TreeNode x = _tree.Root;
        TreeNode sentinel = _tree.Sentinel;

        while (!ReferenceEquals(x, sentinel))
        {
            if (!ReferenceEquals(x.Left, sentinel) && x.LineFeedCountLeft + 1 >= lineNumber)
            {
                x = x.Left;
            }
            else if (x.LineFeedCountLeft + x.Piece.LineFeedCount + 1 >= lineNumber)
            {
                leftLen += x.SizeLeft;
                // lineNumber >= 2
                int accumulatedValInCurrentIndex = GetAccumulatedValue(x, lineNumber - x.LineFeedCountLeft - 2);
                return leftLen + accumulatedValInCurrentIndex + column - 1;
            }
            else
            {
                lineNumber -= x.LineFeedCountLeft + x.Piece.LineFeedCount;
                leftLen += x.SizeLeft + x.Piece.Length;
                x = x.Right;
            }
        }

        return leftLen;
    }

    /// <summary>Position of an offset; a negative offset is treated as 0 (Monaco: <c>getPositionAt</c>).</summary>
    public Position GetPositionAt(int offset)
    {
        offset = Math.Max(0, offset);

        TreeNode x = _tree.Root;
        TreeNode sentinel = _tree.Sentinel;
        int lfCnt = 0;
        int originalOffset = offset;

        while (!ReferenceEquals(x, sentinel))
        {
            if (x.SizeLeft != 0 && x.SizeLeft >= offset)
            {
                x = x.Left;
            }
            else if (x.SizeLeft + x.Piece.Length >= offset)
            {
                (int index, int remainder) = GetIndexOf(x, offset - x.SizeLeft);

                lfCnt += x.LineFeedCountLeft + index;

                if (index == 0)
                {
                    int lineStartOffset = GetOffsetAt(lfCnt + 1, 1);
                    int column = originalOffset - lineStartOffset;
                    return new Position(lfCnt + 1, column + 1);
                }

                return new Position(lfCnt + 1, remainder + 1);
            }
            else
            {
                offset -= x.SizeLeft + x.Piece.Length;
                lfCnt += x.LineFeedCountLeft + x.Piece.LineFeedCount;

                if (ReferenceEquals(x.Right, sentinel))
                {
                    // last node
                    int lineStartOffset = GetOffsetAt(lfCnt + 1, 1);
                    int column = originalOffset - offset - lineStartOffset;
                    return new Position(lfCnt + 1, column + 1);
                }

                x = x.Right;
            }
        }

        return new Position(1, 1);
    }

    /// <summary>Text of a range (Monaco: <c>getValueInRange</c>).</summary>
    /// <param name="range">The range; it must lie inside the document.</param>
    /// <param name="eol">When not null or empty, every line break in the result is replaced by this string.</param>
    /// <exception cref="ArgumentOutOfRangeException">The range is outside the document (Monaco fails with a <c>TypeError</c>).</exception>
    public string GetValueInRange(Range range, string? eol = null)
    {
        if (range.IsEmpty)
        {
            return "";
        }

        NodePosition startPosition = NodeAt2(range.StartLineNumber, range.StartColumn);
        NodePosition endPosition = NodeAt2(range.EndLineNumber, range.EndColumn);

        string value = GetValueInRange2(startPosition, endPosition);
        if (!string.IsNullOrEmpty(eol))
        {
            if (eol == _eol && _eolNormalized)
            {
                return value;
            }

            return ReplaceLineBreaks(value, eol);
        }

        return value;
    }

    /// <summary>Text between two node positions (Monaco: <c>getValueInRange2</c>).</summary>
    internal string GetValueInRange2(NodePosition startPosition, NodePosition endPosition)
    {
        if (ReferenceEquals(startPosition.Node, endPosition.Node))
        {
            TreeNode node = startPosition.Node;
            StringBuffer buffer = _buffers[node.Piece.BufferIndex];
            int startOffset = OffsetInBuffer(node.Piece.BufferIndex, node.Piece.Start);
            return Slice(buffer, startOffset + startPosition.Remainder, startOffset + endPosition.Remainder);
        }

        TreeNode x = startPosition.Node;
        StringBuffer firstBuffer = _buffers[x.Piece.BufferIndex];
        int firstStartOffset = OffsetInBuffer(x.Piece.BufferIndex, x.Piece.Start);
        var ret = new StringBuilder(
            Slice(firstBuffer, firstStartOffset + startPosition.Remainder, firstStartOffset + x.Piece.Length));

        x = _tree.Next(x);
        while (!ReferenceEquals(x, _tree.Sentinel))
        {
            StringBuffer buffer = _buffers[x.Piece.BufferIndex];
            int startOffset = OffsetInBuffer(x.Piece.BufferIndex, x.Piece.Start);

            if (ReferenceEquals(x, endPosition.Node))
            {
                ret.Append(Slice(buffer, startOffset, startOffset + endPosition.Remainder));
                break;
            }

            ret.Append(buffer.Substring(startOffset, x.Piece.Length));
            x = _tree.Next(x);
        }

        return ret.ToString();
    }

    /// <summary>Character at a position inside the tree (Monaco: <c>_getCharCode</c>).</summary>
    /// <remarks>Difference from Monaco: returns <c>'\0'</c> when there is no character (end of the document).</remarks>
    private char GetCharCode(NodePosition nodePos)
    {
        if (nodePos.Remainder == nodePos.Node.Piece.Length)
        {
            // the char we want to fetch is at the head of next node.
            TreeNode matchingNode = _tree.Next(nodePos.Node);
            if (ReferenceEquals(matchingNode, _tree.Sentinel))
            {
                return '\0';
            }

            StringBuffer buffer = _buffers[matchingNode.Piece.BufferIndex];
            int startOffset = OffsetInBuffer(matchingNode.Piece.BufferIndex, matchingNode.Piece.Start);
            return buffer[startOffset];
        }
        else
        {
            StringBuffer buffer = _buffers[nodePos.Node.Piece.BufferIndex];
            int startOffset = OffsetInBuffer(nodePos.Node.Piece.BufferIndex, nodePos.Node.Piece.Start);
            return buffer[startOffset + nodePos.Remainder];
        }
    }

    /// <summary>Character at 0-based <paramref name="index"/> of a line (Monaco: <c>getLineCharCode</c>).</summary>
    /// <param name="lineNumber">1-based line number.</param>
    /// <param name="index">0-based index in the line.</param>
    public char GetLineCharCode(int lineNumber, int index)
    {
        NodePosition nodePos = NodeAt2(lineNumber, index + 1);
        return GetCharCode(nodePos);
    }

    /// <summary>Length of a line without its line break (Monaco: <c>getLineLength</c>).</summary>
    /// <param name="lineNumber">1-based line number.</param>
    public int GetLineLength(int lineNumber)
    {
        if (lineNumber == GetLineCount())
        {
            int startOffset = GetOffsetAt(lineNumber, 1);
            return GetLength() - startOffset;
        }

        return GetOffsetAt(lineNumber + 1, 1) - GetOffsetAt(lineNumber, 1) - _eolLength;
    }

    /// <summary>Character at a document offset (Monaco: <c>getCharCode</c>).</summary>
    /// <remarks>Difference from Monaco: returns <c>'\0'</c> at the end of the document.</remarks>
    public char GetCharCode(int offset) => GetCharCode(NodeAt(offset));

    /// <summary>The rest of the piece that contains <paramref name="offset"/> (Monaco: <c>getNearestChunk</c>).</summary>
    public string GetNearestChunk(int offset)
    {
        NodePosition nodePos = NodeAt(offset);
        if (nodePos.Remainder == nodePos.Node.Piece.Length)
        {
            // the offset is at the head of next node.
            TreeNode matchingNode = _tree.Next(nodePos.Node);
            if (ReferenceEquals(matchingNode, _tree.Sentinel))
            {
                return "";
            }

            StringBuffer buffer = _buffers[matchingNode.Piece.BufferIndex];
            int startOffset = OffsetInBuffer(matchingNode.Piece.BufferIndex, matchingNode.Piece.Start);
            return buffer.Substring(startOffset, matchingNode.Piece.Length);
        }
        else
        {
            StringBuffer buffer = _buffers[nodePos.Node.Piece.BufferIndex];
            int startOffset = OffsetInBuffer(nodePos.Node.Piece.BufferIndex, nodePos.Node.Piece.Start);
            int targetOffset = startOffset + nodePos.Remainder;
            int targetEnd = startOffset + nodePos.Node.Piece.Length;
            return Slice(buffer, targetOffset, targetEnd);
        }
    }

    /// <summary>Buffer cursor at <paramref name="remainder"/> characters into the piece of <paramref name="node"/>.</summary>
    /// <remarks>Difference from Monaco: no optional <c>ret</c> parameter; the cursor is always returned.</remarks>
    internal BufferCursor PositionInBuffer(TreeNode node, int remainder)
    {
        Piece piece = node.Piece;
        List<int> lineStarts = _buffers[piece.BufferIndex].LineStarts;

        int startOffset = lineStarts[piece.Start.Line] + piece.Start.Column;
        int offset = startOffset + remainder;

        // binary search offset between startOffset and endOffset
        int low = piece.Start.Line;
        int high = piece.End.Line;

        int mid = 0;
        int midStart = 0;

        while (low <= high)
        {
            mid = low + ((high - low) / 2);
            midStart = lineStarts[mid];

            if (mid == high)
            {
                break;
            }

            int midStop = lineStarts[mid + 1];

            if (offset < midStart)
            {
                high = mid - 1;
            }
            else if (offset >= midStop)
            {
                low = mid + 1;
            }
            else
            {
                break;
            }
        }

        return new BufferCursor(mid, offset - midStart);
    }

    internal int GetLineFeedCount(int bufferIndex, BufferCursor start, BufferCursor end)
    {
        // we don't need to worry about start: abc\r|\n, or abc|\r, or abc|\n, or abc|\r\n doesn't change the fact that, there is one line break after start.
        // now let's take care of end: abc\r|\n, if end is in between \r and \n, we need to add line feed count by 1
        if (end.Column == 0)
        {
            return end.Line - start.Line;
        }

        StringBuffer buffer = _buffers[bufferIndex];
        List<int> lineStarts = buffer.LineStarts;
        if (end.Line == lineStarts.Count - 1)
        {
            // it means, there is no \n after end, otherwise, there will be one more lineStart.
            return end.Line - start.Line;
        }

        int nextLineStartOffset = lineStarts[end.Line + 1];
        int endOffset = lineStarts[end.Line] + end.Column;
        if (nextLineStartOffset > endOffset + 1)
        {
            // there are more than 1 character after end, which means it can't be \n
            return end.Line - start.Line;
        }

        // endOffset + 1 === nextLineStartOffset
        // character at endOffset is \n, so we check the character before first
        // if character at endOffset is \r, end.column is 0 and we can't get here.
        int previousCharOffset = endOffset - 1; // end.column > 0 so it's okay.
        return buffer[previousCharOffset] == '\r'
            ? end.Line - start.Line + 1
            : end.Line - start.Line;
    }

    private (int Index, int Remainder) GetIndexOf(TreeNode node, int accumulatedValue)
    {
        Piece piece = node.Piece;
        BufferCursor pos = PositionInBuffer(node, accumulatedValue);
        int lineCnt = pos.Line - piece.Start.Line;

        if (OffsetInBuffer(piece.BufferIndex, piece.End) - OffsetInBuffer(piece.BufferIndex, piece.Start) == accumulatedValue)
        {
            // we are checking the end of this node, so a CRLF check is necessary.
            int realLineCnt = GetLineFeedCount(piece.BufferIndex, piece.Start, pos);
            if (realLineCnt != lineCnt)
            {
                // aha yes, CRLF
                return (realLineCnt, 0);
            }
        }

        return (lineCnt, pos.Column);
    }

    /// <summary>The node containing a document offset (Monaco: <c>nodeAt</c>).</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="offset"/> is outside the document (Monaco returns <c>null</c> and fails later).
    /// </exception>
    internal NodePosition NodeAt(int offset)
    {
        TreeNode x = _tree.Root;
        CacheEntry? cache = _searchCache.Get(offset);
        if (cache is not null)
        {
            return new NodePosition(cache.Node, offset - cache.NodeStartOffset, cache.NodeStartOffset);
        }

        int originalOffset = offset;
        int nodeStartOffset = 0;

        while (!ReferenceEquals(x, _tree.Sentinel))
        {
            if (x.SizeLeft > offset)
            {
                x = x.Left;
            }
            else if (x.SizeLeft + x.Piece.Length >= offset)
            {
                nodeStartOffset += x.SizeLeft;
                var ret = new NodePosition(x, offset - x.SizeLeft, nodeStartOffset);
                _searchCache.Set(new CacheEntry(ret.Node, ret.NodeStartOffset, null));
                return ret;
            }
            else
            {
                offset -= x.SizeLeft + x.Piece.Length;
                nodeStartOffset += x.SizeLeft + x.Piece.Length;
                x = x.Right;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(offset), originalOffset, "The offset is outside the document.");
    }

    /// <summary>The node containing a (line, column) position (Monaco: <c>nodeAt2</c>).</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The position is outside the document (Monaco returns <c>null</c> and fails later).
    /// </exception>
    internal NodePosition NodeAt2(int lineNumber, int column)
    {
        TreeNode x = _tree.Root;
        TreeNode sentinel = _tree.Sentinel;
        int originalLineNumber = lineNumber;
        int originalColumn = column;
        int nodeStartOffset = 0;

        while (!ReferenceEquals(x, sentinel))
        {
            if (!ReferenceEquals(x.Left, sentinel) && x.LineFeedCountLeft >= lineNumber - 1)
            {
                x = x.Left;
            }
            else if (x.LineFeedCountLeft + x.Piece.LineFeedCount > lineNumber - 1)
            {
                int prevAccumulatedValue = GetAccumulatedValue(x, lineNumber - x.LineFeedCountLeft - 2);
                int accumulatedValue = GetAccumulatedValue(x, lineNumber - x.LineFeedCountLeft - 1);
                nodeStartOffset += x.SizeLeft;

                return new NodePosition(
                    x,
                    Math.Min(prevAccumulatedValue + column - 1, accumulatedValue),
                    nodeStartOffset);
            }
            else if (x.LineFeedCountLeft + x.Piece.LineFeedCount == lineNumber - 1)
            {
                int prevAccumulatedValue = GetAccumulatedValue(x, lineNumber - x.LineFeedCountLeft - 2);
                if (prevAccumulatedValue + column - 1 <= x.Piece.Length)
                {
                    return new NodePosition(x, prevAccumulatedValue + column - 1, nodeStartOffset);
                }

                column -= x.Piece.Length - prevAccumulatedValue;
                break;
            }
            else
            {
                lineNumber -= x.LineFeedCountLeft + x.Piece.LineFeedCount;
                nodeStartOffset += x.SizeLeft + x.Piece.Length;
                x = x.Right;
            }
        }

        // search in order, to find the node contains position.column
        x = _tree.Next(x);
        while (!ReferenceEquals(x, sentinel))
        {
            if (x.Piece.LineFeedCount > 0)
            {
                int accumulatedValue = GetAccumulatedValue(x, 0);
                return new NodePosition(x, Math.Min(column - 1, accumulatedValue), OffsetOfNode(x));
            }

            if (x.Piece.Length >= column - 1)
            {
                return new NodePosition(x, column - 1, OffsetOfNode(x));
            }

            column -= x.Piece.Length;
            x = _tree.Next(x);
        }

        throw new ArgumentOutOfRangeException(
            nameof(lineNumber),
            $"The position ({originalLineNumber}, {originalColumn}) is outside the document.");
    }

    /// <summary>Character at <paramref name="offset"/> in the piece of <paramref name="node"/>, or -1 if the piece has no line feed (Monaco: <c>nodeCharCodeAt</c>).</summary>
    internal int NodeCharCodeAt(TreeNode node, int offset)
    {
        if (node.Piece.LineFeedCount < 1)
        {
            return -1;
        }

        StringBuffer buffer = _buffers[node.Piece.BufferIndex];
        int newOffset = OffsetInBuffer(node.Piece.BufferIndex, node.Piece.Start) + offset;
        return buffer[newOffset];
    }

    /// <summary>Document offset of the first character of <paramref name="node"/> (Monaco: <c>offsetOfNode</c>).</summary>
    internal int OffsetOfNode(TreeNode node)
    {
        if (ReferenceEquals(node, _tree.Sentinel))
        {
            return 0;
        }

        int pos = node.SizeLeft;
        while (!ReferenceEquals(node, _tree.Root))
        {
            if (ReferenceEquals(node.Parent.Right, node))
            {
                pos += node.Parent.SizeLeft + node.Parent.Piece.Length;
            }

            node = node.Parent;
        }

        return pos;
    }

    // Monaco: value.replace(/\r\n|\r|\n/g, eol)
    private static string ReplaceLineBreaks(string value, string eol)
    {
        var sb = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char ch = value[i];
            if (ch == '\r')
            {
                if (i + 1 < value.Length && value[i + 1] == '\n')
                {
                    i++;
                }

                sb.Append(eol);
            }
            else if (ch == '\n')
            {
                sb.Append(eol);
            }
            else
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }
}

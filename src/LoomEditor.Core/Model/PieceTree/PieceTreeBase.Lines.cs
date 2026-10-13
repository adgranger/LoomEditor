// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;

namespace LoomEditor.Core.Model.PieceTree;

// Reading lines and content.
internal sealed partial class PieceTreeBase
{
    /// <summary>All lines without their line breaks (Monaco: <c>getLinesContent</c>).</summary>
    public string[] GetLinesContent()
    {
        var lines = new List<string>();
        var currentLine = new StringBuilder();
        bool danglingCr = false;

        Iterate(_tree.Root, node =>
        {
            if (ReferenceEquals(node, _tree.Sentinel))
            {
                return true;
            }

            Piece piece = node.Piece;
            int pieceLength = piece.Length;
            if (pieceLength == 0)
            {
                return true;
            }

            StringBuffer buffer = _buffers[piece.BufferIndex];
            List<int> lineStarts = buffer.LineStarts;

            int pieceStartLine = piece.Start.Line;
            int pieceEndLine = piece.End.Line;
            int pieceStartOffset = lineStarts[pieceStartLine] + piece.Start.Column;

            if (danglingCr)
            {
                if (buffer[pieceStartOffset] == '\n')
                {
                    // pretend the \n was in the previous piece..
                    pieceStartOffset++;
                    pieceLength--;
                }

                lines.Add(currentLine.ToString());
                currentLine.Clear();
                danglingCr = false;
                if (pieceLength == 0)
                {
                    return true;
                }
            }

            if (pieceStartLine == pieceEndLine)
            {
                // this piece has no new lines
                if (!_eolNormalized && buffer[pieceStartOffset + pieceLength - 1] == '\r')
                {
                    danglingCr = true;
                    currentLine.Append(buffer.Substring(pieceStartOffset, pieceLength - 1));
                }
                else
                {
                    currentLine.Append(buffer.Substring(pieceStartOffset, pieceLength));
                }

                return true;
            }

            // add the text before the first line start in this piece
            currentLine.Append(
                _eolNormalized
                    ? Slice(buffer, pieceStartOffset, Math.Max(pieceStartOffset, lineStarts[pieceStartLine + 1] - _eolLength))
                    : TrimTrailingEol(Slice(buffer, pieceStartOffset, lineStarts[pieceStartLine + 1])));
            lines.Add(currentLine.ToString());

            for (int line = pieceStartLine + 1; line < pieceEndLine; line++)
            {
                lines.Add(
                    _eolNormalized
                        ? Slice(buffer, lineStarts[line], lineStarts[line + 1] - _eolLength)
                        : TrimTrailingEol(Slice(buffer, lineStarts[line], lineStarts[line + 1])));
            }

            if (!_eolNormalized && buffer[lineStarts[pieceEndLine] + piece.End.Column - 1] == '\r')
            {
                danglingCr = true;
                if (piece.End.Column == 0)
                {
                    // The last line ended with a \r, let's undo the push, it will be pushed by next iteration
                    // (currentLine still holds that line, like in Monaco)
                    lines.RemoveAt(lines.Count - 1);
                }
                else
                {
                    currentLine.Clear();
                    currentLine.Append(buffer.Substring(lineStarts[pieceEndLine], piece.End.Column - 1));
                }
            }
            else
            {
                currentLine.Clear();
                currentLine.Append(buffer.Substring(lineStarts[pieceEndLine], piece.End.Column));
            }

            return true;
        });

        if (danglingCr)
        {
            lines.Add(currentLine.ToString());
            currentLine.Clear();
        }

        lines.Add(currentLine.ToString());
        return [.. lines];
    }

    /// <summary>Content of a line without its line break (Monaco: <c>getLineContent</c>).</summary>
    /// <param name="lineNumber">1-based line number.</param>
    public string GetLineContent(int lineNumber)
    {
        if (_lastVisitedLine.LineNumber == lineNumber)
        {
            return _lastVisitedLine.Value;
        }

        string value;
        if (lineNumber == _lineCnt)
        {
            value = GetLineRawContent(lineNumber);
        }
        else if (_eolNormalized)
        {
            value = GetLineRawContent(lineNumber, _eolLength);
        }
        else
        {
            value = TrimTrailingEol(GetLineRawContent(lineNumber));
        }

        _lastVisitedLine = (lineNumber, value);
        return value;
    }

    /// <summary>The whole text (Monaco: <c>getLinesRawContent</c>).</summary>
    public string GetLinesRawContent() => GetContentOfSubTree(_tree.Root);

    /// <summary>Content of a line including its line break, minus <paramref name="endOffset"/> trailing characters.</summary>
    /// <param name="lineNumber">1-based line number.</param>
    /// <param name="endOffset">Number of trailing characters to cut when the line ends inside the tree.</param>
    public string GetLineRawContent(int lineNumber, int endOffset = 0)
    {
        TreeNode x = _tree.Root;
        TreeNode sentinel = _tree.Sentinel;

        string ret = "";
        CacheEntry? cache = _searchCache.GetByLine(lineNumber);
        if (cache is not null)
        {
            x = cache.Node;
            int nodeStartLineNumber = cache.NodeStartLineNumber!.Value;
            int prevAccumulatedValue = GetAccumulatedValue(x, lineNumber - nodeStartLineNumber - 1);
            StringBuffer buffer = _buffers[x.Piece.BufferIndex];
            int startOffset = OffsetInBuffer(x.Piece.BufferIndex, x.Piece.Start);
            if (nodeStartLineNumber + x.Piece.LineFeedCount == lineNumber)
            {
                ret = Slice(buffer, startOffset + prevAccumulatedValue, startOffset + x.Piece.Length);
            }
            else
            {
                int accumulatedValue = GetAccumulatedValue(x, lineNumber - nodeStartLineNumber);
                return Slice(buffer, startOffset + prevAccumulatedValue, startOffset + accumulatedValue - endOffset);
            }
        }
        else
        {
            int nodeStartOffset = 0;
            int originalLineNumber = lineNumber;
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
                    StringBuffer buffer = _buffers[x.Piece.BufferIndex];
                    int startOffset = OffsetInBuffer(x.Piece.BufferIndex, x.Piece.Start);
                    nodeStartOffset += x.SizeLeft;
                    _searchCache.Set(new CacheEntry(
                        x,
                        nodeStartOffset,
                        originalLineNumber - (lineNumber - 1 - x.LineFeedCountLeft)));

                    return Slice(buffer, startOffset + prevAccumulatedValue, startOffset + accumulatedValue - endOffset);
                }
                else if (x.LineFeedCountLeft + x.Piece.LineFeedCount == lineNumber - 1)
                {
                    int prevAccumulatedValue = GetAccumulatedValue(x, lineNumber - x.LineFeedCountLeft - 2);
                    StringBuffer buffer = _buffers[x.Piece.BufferIndex];
                    int startOffset = OffsetInBuffer(x.Piece.BufferIndex, x.Piece.Start);

                    ret = Slice(buffer, startOffset + prevAccumulatedValue, startOffset + x.Piece.Length);
                    break;
                }
                else
                {
                    lineNumber -= x.LineFeedCountLeft + x.Piece.LineFeedCount;
                    nodeStartOffset += x.SizeLeft + x.Piece.Length;
                    x = x.Right;
                }
            }
        }

        // search in order, to find the node contains end column
        var sb = new StringBuilder(ret);
        x = _tree.Next(x);
        while (!ReferenceEquals(x, sentinel))
        {
            StringBuffer buffer = _buffers[x.Piece.BufferIndex];
            int startOffset = OffsetInBuffer(x.Piece.BufferIndex, x.Piece.Start);

            if (x.Piece.LineFeedCount > 0)
            {
                int accumulatedValue = GetAccumulatedValue(x, 0);

                sb.Append(Slice(buffer, startOffset, startOffset + accumulatedValue - endOffset));
                return sb.ToString();
            }

            sb.Append(buffer.Substring(startOffset, x.Piece.Length));

            x = _tree.Next(x);
        }

        return sb.ToString();
    }

    /// <summary>Offset in its buffer of <paramref name="cursor"/> (Monaco: <c>offsetInBuffer</c>).</summary>
    private int OffsetInBuffer(int bufferIndex, BufferCursor cursor)
    {
        List<int> lineStarts = _buffers[bufferIndex].LineStarts;
        return lineStarts[cursor.Line] + cursor.Column;
    }

    private int GetAccumulatedValue(TreeNode node, int index)
    {
        if (index < 0)
        {
            return 0;
        }

        Piece piece = node.Piece;
        List<int> lineStarts = _buffers[piece.BufferIndex].LineStarts;
        int expectedLineStartIndex = piece.Start.Line + index + 1;
        if (expectedLineStartIndex > piece.End.Line)
        {
            return lineStarts[piece.End.Line] + piece.End.Column - lineStarts[piece.Start.Line] - piece.Start.Column;
        }

        return lineStarts[expectedLineStartIndex] - lineStarts[piece.Start.Line] - piece.Start.Column;
    }

    /// <summary>
    /// In-order walk calling <paramref name="callback"/> on every node, including the sentinel leaves (like Monaco);
    /// stops as soon as the callback returns <c>false</c>.
    /// </summary>
    internal bool Iterate(TreeNode node, Func<TreeNode, bool> callback)
    {
        if (ReferenceEquals(node, _tree.Sentinel))
        {
            return callback(_tree.Sentinel);
        }

        return Iterate(node.Left, callback) && callback(node) && Iterate(node.Right, callback);
    }

    private string GetNodeContent(TreeNode node)
    {
        return ReferenceEquals(node, _tree.Sentinel) ? "" : GetPieceContent(node.Piece);
    }

    /// <summary>The text a piece covers (Monaco: <c>getPieceContent</c>).</summary>
    internal string GetPieceContent(Piece piece)
    {
        StringBuffer buffer = _buffers[piece.BufferIndex];
        int startOffset = OffsetInBuffer(piece.BufferIndex, piece.Start);
        int endOffset = OffsetInBuffer(piece.BufferIndex, piece.End);
        return Slice(buffer, startOffset, endOffset);
    }

    private string GetContentOfSubTree(TreeNode node)
    {
        var sb = new StringBuilder();

        Iterate(node, n =>
        {
            sb.Append(GetNodeContent(n));
            return true;
        });

        return sb.ToString();
    }

    // JavaScript's substring(start, end) as a (start, end) slice; callers guarantee start <= end.
    private static string Slice(StringBuffer buffer, int start, int end) => buffer.Substring(start, end - start);

    // Monaco: .replace(/(\r\n|\r|\n)$/, '')
    private static string TrimTrailingEol(string text)
    {
        if (text.EndsWith("\r\n", StringComparison.Ordinal))
        {
            return text[..^2];
        }

        if (text.Length > 0 && text[^1] is '\r' or '\n')
        {
            return text[..^1];
        }

        return text;
    }
}

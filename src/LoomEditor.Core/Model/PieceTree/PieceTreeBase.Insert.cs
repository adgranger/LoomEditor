// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.PieceTree;

// Insert.
internal sealed partial class PieceTreeBase
{
    /// <summary>Inserts <paramref name="value"/> at <paramref name="offset"/> (Monaco: <c>insert</c>).</summary>
    /// <param name="offset">Document offset, between 0 and <see cref="GetLength"/>.</param>
    /// <param name="value">The text to insert.</param>
    /// <param name="eolNormalized">True when every line break in <paramref name="value"/> equals the document's EOL.</param>
    public void Insert(int offset, string value, bool eolNormalized = false)
    {
        _eolNormalized = _eolNormalized && eolNormalized;
        _lastVisitedLine = (0, "");

        if (!ReferenceEquals(_tree.Root, _tree.Sentinel))
        {
            (TreeNode node, int remainder, int nodeStartOffset) = NodeAt(offset);
            Piece piece = node.Piece;
            int bufferIndex = piece.BufferIndex;
            BufferCursor insertPosInBuffer = PositionInBuffer(node, remainder);
            if (node.Piece.BufferIndex == 0
                && piece.End.Line == _lastChangeBufferPos.Line
                && piece.End.Column == _lastChangeBufferPos.Column
                && nodeStartOffset + piece.Length == offset
                && value.Length < PieceTreeConstants.AverageBufferSize)
            {
                // changed buffer
                AppendToNode(node, value);
                ComputeBufferMetadata();
                return;
            }

            if (nodeStartOffset == offset)
            {
                InsertContentToNodeLeft(value, node);
                _searchCache.Validate(offset);
            }
            else if (nodeStartOffset + node.Piece.Length > offset)
            {
                // we are inserting into the middle of a node.
                var nodesToDelete = new List<TreeNode>();
                var newRightPiece = new Piece(
                    piece.BufferIndex,
                    insertPosInBuffer,
                    piece.End,
                    GetLineFeedCount(piece.BufferIndex, insertPosInBuffer, piece.End),
                    OffsetInBuffer(bufferIndex, piece.End) - OffsetInBuffer(bufferIndex, insertPosInBuffer));

                if (ShouldCheckCrlf() && EndWithCr(value))
                {
                    int headOfRight = NodeCharCodeAt(node, remainder);

                    if (headOfRight == '\n')
                    {
                        var newStart = new BufferCursor(newRightPiece.Start.Line + 1, 0);
                        newRightPiece = new Piece(
                            newRightPiece.BufferIndex,
                            newStart,
                            newRightPiece.End,
                            GetLineFeedCount(newRightPiece.BufferIndex, newStart, newRightPiece.End),
                            newRightPiece.Length - 1);

                        value += "\n";
                    }
                }

                // reuse node for content before insertion point.
                if (ShouldCheckCrlf() && StartWithLf(value))
                {
                    int tailOfLeft = NodeCharCodeAt(node, remainder - 1);
                    if (tailOfLeft == '\r')
                    {
                        BufferCursor previousPos = PositionInBuffer(node, remainder - 1);
                        DeleteNodeTail(node, previousPos);
                        value = "\r" + value;

                        if (node.Piece.Length == 0)
                        {
                            nodesToDelete.Add(node);
                        }
                    }
                    else
                    {
                        DeleteNodeTail(node, insertPosInBuffer);
                    }
                }
                else
                {
                    DeleteNodeTail(node, insertPosInBuffer);
                }

                List<Piece> newPieces = CreateNewPieces(value);
                if (newRightPiece.Length > 0)
                {
                    _tree.InsertRight(node, newRightPiece);
                }

                TreeNode tmpNode = node;
                for (int k = 0; k < newPieces.Count; k++)
                {
                    tmpNode = _tree.InsertRight(tmpNode, newPieces[k]);
                }

                DeleteNodes(nodesToDelete);
            }
            else
            {
                InsertContentToNodeRight(value, node);
            }
        }
        else
        {
            // insert new node
            List<Piece> pieces = CreateNewPieces(value);
            TreeNode node = _tree.InsertLeft(null, pieces[0]);

            for (int k = 1; k < pieces.Count; k++)
            {
                node = _tree.InsertRight(node, pieces[k]);
            }
        }

        // todo, this is too brutal. Total line feed count should be updated the same way as lf_left.
        ComputeBufferMetadata();
    }

    private void InsertContentToNodeLeft(string value, TreeNode node)
    {
        // we are inserting content to the beginning of node
        var nodesToDelete = new List<TreeNode>();
        if (ShouldCheckCrlf() && EndWithCr(value) && StartWithLf(node))
        {
            // move `\n` to new node.
            Piece piece = node.Piece;
            var newStart = new BufferCursor(piece.Start.Line + 1, 0);
            node.Piece = new Piece(
                piece.BufferIndex,
                newStart,
                piece.End,
                GetLineFeedCount(piece.BufferIndex, newStart, piece.End),
                piece.Length - 1);

            value += "\n";
            _tree.UpdateTreeMetadata(node, -1, -1);

            if (node.Piece.Length == 0)
            {
                nodesToDelete.Add(node);
            }
        }

        List<Piece> newPieces = CreateNewPieces(value);
        TreeNode newNode = _tree.InsertLeft(node, newPieces[^1]);
        for (int k = newPieces.Count - 2; k >= 0; k--)
        {
            newNode = _tree.InsertLeft(newNode, newPieces[k]);
        }

        ValidateCrlfWithPrevNode(newNode);
        DeleteNodes(nodesToDelete);
    }

    private void InsertContentToNodeRight(string value, TreeNode node)
    {
        // we are inserting to the right of this node.
        if (AdjustCarriageReturnFromNext(value, node))
        {
            // move \n to the new node.
            value += "\n";
        }

        List<Piece> newPieces = CreateNewPieces(value);
        TreeNode newNode = _tree.InsertRight(node, newPieces[0]);
        TreeNode tmpNode = newNode;

        for (int k = 1; k < newPieces.Count; k++)
        {
            tmpNode = _tree.InsertRight(tmpNode, newPieces[k]);
        }

        ValidateCrlfWithPrevNode(newNode);
    }

    private void AppendToNode(TreeNode node, string value)
    {
        if (AdjustCarriageReturnFromNext(value, node))
        {
            value += "\n";
        }

        bool hitCrlf = ShouldCheckCrlf() && StartWithLf(value) && EndWithCr(node);
        StringBuffer changeBuffer = _buffers[0];
        int startOffset = changeBuffer.Length;
        changeBuffer.Append(value);
        List<int> lineStarts = LineStarts.CreateFast(value);
        for (int i = 0; i < lineStarts.Count; i++)
        {
            lineStarts[i] += startOffset;
        }

        if (hitCrlf)
        {
            int prevStartOffset = changeBuffer.LineStarts[^2];
            changeBuffer.LineStarts.RemoveAt(changeBuffer.LineStarts.Count - 1);
            // _lastChangeBufferPos is already wrong
            _lastChangeBufferPos = new BufferCursor(_lastChangeBufferPos.Line - 1, startOffset - prevStartOffset);
        }

        AppendLineStarts(changeBuffer, lineStarts);
        int endIndex = changeBuffer.LineStarts.Count - 1;
        int endColumn = changeBuffer.Length - changeBuffer.LineStarts[endIndex];
        var newEnd = new BufferCursor(endIndex, endColumn);
        int newLength = node.Piece.Length + value.Length;
        int oldLineFeedCount = node.Piece.LineFeedCount;
        int newLineFeedCount = GetLineFeedCount(0, node.Piece.Start, newEnd);
        int lineFeedDelta = newLineFeedCount - oldLineFeedCount;

        node.Piece = new Piece(node.Piece.BufferIndex, node.Piece.Start, newEnd, newLineFeedCount, newLength);

        _lastChangeBufferPos = newEnd;
        _tree.UpdateTreeMetadata(node, value.Length, lineFeedDelta);
    }

    private List<Piece> CreateNewPieces(string text)
    {
        if (text.Length > PieceTreeConstants.AverageBufferSize)
        {
            // the content is large, operations like substring, charCode becomes slow
            // so here we split it into smaller chunks, just like what we did for CR/LF normalization
            var newPieces = new List<Piece>();
            while (text.Length > PieceTreeConstants.AverageBufferSize)
            {
                char lastChar = text[PieceTreeConstants.AverageBufferSize - 1];
                string splitText;
                if (lastChar == '\r' || char.IsHighSurrogate(lastChar))
                {
                    // last character is \r or a high surrogate => keep it back
                    splitText = text[..(PieceTreeConstants.AverageBufferSize - 1)];
                    text = text[(PieceTreeConstants.AverageBufferSize - 1)..];
                }
                else
                {
                    splitText = text[..PieceTreeConstants.AverageBufferSize];
                    text = text[PieceTreeConstants.AverageBufferSize..];
                }

                newPieces.Add(NewOriginalPiece(splitText));
            }

            newPieces.Add(NewOriginalPiece(text));
            return newPieces;
        }

        StringBuffer changeBuffer = _buffers[0];
        int startOffset = changeBuffer.Length;
        List<int> lineStarts = LineStarts.CreateFast(text);

        BufferCursor start = _lastChangeBufferPos;
        if (changeBuffer.LineStarts[^1] == startOffset
            && startOffset != 0
            && StartWithLf(text)
            && changeBuffer[startOffset - 1] == '\r') // todo, we can check _lastChangeBufferPos's column as it's the last one
        {
            _lastChangeBufferPos = new BufferCursor(_lastChangeBufferPos.Line, _lastChangeBufferPos.Column + 1);
            start = _lastChangeBufferPos;

            for (int i = 0; i < lineStarts.Count; i++)
            {
                lineStarts[i] += startOffset + 1;
            }

            AppendLineStarts(changeBuffer, lineStarts);
            changeBuffer.Append("_" + text);
            startOffset += 1;
        }
        else
        {
            if (startOffset != 0)
            {
                for (int i = 0; i < lineStarts.Count; i++)
                {
                    lineStarts[i] += startOffset;
                }
            }

            AppendLineStarts(changeBuffer, lineStarts);
            changeBuffer.Append(text);
        }

        int endOffset = changeBuffer.Length;
        int endIndex = changeBuffer.LineStarts.Count - 1;
        int endColumn = endOffset - changeBuffer.LineStarts[endIndex];
        var endPos = new BufferCursor(endIndex, endColumn);
        var newPiece = new Piece(0, start, endPos, GetLineFeedCount(0, start, endPos), endOffset - startOffset);
        _lastChangeBufferPos = endPos;
        return [newPiece];
    }

    // Adds a new read-only buffer holding `text` and returns the piece that covers all of it.
    private Piece NewOriginalPiece(string text)
    {
        List<int> lineStarts = LineStarts.CreateFast(text);
        var piece = new Piece(
            _buffers.Count,
            new BufferCursor(0, 0),
            new BufferCursor(lineStarts.Count - 1, text.Length - lineStarts[^1]),
            lineStarts.Count - 1,
            text.Length);
        _buffers.Add(new StringBuffer(text, lineStarts));
        return piece;
    }

    // Monaco: `lineStarts.concat(x.slice(1))`, done in place.
    private static void AppendLineStarts(StringBuffer buffer, List<int> lineStarts)
    {
        for (int i = 1; i < lineStarts.Count; i++)
        {
            buffer.LineStarts.Add(lineStarts[i]);
        }
    }
}

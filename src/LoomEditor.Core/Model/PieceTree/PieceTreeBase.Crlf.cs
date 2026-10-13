// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.PieceTree;

// Keeping a \r and a following \n in one piece-tree line break.
internal sealed partial class PieceTreeBase
{
    private bool ShouldCheckCrlf() => !(_eolNormalized && _eol == "\n");

    private static bool StartWithLf(string value) => value.Length > 0 && value[0] == '\n';

    private bool StartWithLf(TreeNode node)
    {
        if (ReferenceEquals(node, _tree.Sentinel) || node.Piece.LineFeedCount == 0)
        {
            return false;
        }

        Piece piece = node.Piece;
        List<int> lineStarts = _buffers[piece.BufferIndex].LineStarts;
        int line = piece.Start.Line;
        int startOffset = lineStarts[line] + piece.Start.Column;
        if (line == lineStarts.Count - 1)
        {
            // last line, so there is no line feed at the end of this line
            return false;
        }

        int nextLineOffset = lineStarts[line + 1];
        if (nextLineOffset > startOffset + 1)
        {
            return false;
        }

        return _buffers[piece.BufferIndex][startOffset] == '\n';
    }

    private static bool EndWithCr(string value) => value.Length > 0 && value[^1] == '\r';

    private bool EndWithCr(TreeNode node)
    {
        if (ReferenceEquals(node, _tree.Sentinel) || node.Piece.LineFeedCount == 0)
        {
            return false;
        }

        return NodeCharCodeAt(node, node.Piece.Length - 1) == '\r';
    }

    private void ValidateCrlfWithPrevNode(TreeNode nextNode)
    {
        if (ShouldCheckCrlf() && StartWithLf(nextNode))
        {
            TreeNode node = _tree.Prev(nextNode);
            if (EndWithCr(node))
            {
                FixCrlf(node, nextNode);
            }
        }
    }

    private void ValidateCrlfWithNextNode(TreeNode node)
    {
        if (ShouldCheckCrlf() && EndWithCr(node))
        {
            TreeNode nextNode = _tree.Next(node);
            if (StartWithLf(nextNode))
            {
                FixCrlf(node, nextNode);
            }
        }
    }

    private void FixCrlf(TreeNode prev, TreeNode next)
    {
        var nodesToDelete = new List<TreeNode>();
        // update node
        List<int> lineStarts = _buffers[prev.Piece.BufferIndex].LineStarts;
        BufferCursor newEnd;
        if (prev.Piece.End.Column == 0)
        {
            // it means, last line ends with \r, not \r\n
            newEnd = new BufferCursor(
                prev.Piece.End.Line - 1,
                lineStarts[prev.Piece.End.Line] - lineStarts[prev.Piece.End.Line - 1] - 1);
        }
        else
        {
            // \r\n
            newEnd = new BufferCursor(prev.Piece.End.Line, prev.Piece.End.Column - 1);
        }

        int prevNewLength = prev.Piece.Length - 1;
        int prevNewLineFeedCount = prev.Piece.LineFeedCount - 1;
        prev.Piece = new Piece(prev.Piece.BufferIndex, prev.Piece.Start, newEnd, prevNewLineFeedCount, prevNewLength);

        _tree.UpdateTreeMetadata(prev, -1, -1);
        if (prev.Piece.Length == 0)
        {
            nodesToDelete.Add(prev);
        }

        // update nextNode
        var newStart = new BufferCursor(next.Piece.Start.Line + 1, 0);
        int newLength = next.Piece.Length - 1;
        int newLineFeedCount = GetLineFeedCount(next.Piece.BufferIndex, newStart, next.Piece.End);
        next.Piece = new Piece(next.Piece.BufferIndex, newStart, next.Piece.End, newLineFeedCount, newLength);

        _tree.UpdateTreeMetadata(next, -1, -1);
        if (next.Piece.Length == 0)
        {
            nodesToDelete.Add(next);
        }

        // create new piece which contains \r\n
        List<Piece> pieces = CreateNewPieces("\r\n");
        _tree.InsertRight(prev, pieces[0]);

        // delete empty nodes
        DeleteNodes(nodesToDelete);
    }

    private bool AdjustCarriageReturnFromNext(string value, TreeNode node)
    {
        if (ShouldCheckCrlf() && EndWithCr(value))
        {
            TreeNode nextNode = _tree.Next(node);
            if (StartWithLf(nextNode))
            {
                // move `\n` forward
                if (nextNode.Piece.Length == 1)
                {
                    _tree.Delete(nextNode);
                }
                else
                {
                    Piece piece = nextNode.Piece;
                    var newStart = new BufferCursor(piece.Start.Line + 1, 0);
                    int newLength = piece.Length - 1;
                    int newLineFeedCount = GetLineFeedCount(piece.BufferIndex, newStart, piece.End);
                    nextNode.Piece = new Piece(piece.BufferIndex, newStart, piece.End, newLineFeedCount, newLength);

                    _tree.UpdateTreeMetadata(nextNode, -1, -1);
                }

                return true;
            }
        }

        return false;
    }
}

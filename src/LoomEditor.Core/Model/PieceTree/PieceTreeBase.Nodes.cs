// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.PieceTree;

// Node helpers shared by insert and delete.
internal sealed partial class PieceTreeBase
{
    private void DeleteNodes(List<TreeNode> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            _tree.Delete(nodes[i]);
        }
    }

    private void DeleteNodeTail(TreeNode node, BufferCursor pos)
    {
        Piece piece = node.Piece;
        int originalLineFeedCount = piece.LineFeedCount;
        int originalEndOffset = OffsetInBuffer(piece.BufferIndex, piece.End);

        BufferCursor newEnd = pos;
        int newEndOffset = OffsetInBuffer(piece.BufferIndex, newEnd);
        int newLineFeedCount = GetLineFeedCount(piece.BufferIndex, piece.Start, newEnd);

        int lineFeedDelta = newLineFeedCount - originalLineFeedCount;
        int sizeDelta = newEndOffset - originalEndOffset;
        int newLength = piece.Length + sizeDelta;

        node.Piece = new Piece(piece.BufferIndex, piece.Start, newEnd, newLineFeedCount, newLength);

        _tree.UpdateTreeMetadata(node, sizeDelta, lineFeedDelta);
    }

    private void DeleteNodeHead(TreeNode node, BufferCursor pos)
    {
        Piece piece = node.Piece;
        int originalLineFeedCount = piece.LineFeedCount;
        int originalStartOffset = OffsetInBuffer(piece.BufferIndex, piece.Start);

        BufferCursor newStart = pos;
        int newLineFeedCount = GetLineFeedCount(piece.BufferIndex, newStart, piece.End);
        int newStartOffset = OffsetInBuffer(piece.BufferIndex, newStart);
        int lineFeedDelta = newLineFeedCount - originalLineFeedCount;
        int sizeDelta = originalStartOffset - newStartOffset;
        int newLength = piece.Length + sizeDelta;

        node.Piece = new Piece(piece.BufferIndex, newStart, piece.End, newLineFeedCount, newLength);

        _tree.UpdateTreeMetadata(node, sizeDelta, lineFeedDelta);
    }

    private void ShrinkNode(TreeNode node, BufferCursor start, BufferCursor end)
    {
        Piece piece = node.Piece;
        BufferCursor originalStartPos = piece.Start;
        BufferCursor originalEndPos = piece.End;

        // old piece, originalStartPos, start
        int oldLength = piece.Length;
        int oldLineFeedCount = piece.LineFeedCount;
        BufferCursor newEnd = start;
        int newLineFeedCount = GetLineFeedCount(piece.BufferIndex, piece.Start, newEnd);
        int newLength = OffsetInBuffer(piece.BufferIndex, start) - OffsetInBuffer(piece.BufferIndex, originalStartPos);

        node.Piece = new Piece(piece.BufferIndex, piece.Start, newEnd, newLineFeedCount, newLength);

        _tree.UpdateTreeMetadata(node, newLength - oldLength, newLineFeedCount - oldLineFeedCount);

        // new right piece, end, originalEndPos
        var newPiece = new Piece(
            piece.BufferIndex,
            end,
            originalEndPos,
            GetLineFeedCount(piece.BufferIndex, end, originalEndPos),
            OffsetInBuffer(piece.BufferIndex, originalEndPos) - OffsetInBuffer(piece.BufferIndex, end));

        TreeNode newNode = _tree.InsertRight(node, newPiece);
        ValidateCrlfWithPrevNode(newNode);
    }
}

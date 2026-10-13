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
}

// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.PieceTree;

// Delete.
internal sealed partial class PieceTreeBase
{
    /// <summary>Deletes <paramref name="count"/> characters starting at <paramref name="offset"/> (Monaco: <c>delete</c>).</summary>
    /// <param name="offset">Document offset of the first deleted character.</param>
    /// <param name="count">Number of characters to delete; zero or less does nothing.</param>
    public void Delete(int offset, int count)
    {
        _lastVisitedLine = (0, "");

        if (count <= 0 || ReferenceEquals(_tree.Root, _tree.Sentinel))
        {
            return;
        }

        NodePosition startPosition = NodeAt(offset);
        NodePosition endPosition = NodeAt(offset + count);
        TreeNode startNode = startPosition.Node;
        TreeNode endNode = endPosition.Node;

        if (ReferenceEquals(startNode, endNode))
        {
            BufferCursor startSplitPosInBuffer = PositionInBuffer(startNode, startPosition.Remainder);
            BufferCursor endSplitPosInBuffer = PositionInBuffer(startNode, endPosition.Remainder);

            if (startPosition.NodeStartOffset == offset)
            {
                if (count == startNode.Piece.Length)
                {
                    // delete node
                    TreeNode next = _tree.Next(startNode);
                    _tree.Delete(startNode);
                    ValidateCrlfWithPrevNode(next);
                    ComputeBufferMetadata();
                    return;
                }

                DeleteNodeHead(startNode, endSplitPosInBuffer);
                _searchCache.Validate(offset);
                ValidateCrlfWithPrevNode(startNode);
                ComputeBufferMetadata();
                return;
            }

            if (startPosition.NodeStartOffset + startNode.Piece.Length == offset + count)
            {
                DeleteNodeTail(startNode, startSplitPosInBuffer);
                ValidateCrlfWithNextNode(startNode);
                ComputeBufferMetadata();
                return;
            }

            // delete content in the middle, this node will be split into two nodes
            ShrinkNode(startNode, startSplitPosInBuffer, endSplitPosInBuffer);
            ComputeBufferMetadata();
            return;
        }

        var nodesToDelete = new List<TreeNode>();

        BufferCursor startSplit = PositionInBuffer(startNode, startPosition.Remainder);
        DeleteNodeTail(startNode, startSplit);
        _searchCache.Validate(offset);
        if (startNode.Piece.Length == 0)
        {
            nodesToDelete.Add(startNode);
        }

        // update last touched node
        BufferCursor endSplit = PositionInBuffer(endNode, endPosition.Remainder);
        DeleteNodeHead(endNode, endSplit);
        if (endNode.Piece.Length == 0)
        {
            nodesToDelete.Add(endNode);
        }

        // delete nodes in between
        TreeNode secondNode = _tree.Next(startNode);
        for (TreeNode node = secondNode;
             !ReferenceEquals(node, _tree.Sentinel) && !ReferenceEquals(node, endNode);
             node = _tree.Next(node))
        {
            nodesToDelete.Add(node);
        }

        TreeNode prev = startNode.Piece.Length == 0 ? _tree.Prev(startNode) : startNode;
        DeleteNodes(nodesToDelete);
        ValidateCrlfWithNextNode(prev);
        ComputeBufferMetadata();
    }
}

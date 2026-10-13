// Ported from microsoft/vscode: src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.PieceTree;

namespace LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

// Monaco: assertTreeInvariants, depth, assertValidNode, assertValidTree
internal static class TreeInvariants
{
    public static void AssertValid(RedBlackTree tree)
    {
        TreeNode sentinel = tree.Sentinel;
        Assert.Equal(NodeColor.Black, sentinel.Color);
        Assert.Same(sentinel, sentinel.Parent);
        Assert.Same(sentinel, sentinel.Left);
        Assert.Same(sentinel, sentinel.Right);
        Assert.Equal(0, sentinel.SizeLeft);
        Assert.Equal(0, sentinel.LineFeedCountLeft);

        if (ReferenceEquals(tree.Root, sentinel))
        {
            return;
        }

        Assert.Equal(NodeColor.Black, tree.Root.Color);
        Assert.Same(sentinel, tree.Root.Parent);
        Assert.Equal(Depth(tree, tree.Root.Left), Depth(tree, tree.Root.Right));
        AssertValidNode(tree, tree.Root);
    }

    private static int Depth(RedBlackTree tree, TreeNode n)
    {
        if (ReferenceEquals(n, tree.Sentinel))
        {
            // The leaves are black
            return 1;
        }

        int left = Depth(tree, n.Left);
        Assert.Equal(left, Depth(tree, n.Right));
        return (n.Color == NodeColor.Black ? 1 : 0) + left;
    }

    private static (int Size, int LineFeeds) AssertValidNode(RedBlackTree tree, TreeNode n)
    {
        if (ReferenceEquals(n, tree.Sentinel))
        {
            return (0, 0);
        }

        if (n.Color == NodeColor.Red)
        {
            Assert.Equal(NodeColor.Black, n.Left.Color);
            Assert.Equal(NodeColor.Black, n.Right.Color);
        }

        if (!ReferenceEquals(n.Left, tree.Sentinel))
        {
            Assert.Same(n, n.Left.Parent);
        }

        if (!ReferenceEquals(n.Right, tree.Sentinel))
        {
            Assert.Same(n, n.Right.Parent);
        }

        var actualLeft = AssertValidNode(tree, n.Left);
        Assert.Equal(n.LineFeedCountLeft, actualLeft.LineFeeds);
        Assert.Equal(n.SizeLeft, actualLeft.Size);
        var actualRight = AssertValidNode(tree, n.Right);

        return (
            n.SizeLeft + n.Piece.Length + actualRight.Size,
            n.LineFeedCountLeft + n.Piece.LineFeedCount + actualRight.LineFeeds);
    }
}

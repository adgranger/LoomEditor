// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/rbTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.PieceTree;

/// <summary>
/// The red-black tree that holds the pieces, augmented with the size and line feed count of each left subtree.
/// </summary>
/// <remarks>
/// Monaco: the functions of <c>rbTreeBase.ts</c> plus <c>rbInsertRight</c>/<c>rbInsertLeft</c> of
/// <c>pieceTreeBase.ts</c>. Difference: each tree owns its sentinel instead of sharing a global one.
/// Threading: not thread-safe; distinct trees are fully independent.
/// </remarks>
internal sealed partial class RedBlackTree
{
    private static readonly Piece SentinelPiece = new(0, default, default, 0, 0);

    public RedBlackTree()
    {
        Sentinel = new TreeNode(SentinelPiece, NodeColor.Black);
        Root = Sentinel;
    }

    /// <summary>The shared black leaf of this tree (Monaco: <c>SENTINEL</c>, but one per tree).</summary>
    public TreeNode Sentinel { get; }

    public TreeNode Root { get; set; }

    public TreeNode Leftest(TreeNode node)
    {
        while (!ReferenceEquals(node.Left, Sentinel))
        {
            node = node.Left;
        }

        return node;
    }

    public TreeNode Righttest(TreeNode node)
    {
        while (!ReferenceEquals(node.Right, Sentinel))
        {
            node = node.Right;
        }

        return node;
    }

    /// <summary>In-order successor, or the sentinel (Monaco: <c>TreeNode.next</c>).</summary>
    public TreeNode Next(TreeNode node)
    {
        if (!ReferenceEquals(node.Right, Sentinel))
        {
            return Leftest(node.Right);
        }

        while (!ReferenceEquals(node.Parent, Sentinel))
        {
            if (ReferenceEquals(node.Parent.Left, node))
            {
                break;
            }

            node = node.Parent;
        }

        return ReferenceEquals(node.Parent, Sentinel) ? Sentinel : node.Parent;
    }

    /// <summary>In-order predecessor, or the sentinel (Monaco: <c>TreeNode.prev</c>).</summary>
    public TreeNode Prev(TreeNode node)
    {
        if (!ReferenceEquals(node.Left, Sentinel))
        {
            return Righttest(node.Left);
        }

        while (!ReferenceEquals(node.Parent, Sentinel))
        {
            if (ReferenceEquals(node.Parent.Right, node))
            {
                break;
            }

            node = node.Parent;
        }

        return ReferenceEquals(node.Parent, Sentinel) ? Sentinel : node.Parent;
    }

    /// <summary>
    /// Inserts <paramref name="piece"/> right after <paramref name="node"/> (in-order); <c>null</c> is only valid
    /// on an empty tree (Monaco: <c>rbInsertRight</c>).
    /// </summary>
    public TreeNode InsertRight(TreeNode? node, Piece piece)
    {
        TreeNode z = NewLeaf(piece);

        if (ReferenceEquals(Root, Sentinel))
        {
            Root = z;
            z.Color = NodeColor.Black;
        }
        else if (ReferenceEquals(node!.Right, Sentinel))
        {
            node.Right = z;
            z.Parent = node;
        }
        else
        {
            TreeNode nextNode = Leftest(node.Right);
            nextNode.Left = z;
            z.Parent = nextNode;
        }

        FixInsert(z);
        return z;
    }

    /// <summary>
    /// Inserts <paramref name="piece"/> right before <paramref name="node"/> (in-order); <c>null</c> is only valid
    /// on an empty tree (Monaco: <c>rbInsertLeft</c>).
    /// </summary>
    public TreeNode InsertLeft(TreeNode? node, Piece piece)
    {
        TreeNode z = NewLeaf(piece);

        if (ReferenceEquals(Root, Sentinel))
        {
            Root = z;
            z.Color = NodeColor.Black;
        }
        else if (ReferenceEquals(node!.Left, Sentinel))
        {
            node.Left = z;
            z.Parent = node;
        }
        else
        {
            TreeNode prevNode = Righttest(node.Left);
            prevNode.Right = z;
            z.Parent = prevNode;
        }

        FixInsert(z);
        return z;
    }

    public void LeftRotate(TreeNode x)
    {
        TreeNode y = x.Right;

        // fix size_left
        y.SizeLeft += x.SizeLeft + x.Piece.Length;
        y.LineFeedCountLeft += x.LineFeedCountLeft + x.Piece.LineFeedCount;
        x.Right = y.Left;

        if (!ReferenceEquals(y.Left, Sentinel))
        {
            y.Left.Parent = x;
        }

        y.Parent = x.Parent;
        if (ReferenceEquals(x.Parent, Sentinel))
        {
            Root = y;
        }
        else if (ReferenceEquals(x.Parent.Left, x))
        {
            x.Parent.Left = y;
        }
        else
        {
            x.Parent.Right = y;
        }

        y.Left = x;
        x.Parent = y;
    }

    public void RightRotate(TreeNode y)
    {
        TreeNode x = y.Left;
        y.Left = x.Right;
        if (!ReferenceEquals(x.Right, Sentinel))
        {
            x.Right.Parent = y;
        }

        x.Parent = y.Parent;

        // fix size_left
        y.SizeLeft -= x.SizeLeft + x.Piece.Length;
        y.LineFeedCountLeft -= x.LineFeedCountLeft + x.Piece.LineFeedCount;

        if (ReferenceEquals(y.Parent, Sentinel))
        {
            Root = x;
        }
        else if (ReferenceEquals(y, y.Parent.Right))
        {
            y.Parent.Right = x;
        }
        else
        {
            y.Parent.Left = x;
        }

        x.Right = y;
        y.Parent = x;
    }

    public void FixInsert(TreeNode x)
    {
        RecomputeTreeMetadata(x);

        while (!ReferenceEquals(x, Root) && x.Parent.Color == NodeColor.Red)
        {
            if (ReferenceEquals(x.Parent, x.Parent.Parent.Left))
            {
                TreeNode y = x.Parent.Parent.Right;

                if (y.Color == NodeColor.Red)
                {
                    x.Parent.Color = NodeColor.Black;
                    y.Color = NodeColor.Black;
                    x.Parent.Parent.Color = NodeColor.Red;
                    x = x.Parent.Parent;
                }
                else
                {
                    if (ReferenceEquals(x, x.Parent.Right))
                    {
                        x = x.Parent;
                        LeftRotate(x);
                    }

                    x.Parent.Color = NodeColor.Black;
                    x.Parent.Parent.Color = NodeColor.Red;
                    RightRotate(x.Parent.Parent);
                }
            }
            else
            {
                TreeNode y = x.Parent.Parent.Left;

                if (y.Color == NodeColor.Red)
                {
                    x.Parent.Color = NodeColor.Black;
                    y.Color = NodeColor.Black;
                    x.Parent.Parent.Color = NodeColor.Red;
                    x = x.Parent.Parent;
                }
                else
                {
                    if (ReferenceEquals(x, x.Parent.Left))
                    {
                        x = x.Parent;
                        RightRotate(x);
                    }

                    x.Parent.Color = NodeColor.Black;
                    x.Parent.Parent.Color = NodeColor.Red;
                    LeftRotate(x.Parent.Parent);
                }
            }
        }

        Root.Color = NodeColor.Black;
    }

    /// <summary>Propagates a length / line feed change of <paramref name="x"/>'s piece to its ancestors.</summary>
    public void UpdateTreeMetadata(TreeNode x, int delta, int lineFeedCountDelta)
    {
        // node length change or line feed count change
        while (!ReferenceEquals(x, Root) && !ReferenceEquals(x, Sentinel))
        {
            if (ReferenceEquals(x.Parent.Left, x))
            {
                x.Parent.SizeLeft += delta;
                x.Parent.LineFeedCountLeft += lineFeedCountDelta;
            }

            x = x.Parent;
        }
    }

    public void RecomputeTreeMetadata(TreeNode x)
    {
        if (ReferenceEquals(x, Root))
        {
            return;
        }

        // go upwards till the node whose left subtree is changed.
        while (!ReferenceEquals(x, Root) && ReferenceEquals(x, x.Parent.Right))
        {
            x = x.Parent;
        }

        if (ReferenceEquals(x, Root))
        {
            // well, it means we add a node to the end (inorder)
            return;
        }

        // x is the node whose right subtree is changed.
        x = x.Parent;

        int delta = CalculateSize(x.Left) - x.SizeLeft;
        int lfDelta = CalculateLineFeedCount(x.Left) - x.LineFeedCountLeft;
        x.SizeLeft += delta;
        x.LineFeedCountLeft += lfDelta;

        // go upwards till root. O(logN)
        while (!ReferenceEquals(x, Root) && (delta != 0 || lfDelta != 0))
        {
            if (ReferenceEquals(x.Parent.Left, x))
            {
                x.Parent.SizeLeft += delta;
                x.Parent.LineFeedCountLeft += lfDelta;
            }

            x = x.Parent;
        }
    }

    private TreeNode NewLeaf(Piece piece) => new(piece, NodeColor.Red)
    {
        Left = Sentinel,
        Right = Sentinel,
        Parent = Sentinel,
    };

    private int CalculateSize(TreeNode node)
    {
        // Iterative on the right spine: same sum as Monaco's recursion.
        int size = 0;
        while (!ReferenceEquals(node, Sentinel))
        {
            size += node.SizeLeft + node.Piece.Length;
            node = node.Right;
        }

        return size;
    }

    private int CalculateLineFeedCount(TreeNode node)
    {
        int count = 0;
        while (!ReferenceEquals(node, Sentinel))
        {
            count += node.LineFeedCountLeft + node.Piece.LineFeedCount;
            node = node.Right;
        }

        return count;
    }

    private void ResetSentinel() => Sentinel.Parent = Sentinel;
}

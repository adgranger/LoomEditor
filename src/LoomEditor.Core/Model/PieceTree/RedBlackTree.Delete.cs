// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/rbTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.PieceTree;

internal sealed partial class RedBlackTree
{
    /// <summary>Removes <paramref name="z"/> from the tree and detaches it (Monaco: <c>rbDelete</c>).</summary>
    public void Delete(TreeNode z)
    {
        TreeNode x;
        TreeNode y;

        if (ReferenceEquals(z.Left, Sentinel))
        {
            y = z;
            x = y.Right;
        }
        else if (ReferenceEquals(z.Right, Sentinel))
        {
            y = z;
            x = y.Left;
        }
        else
        {
            y = Leftest(z.Right);
            x = y.Right;
        }

        if (ReferenceEquals(y, Root))
        {
            Root = x;

            // if x is the sentinel, we are removing the only node
            x.Color = NodeColor.Black;
            z.Detach(Sentinel);
            ResetSentinel();
            Root.Parent = Sentinel;

            return;
        }

        bool yWasRed = y.Color == NodeColor.Red;

        if (ReferenceEquals(y, y.Parent.Left))
        {
            y.Parent.Left = x;
        }
        else
        {
            y.Parent.Right = x;
        }

        if (ReferenceEquals(y, z))
        {
            x.Parent = y.Parent;
            RecomputeTreeMetadata(x);
        }
        else
        {
            x.Parent = ReferenceEquals(y.Parent, z) ? y : y.Parent;

            // as we make changes to x's hierarchy, update size_left of subtree first
            RecomputeTreeMetadata(x);

            y.Left = z.Left;
            y.Right = z.Right;
            y.Parent = z.Parent;
            y.Color = z.Color;

            if (ReferenceEquals(z, Root))
            {
                Root = y;
            }
            else if (ReferenceEquals(z, z.Parent.Left))
            {
                z.Parent.Left = y;
            }
            else
            {
                z.Parent.Right = y;
            }

            if (!ReferenceEquals(y.Left, Sentinel))
            {
                y.Left.Parent = y;
            }

            if (!ReferenceEquals(y.Right, Sentinel))
            {
                y.Right.Parent = y;
            }

            // we replace z with y, so in this sub tree, the length change is z.item.length
            y.SizeLeft = z.SizeLeft;
            y.LineFeedCountLeft = z.LineFeedCountLeft;
            RecomputeTreeMetadata(y);
        }

        z.Detach(Sentinel);

        if (ReferenceEquals(x.Parent.Left, x))
        {
            int newSizeLeft = CalculateSize(x);
            int newLineFeedLeft = CalculateLineFeedCount(x);
            if (newSizeLeft != x.Parent.SizeLeft || newLineFeedLeft != x.Parent.LineFeedCountLeft)
            {
                int delta = newSizeLeft - x.Parent.SizeLeft;
                int lfDelta = newLineFeedLeft - x.Parent.LineFeedCountLeft;
                x.Parent.SizeLeft = newSizeLeft;
                x.Parent.LineFeedCountLeft = newLineFeedLeft;
                UpdateTreeMetadata(x.Parent, delta, lfDelta);
            }
        }

        RecomputeTreeMetadata(x.Parent);

        if (yWasRed)
        {
            ResetSentinel();
            return;
        }

        DeleteFixup(x);
        ResetSentinel();
    }

    private void DeleteFixup(TreeNode x)
    {
        TreeNode w;
        while (!ReferenceEquals(x, Root) && x.Color == NodeColor.Black)
        {
            if (ReferenceEquals(x, x.Parent.Left))
            {
                w = x.Parent.Right;

                if (w.Color == NodeColor.Red)
                {
                    w.Color = NodeColor.Black;
                    x.Parent.Color = NodeColor.Red;
                    LeftRotate(x.Parent);
                    w = x.Parent.Right;
                }

                if (w.Left.Color == NodeColor.Black && w.Right.Color == NodeColor.Black)
                {
                    w.Color = NodeColor.Red;
                    x = x.Parent;
                }
                else
                {
                    if (w.Right.Color == NodeColor.Black)
                    {
                        w.Left.Color = NodeColor.Black;
                        w.Color = NodeColor.Red;
                        RightRotate(w);
                        w = x.Parent.Right;
                    }

                    w.Color = x.Parent.Color;
                    x.Parent.Color = NodeColor.Black;
                    w.Right.Color = NodeColor.Black;
                    LeftRotate(x.Parent);
                    x = Root;
                }
            }
            else
            {
                w = x.Parent.Left;

                if (w.Color == NodeColor.Red)
                {
                    w.Color = NodeColor.Black;
                    x.Parent.Color = NodeColor.Red;
                    RightRotate(x.Parent);
                    w = x.Parent.Left;
                }

                if (w.Left.Color == NodeColor.Black && w.Right.Color == NodeColor.Black)
                {
                    w.Color = NodeColor.Red;
                    x = x.Parent;
                }
                else
                {
                    if (w.Left.Color == NodeColor.Black)
                    {
                        w.Right.Color = NodeColor.Black;
                        w.Color = NodeColor.Red;
                        LeftRotate(w);
                        w = x.Parent.Left;
                    }

                    w.Color = x.Parent.Color;
                    x.Parent.Color = NodeColor.Black;
                    w.Left.Color = NodeColor.Black;
                    RightRotate(x.Parent);
                    x = Root;
                }
            }
        }

        x.Color = NodeColor.Black;
    }
}

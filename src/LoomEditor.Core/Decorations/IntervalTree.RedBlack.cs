// Ported from microsoft/vscode: src/vs/editor/common/model/intervalTree.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Decorations;

// Red-black insertion, deletion, rotations and the maxEnd computation.
internal sealed partial class IntervalTree
{
    /// <summary>Compares two intervals by start, then by end (Monaco: <c>intervalCompare</c>).</summary>
    public static long IntervalCompare(long aStart, long aEnd, long bStart, long bEnd)
    {
        if (aStart == bStart)
        {
            return aEnd - bEnd;
        }

        return aStart - bStart;
    }

    /// <summary>Recomputes <see cref="IntervalNode.MaxEnd"/> from the node and its children (Monaco: <c>recomputeMaxEnd</c>).</summary>
    public void RecomputeMaxEnd(IntervalNode node)
    {
        node.MaxEnd = ComputeMaxEnd(node);
    }

    private long ComputeMaxEnd(IntervalNode node)
    {
        var maxEnd = node.End;
        if (node.Left != Sentinel)
        {
            var leftMaxEnd = node.Left.MaxEnd;
            if (leftMaxEnd > maxEnd)
            {
                maxEnd = leftMaxEnd;
            }
        }

        if (node.Right != Sentinel)
        {
            var rightMaxEnd = node.Right.MaxEnd + node.Delta;
            if (rightMaxEnd > maxEnd)
            {
                maxEnd = rightMaxEnd;
            }
        }

        return maxEnd;
    }

    private void RecomputeMaxEndWalkToRoot(IntervalNode node)
    {
        while (node != Sentinel)
        {
            var maxEnd = ComputeMaxEnd(node);

            if (node.MaxEnd == maxEnd)
            {
                // no need to go further
                return;
            }

            node.MaxEnd = maxEnd;
            node = node.Parent;
        }
    }

    private void RequestNormalizeIfUnsafe(long delta)
    {
        if (delta < MinSafeDelta || delta > MaxSafeDelta)
        {
            RequestNormalizeDelta = true;
        }
    }

    private IntervalNode RbTreeInsert(IntervalNode newNode)
    {
        if (Root == Sentinel)
        {
            newNode.Parent = Sentinel;
            newNode.Left = Sentinel;
            newNode.Right = Sentinel;
            newNode.Color = NodeColor.Black;
            Root = newNode;
            return Root;
        }

        TreeInsert(newNode);

        RecomputeMaxEndWalkToRoot(newNode.Parent);

        // repair tree
        var x = newNode;
        while (x != Root && x.Parent.Color == NodeColor.Red)
        {
            if (x.Parent == x.Parent.Parent.Left)
            {
                var y = x.Parent.Parent.Right;

                if (y.Color == NodeColor.Red)
                {
                    x.Parent.Color = NodeColor.Black;
                    y.Color = NodeColor.Black;
                    x.Parent.Parent.Color = NodeColor.Red;
                    x = x.Parent.Parent;
                }
                else
                {
                    if (x == x.Parent.Right)
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
                var y = x.Parent.Parent.Left;

                if (y.Color == NodeColor.Red)
                {
                    x.Parent.Color = NodeColor.Black;
                    y.Color = NodeColor.Black;
                    x.Parent.Parent.Color = NodeColor.Red;
                    x = x.Parent.Parent;
                }
                else
                {
                    if (x == x.Parent.Left)
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

        return newNode;
    }

    private void TreeInsert(IntervalNode z)
    {
        long delta = 0;
        var x = Root;
        var zAbsoluteStart = z.Start;
        var zAbsoluteEnd = z.End;
        while (true)
        {
            var cmp = IntervalCompare(zAbsoluteStart, zAbsoluteEnd, x.Start + delta, x.End + delta);
            if (cmp < 0)
            {
                // this node should be inserted to the left
                // => it is not affected by the node's delta
                if (x.Left == Sentinel)
                {
                    z.Start -= delta;
                    z.End -= delta;
                    z.MaxEnd -= delta;
                    x.Left = z;
                    break;
                }

                x = x.Left;
            }
            else
            {
                // this node should be inserted to the right
                // => it is not affected by the node's delta
                if (x.Right == Sentinel)
                {
                    z.Start -= delta + x.Delta;
                    z.End -= delta + x.Delta;
                    z.MaxEnd -= delta + x.Delta;
                    x.Right = z;
                    break;
                }

                delta += x.Delta;
                x = x.Right;
            }
        }

        z.Parent = x;
        z.Left = Sentinel;
        z.Right = Sentinel;
        z.Color = NodeColor.Red;
    }

    private void RbTreeDelete(IntervalNode z)
    {
        IntervalNode x;
        IntervalNode y;

        // RB-DELETE except we don't swap z and y in case c)
        // i.e. we always delete what's pointed at by z.
        if (z.Left == Sentinel)
        {
            x = z.Right;
            y = z;

            // x's delta is no longer influenced by z's delta
            x.Delta += z.Delta;
            RequestNormalizeIfUnsafe(x.Delta);
            x.Start += z.Delta;
            x.End += z.Delta;
        }
        else if (z.Right == Sentinel)
        {
            x = z.Left;
            y = z;
        }
        else
        {
            y = Leftest(z.Right);
            x = y.Right;

            // y's delta is no longer influenced by z's delta,
            // but we don't want to walk the entire right-hand-side subtree of x.
            // we therefore maintain z's delta in y, and adjust only x
            x.Start += y.Delta;
            x.End += y.Delta;
            x.Delta += y.Delta;
            RequestNormalizeIfUnsafe(x.Delta);

            y.Start += z.Delta;
            y.End += z.Delta;
            y.Delta = z.Delta;
            RequestNormalizeIfUnsafe(y.Delta);
        }

        if (y == Root)
        {
            Root = x;
            x.Color = NodeColor.Black;

            z.Detach();
            ResetSentinel();
            RecomputeMaxEnd(x);
            Root.Parent = Sentinel;
            return;
        }

        var yWasRed = y.Color == NodeColor.Red;

        if (y == y.Parent.Left)
        {
            y.Parent.Left = x;
        }
        else
        {
            y.Parent.Right = x;
        }

        if (y == z)
        {
            x.Parent = y.Parent;
        }
        else
        {
            x.Parent = y.Parent == z ? y : y.Parent;

            y.Left = z.Left;
            y.Right = z.Right;
            y.Parent = z.Parent;
            y.Color = z.Color;

            if (z == Root)
            {
                Root = y;
            }
            else if (z == z.Parent.Left)
            {
                z.Parent.Left = y;
            }
            else
            {
                z.Parent.Right = y;
            }

            if (y.Left != Sentinel)
            {
                y.Left.Parent = y;
            }

            if (y.Right != Sentinel)
            {
                y.Right.Parent = y;
            }
        }

        z.Detach();

        if (yWasRed)
        {
            RecomputeMaxEndWalkToRoot(x.Parent);
            if (y != z)
            {
                RecomputeMaxEndWalkToRoot(y);
                RecomputeMaxEndWalkToRoot(y.Parent);
            }

            ResetSentinel();
            return;
        }

        RecomputeMaxEndWalkToRoot(x);
        RecomputeMaxEndWalkToRoot(x.Parent);
        if (y != z)
        {
            RecomputeMaxEndWalkToRoot(y);
            RecomputeMaxEndWalkToRoot(y.Parent);
        }

        // RB-DELETE-FIXUP
        IntervalNode w;
        while (x != Root && x.Color == NodeColor.Black)
        {
            if (x == x.Parent.Left)
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
        ResetSentinel();
    }

    private IntervalNode Leftest(IntervalNode node)
    {
        while (node.Left != Sentinel)
        {
            node = node.Left;
        }

        return node;
    }

    private void ResetSentinel()
    {
        Sentinel.Parent = Sentinel;
        Sentinel.Delta = 0; // optional
        Sentinel.Start = 0; // optional
        Sentinel.End = 0; // optional
    }

    private void LeftRotate(IntervalNode x)
    {
        var y = x.Right; // set y.

        y.Delta += x.Delta; // y's delta is no longer influenced by x's delta
        RequestNormalizeIfUnsafe(y.Delta);
        y.Start += x.Delta;
        y.End += x.Delta;

        x.Right = y.Left; // turn y's left subtree into x's right subtree.
        if (y.Left != Sentinel)
        {
            y.Left.Parent = x;
        }

        y.Parent = x.Parent; // link x's parent to y.
        if (x.Parent == Sentinel)
        {
            Root = y;
        }
        else if (x == x.Parent.Left)
        {
            x.Parent.Left = y;
        }
        else
        {
            x.Parent.Right = y;
        }

        y.Left = x; // put x on y's left.
        x.Parent = y;

        RecomputeMaxEnd(x);
        RecomputeMaxEnd(y);
    }

    private void RightRotate(IntervalNode y)
    {
        var x = y.Left;

        y.Delta -= x.Delta;
        RequestNormalizeIfUnsafe(y.Delta);
        y.Start -= x.Delta;
        y.End -= x.Delta;

        y.Left = x.Right;
        if (x.Right != Sentinel)
        {
            x.Right.Parent = y;
        }

        x.Parent = y.Parent;
        if (y.Parent == Sentinel)
        {
            Root = x;
        }
        else if (y == y.Parent.Right)
        {
            y.Parent.Right = x;
        }
        else
        {
            y.Parent.Left = x;
        }

        x.Right = y;
        y.Parent = x;

        RecomputeMaxEnd(y);
        RecomputeMaxEnd(x);
    }
}

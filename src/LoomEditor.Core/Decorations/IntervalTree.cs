// Ported from microsoft/vscode: src/vs/editor/common/model/intervalTree.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Decorations;

/// <summary>
/// An augmented red-black tree of intervals that moves them correctly when the text is edited
/// (based on "Introduction to Algorithms" by Cormen, Leiserson and Rivest). Stores decorations and tracked ranges.
/// </summary>
/// <remarks>
/// <para>Threading: not thread-safe; use from one thread.</para>
/// <para>
/// Behavior differences: each tree owns its sentinel (Monaco uses one global <c>SENTINEL</c>), and offsets are
/// <see cref="long"/> (Monaco uses 53-bit JavaScript numbers).
/// </para>
/// </remarks>
internal sealed partial class IntervalTree
{
    /// <summary>Deltas are normalized when they leave [<see cref="MinSafeDelta"/>, <see cref="MaxSafeDelta"/>] (Monaco: <c>MIN_SAFE_DELTA</c>).</summary>
    internal const long MinSafeDelta = -(1L << 30);

    /// <summary>See <see cref="MinSafeDelta"/> (Monaco: <c>MAX_SAFE_DELTA</c>).</summary>
    internal const long MaxSafeDelta = 1L << 30;

    public IntervalTree()
    {
        Sentinel = new IntervalNode(string.Empty, 0, 0);
        Sentinel.Parent = Sentinel;
        Sentinel.Left = Sentinel;
        Sentinel.Right = Sentinel;
        Sentinel.Color = NodeColor.Black;
        Root = Sentinel;
        RequestNormalizeDelta = false;
    }

    /// <summary>The node that stands for "no node"; black, and never part of the tree.</summary>
    public IntervalNode Sentinel { get; }

    public IntervalNode Root { get; private set; }

    public bool RequestNormalizeDelta { get; private set; }

    public void Insert(IntervalNode node)
    {
        RbTreeInsert(node);
        NormalizeDeltaIfNecessary();
    }

    public void Delete(IntervalNode node)
    {
        RbTreeDelete(node);
        NormalizeDeltaIfNecessary();
    }

    private void NormalizeDeltaIfNecessary()
    {
        if (!RequestNormalizeDelta)
        {
            return;
        }

        RequestNormalizeDelta = false;
        NormalizeDelta();
    }

    private void NormalizeDelta()
    {
        var node = Root;
        long delta = 0;
        while (node != Sentinel)
        {
            if (node.Left != Sentinel && !node.Left.IsVisited)
            {
                // go left
                node = node.Left;
                continue;
            }

            if (node.Right != Sentinel && !node.Right.IsVisited)
            {
                // go right
                delta += node.Delta;
                node = node.Right;
                continue;
            }

            // handle current node
            node.Start = delta + node.Start;
            node.End = delta + node.End;
            node.Delta = 0;
            RecomputeMaxEnd(node);

            node.IsVisited = true;

            // going up from this node
            node.Left.IsVisited = false;
            node.Right.IsVisited = false;
            if (node == node.Parent.Right)
            {
                delta -= node.Parent.Delta;
            }

            node = node.Parent;
        }

        Root.IsVisited = false;
    }
}

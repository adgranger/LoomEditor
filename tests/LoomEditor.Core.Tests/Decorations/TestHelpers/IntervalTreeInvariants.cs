// Ported from microsoft/vscode: src/vs/editor/test/common/model/intervalTree.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;

namespace LoomEditor.Core.Tests.Decorations.TestHelpers;

/// <summary>Port of Monaco's <c>assertTreeInvariants</c> helpers, plus an in-order walk that resolves absolute offsets.</summary>
internal static class IntervalTreeInvariants
{
    // Monaco: 'assertTreeInvariants'
    public static void AssertTreeInvariants(IntervalTree tree)
    {
        var sentinel = tree.Sentinel;
        Assert.Equal(NodeColor.Black, sentinel.Color);
        Assert.Same(sentinel, sentinel.Parent);
        Assert.Same(sentinel, sentinel.Left);
        Assert.Same(sentinel, sentinel.Right);
        Assert.Equal(0, sentinel.Start);
        Assert.Equal(0, sentinel.End);
        Assert.Equal(0, sentinel.Delta);
        Assert.Same(sentinel, tree.Root.Parent);
        AssertValidTree(tree);
    }

    // Monaco: 'depth'
    private static int Depth(IntervalTree tree, IntervalNode n)
    {
        if (n == tree.Sentinel)
        {
            // The leafs are black
            return 1;
        }

        var left = Depth(tree, n.Left);
        Assert.Equal(left, Depth(tree, n.Right));
        return (n.Color == NodeColor.Black ? 1 : 0) + left;
    }

    // Monaco: 'assertValidNode'
    private static void AssertValidNode(IntervalTree tree, IntervalNode n, long delta)
    {
        if (n == tree.Sentinel)
        {
            return;
        }

        var l = n.Left;
        var r = n.Right;

        if (n.Color == NodeColor.Red)
        {
            Assert.Equal(NodeColor.Black, l.Color);
            Assert.Equal(NodeColor.Black, r.Color);
        }

        var expectedMaxEnd = n.End;
        if (l != tree.Sentinel)
        {
            Assert.True(IntervalTree.IntervalCompare(l.Start + delta, l.End + delta, n.Start + delta, n.End + delta) <= 0);
            expectedMaxEnd = Math.Max(expectedMaxEnd, l.MaxEnd);
        }

        if (r != tree.Sentinel)
        {
            Assert.True(IntervalTree.IntervalCompare(n.Start + delta, n.End + delta, r.Start + delta + n.Delta, r.End + delta + n.Delta) <= 0);
            expectedMaxEnd = Math.Max(expectedMaxEnd, r.MaxEnd + n.Delta);
        }

        Assert.Equal(expectedMaxEnd, n.MaxEnd);

        AssertValidNode(tree, l, delta);
        AssertValidNode(tree, r, delta + n.Delta);
    }

    // Monaco: 'assertValidTree'
    public static void AssertValidTree(IntervalTree tree)
    {
        if (tree.Root == tree.Sentinel)
        {
            return;
        }

        Assert.Equal(NodeColor.Black, tree.Root.Color);
        Assert.Equal(Depth(tree, tree.Root.Left), Depth(tree, tree.Root.Right));
        AssertValidNode(tree, tree.Root, 0);
    }

    /// <summary>The absolute (start, end) of every node, in tree order.</summary>
    public static List<(long Start, long End)> InOrder(IntervalTree tree)
    {
        var result = new List<(long, long)>();
        Walk(tree, tree.Root, 0, result);
        return result;
    }

    private static void Walk(IntervalTree tree, IntervalNode n, long delta, List<(long, long)> result)
    {
        if (n == tree.Sentinel)
        {
            return;
        }

        Walk(tree, n.Left, delta, result);
        result.Add((n.Start + delta, n.End + delta));
        Walk(tree, n.Right, delta + n.Delta, result);
    }
}

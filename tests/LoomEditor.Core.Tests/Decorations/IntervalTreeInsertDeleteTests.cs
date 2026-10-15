// Ported from microsoft/vscode: src/vs/editor/test/common/model/intervalTree.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Tests.Decorations.TestHelpers;

namespace LoomEditor.Core.Tests.Decorations;

public class IntervalTreeInsertDeleteTests
{
    private static readonly (long Start, long End)[] CormenIntervals =
    [
        (16, 21), (8, 9), (25, 30), (5, 8), (15, 23), (17, 19), (26, 26), (0, 3), (6, 10), (19, 20),
    ];

    private static IntervalNode NewNode((long Start, long End) interval) => new("id", interval.Start, interval.End);

    private static int Compare((long Start, long End) a, (long Start, long End) b) =>
        a.Start == b.Start ? a.End.CompareTo(b.End) : a.Start.CompareTo(b.Start);

    [Fact]
    public void Insert_CormenIntervals_KeepsInvariantsAndOrder()
    {
        var tree = new IntervalTree();
        var inserted = new List<(long Start, long End)>();

        foreach (var interval in CormenIntervals)
        {
            tree.Insert(NewNode(interval));
            inserted.Add(interval);
            IntervalTreeInvariants.AssertTreeInvariants(tree);

            var expected = inserted.ToList();
            expected.Sort(Compare);
            Assert.Equal(expected, IntervalTreeInvariants.InOrder(tree));
        }
    }

    [Fact]
    public void Insert_EmptyTree_RootIsBlack()
    {
        var tree = new IntervalTree();
        var node = NewNode((3, 7));

        tree.Insert(node);

        Assert.Same(node, tree.Root);
        Assert.Equal(NodeColor.Black, node.Color);
        Assert.Same(tree.Sentinel, node.Parent);
        IntervalTreeInvariants.AssertTreeInvariants(tree);
    }

    [Fact]
    public void Delete_Root_KeepsInvariants()
    {
        var (tree, _) = BuildCormenTree();

        tree.Delete(tree.Root);

        IntervalTreeInvariants.AssertTreeInvariants(tree);
        Assert.Equal(CormenIntervals.Length - 1, IntervalTreeInvariants.InOrder(tree).Count);
    }

    [Fact]
    public void Delete_LeafAndNodeWithTwoChildren_KeepsInvariants()
    {
        var (tree, nodes) = BuildCormenTree();
        var remaining = nodes.ToList();

        var leaf = remaining.First(n => n.Left == tree.Sentinel && n.Right == tree.Sentinel);
        tree.Delete(leaf);
        remaining.Remove(leaf);
        IntervalTreeInvariants.AssertTreeInvariants(tree);

        var twoChildren = remaining.First(n => n.Left != tree.Sentinel && n.Right != tree.Sentinel);
        tree.Delete(twoChildren);
        remaining.Remove(twoChildren);
        IntervalTreeInvariants.AssertTreeInvariants(tree);

        Assert.Equal(CormenIntervals.Length - 2, IntervalTreeInvariants.InOrder(tree).Count);
    }

    [Fact]
    public void Delete_AllNodes_LeavesEmptyTree()
    {
        var (tree, nodes) = BuildCormenTree();

        foreach (var node in nodes)
        {
            tree.Delete(node);
            IntervalTreeInvariants.AssertTreeInvariants(tree);
        }

        Assert.Same(tree.Sentinel, tree.Root);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void InsertDelete_Randomized_MatchesOracle(int seed)
    {
        var random = new Random(seed);
        var tree = new IntervalTree();
        var live = new List<IntervalNode>();
        var oracle = new List<(long Start, long End)>();

        for (var step = 0; step < 200; step++)
        {
            var message = $"seed {seed}, step {step}";
            if (live.Count == 0 || random.Next(3) != 0)
            {
                long start = random.Next(0, 1000);
                long end = start + random.Next(0, 1000 - (int)start);
                var node = NewNode((start, end));
                tree.Insert(node);
                live.Add(node);
                oracle.Add((start, end));
            }
            else
            {
                var index = random.Next(live.Count);
                var node = live[index];
                live.RemoveAt(index);
                oracle.Remove((node.CachedAbsoluteStart, node.CachedAbsoluteEnd));
                tree.Delete(node);
            }

            IntervalTreeInvariants.AssertTreeInvariants(tree);
            var expected = oracle.ToList();
            expected.Sort(Compare);
            Assert.True(expected.SequenceEqual(IntervalTreeInvariants.InOrder(tree)), message);
        }
    }

    public static TheoryData<int> Seeds()
    {
        var data = new TheoryData<int>();
        for (var seed = 1; seed <= 20; seed++)
        {
            data.Add(seed);
        }

        return data;
    }

    private static (IntervalTree Tree, List<IntervalNode> Nodes) BuildCormenTree()
    {
        var tree = new IntervalTree();
        var nodes = new List<IntervalNode>();
        foreach (var interval in CormenIntervals)
        {
            var node = NewNode(interval);
            tree.Insert(node);
            nodes.Add(node);
        }

        return (tree, nodes);
    }
}

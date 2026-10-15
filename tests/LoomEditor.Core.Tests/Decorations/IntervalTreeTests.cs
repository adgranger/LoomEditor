// Ported from microsoft/vscode: src/vs/editor/test/common/model/intervalTree.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Tests.Decorations.TestHelpers;

namespace LoomEditor.Core.Tests.Decorations;

// Suite 'IntervalTree 1'.
public class IntervalTreeTests
{
    private static IntervalTreeOperation Insert(long begin, long end) => new("insert", begin, end);

    private static IntervalTreeOperation Delete(int id) => new("delete", Id: id);

    private static IntervalTreeOperation Search(long begin, long end) => new("search", begin, end);

    // Suite 'generated'.
    // Monaco: 'gen01'
    [Fact]
    public void Gen01_Monaco()
    {
        IntervalTreeTestState.Run(
            Insert(28, 35),
            Insert(52, 54),
            Insert(63, 69)
        );
    }

    // Monaco: 'gen02'
    [Fact]
    public void Gen02_Monaco()
    {
        IntervalTreeTestState.Run(
            Insert(80, 89),
            Insert(92, 100),
            Insert(99, 99)
        );
    }

    // Monaco: 'gen03'
    [Fact]
    public void Gen03_Monaco()
    {
        IntervalTreeTestState.Run(
            Insert(89, 96),
            Insert(71, 74),
            Delete(1)
        );
    }

    // Monaco: 'gen04'
    [Fact]
    public void Gen04_Monaco()
    {
        IntervalTreeTestState.Run(
            Insert(44, 46),
            Insert(85, 88),
            Delete(0)
        );
    }

    // Monaco: 'gen05'
    [Fact]
    public void Gen05_Monaco()
    {
        IntervalTreeTestState.Run(
            Insert(82, 90),
            Insert(69, 73),
            Delete(0),
            Delete(1)
        );
    }

    // Monaco: 'gen06'
    [Fact]
    public void Gen06_Monaco()
    {
        IntervalTreeTestState.Run(
            Insert(41, 63),
            Insert(98, 98),
            Insert(47, 51),
            Delete(2)
        );
    }

    // Monaco: 'gen07'
    [Fact]
    public void Gen07_Monaco()
    {
        IntervalTreeTestState.Run(
            Insert(24, 26),
            Insert(11, 28),
            Insert(27, 30),
            Insert(80, 85),
            Delete(1)
        );
    }

    // Monaco: 'gen08'
    [Fact]
    public void Gen08_Monaco()
    {
        IntervalTreeTestState.Run(
            Insert(100, 100),
            Insert(100, 100)
        );
    }

    // Monaco: 'gen09'
    [Fact]
    public void Gen09_Monaco()
    {
        IntervalTreeTestState.Run(
            Insert(58, 65),
            Insert(82, 96),
            Insert(58, 65)
        );
    }

    // Monaco: 'gen10'
    [Fact]
    public void Gen10_Monaco()
    {
        IntervalTreeTestState.Run(
            Insert(32, 40),
            Insert(25, 29),
            Insert(24, 32)
        );
    }

    // Monaco: 'gen11'
    [Fact]
    public void Gen11_Monaco()
    {
        IntervalTreeTestState.Run(
            Insert(25, 70),
            Insert(99, 100),
            Insert(46, 51),
            Insert(57, 57),
            Delete(2)
        );
    }

    // Monaco: 'gen12'
    [Fact]
    public void Gen12_Monaco()
    {
        IntervalTreeTestState.Run(
            Insert(20, 26),
            Insert(10, 18),
            Insert(99, 99),
            Insert(37, 59),
            Delete(2)
        );
    }

    // Monaco: 'gen13'
    [Fact]
    public void Gen13_Monaco()
    {
        IntervalTreeTestState.Run(
            Insert(3, 91),
            Insert(57, 57),
            Insert(35, 44),
            Insert(72, 81),
            Delete(2)
        );
    }

    // Monaco: 'gen14'
    [Fact]
    public void Gen14_Monaco()
    {
        IntervalTreeTestState.Run(
            Insert(58, 61),
            Insert(34, 35),
            Insert(56, 62),
            Insert(69, 78),
            Delete(0)
        );
    }

    // Monaco: 'gen15'
    [Fact]
    public void Gen15_Monaco()
    {
        IntervalTreeTestState.Run(
            Insert(63, 69),
            Insert(17, 24),
            Insert(3, 13),
            Insert(84, 94),
            Insert(18, 23),
            Insert(96, 98),
            Delete(1)
        );
    }

    // Monaco: 'gen16'
    [Fact]
    public void Gen16_Monaco()
    {
        IntervalTreeTestState.Run(
            Insert(27, 27),
            Insert(42, 87),
            Insert(42, 49),
            Insert(69, 71),
            Insert(20, 27),
            Insert(8, 9),
            Insert(42, 49),
            Delete(1)
        );
    }

    // Monaco: 'gen17'
    [Fact]
    public void Gen17_Monaco()
    {
        IntervalTreeTestState.Run(
            Insert(21, 23),
            Insert(83, 87),
            Insert(56, 58),
            Insert(1, 55),
            Insert(56, 59),
            Insert(58, 60),
            Insert(56, 65),
            Delete(1),
            Delete(0),
            Delete(6)
        );
    }

    // Monaco: 'gen18'
    [Fact]
    public void Gen18_Monaco()
    {
        IntervalTreeTestState.Run(
            Insert(25, 25),
            Insert(67, 79),
            Delete(0),
            Search(65, 75)
        );
    }

    // Monaco: 'force delta overflow'
    [Fact]
    public void ForceDeltaOverflow_Monaco()
    {
        IntervalTreeTestState.Run(
            Insert(686081138593427, 733009856502260),
            Insert(591031326181669, 591031326181672),
            Insert(940037682731896, 940037682731903),
            Insert(598413641151120, 598413641151128),
            Insert(800564156553344, 800564156553351),
            Insert(894198957565481, 894198957565491)
        );
    }

    // Suite 'searching'.
    private static IntervalTree CreateCormenTree()
    {
        var tree = new IntervalTree();
        (long, long)[] data = [(16, 21), (8, 9), (25, 30), (5, 8), (15, 23), (17, 19), (26, 26), (0, 3), (6, 10), (19, 20)];
        foreach (var (start, end) in data)
        {
            tree.Insert(new IntervalNode(string.Empty, start, end));
        }

        return tree;
    }

    private static void AssertIntervalSearch(IntervalTree tree, long start, long end, params (long, long)[] expected)
    {
        var actualNodes = tree.IntervalSearch(start, end, 0, false, false, 0, false);
        var actual = actualNodes.Select(n => (n.CachedAbsoluteStart, n.CachedAbsoluteEnd)).ToArray();
        Assert.Equal(expected, actual);
    }

    // Monaco: 'cormen 1->2'
    [Fact]
    public void Cormen_1To2_Monaco() => AssertIntervalSearch(CreateCormenTree(), 1, 2, (0, 3));

    // Monaco: 'cormen 4->8'
    [Fact]
    public void Cormen_4To8_Monaco() => AssertIntervalSearch(CreateCormenTree(), 4, 8, (5, 8), (6, 10), (8, 9));

    // Monaco: 'cormen 10->15'
    [Fact]
    public void Cormen_10To15_Monaco() => AssertIntervalSearch(CreateCormenTree(), 10, 15, (6, 10), (15, 23));

    // Monaco: 'cormen 21->25'
    [Fact]
    public void Cormen_21To25_Monaco() => AssertIntervalSearch(CreateCormenTree(), 21, 25, (15, 23), (16, 21), (25, 30));

    // Monaco: 'cormen 24->24'
    [Fact]
    public void Cormen_24To24_Monaco() => AssertIntervalSearch(CreateCormenTree(), 24, 24);

    [Fact]
    public void SearchAndIntervalSearch_Always_ClearVisitedBits()
    {
        var tree = CreateCormenTree();

        tree.Search(0, false, false, 0, false);
        tree.IntervalSearch(4, 8, 0, false, false, 0, false);
        tree.CollectNodesFromOwner(0);
        tree.CollectNodesPostOrder();

        Assert.All(tree.GetAllInOrder(), n => Assert.False(n.IsVisited));
        Assert.False(tree.Sentinel.IsVisited);
    }

    [Fact]
    public void ResolveNode_CormenTree_MatchesGetAllInOrder()
    {
        var tree = CreateCormenTree();
        var nodes = tree.CollectNodesPostOrder();
        var expected = nodes.ToDictionary(n => n, n => (n.CachedAbsoluteStart, n.CachedAbsoluteEnd));
        _ = tree.GetAllInOrder();
        var inOrder = tree.GetAllInOrder().ToDictionary(n => n, n => (n.CachedAbsoluteStart, n.CachedAbsoluteEnd));

        foreach (var node in nodes)
        {
            node.CachedAbsoluteStart = -1;
            node.CachedAbsoluteEnd = -1;
            tree.ResolveNode(node, 0);
            Assert.Equal(inOrder[node], (node.CachedAbsoluteStart, node.CachedAbsoluteEnd));
        }

        Assert.Equal(expected.Count, inOrder.Count);
    }
}

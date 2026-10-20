using LoomEditor.Core.Model.BracketPairs.Tree;
using LoomEditor.Core.Tests.Model.BracketPairs.Tree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.BracketPairs.Tree;

// The 'Clone' test of the same Monaco suite is in ListAstNodeTests.
public class Concat23TreesTests
{
    private static TextAstNode Text() => new(Length.Of(1, 1));

    // Monaco: 'Bracket Pair Colorizer - mergeItems' > testMerge helper
    private static void TestMerge(List<AstNode> lists, string message = "merge23Trees failed")
    {
        var node = (Concat23Trees.Concat([.. lists.Select(l => l.DeepClone())]) ?? ListAstNode.Create([])).FlattenLists();

        // This trivial merge does not maintain the (2,3) tree invariant.
        var referenceNode = ListAstNode.Create([.. lists]).FlattenLists();

        Assert.True(AstAssert.StructurallyEqual(node, referenceNode), message);
    }

    // Monaco: 'Bracket Pair Colorizer - mergeItems' > 'Empty List'
    [Fact]
    public void EmptyList()
    {
        TestMerge([]);
    }

    // Monaco: 'Bracket Pair Colorizer - mergeItems' > 'Same Height Lists'
    [Fact]
    public void SameHeightLists()
    {
        var textNode = Text();
        var tree = ListAstNode.Create([textNode.DeepClone(), textNode.DeepClone()]);

        TestMerge([tree.DeepClone(), tree.DeepClone(), tree.DeepClone(), tree.DeepClone(), tree.DeepClone()]);
    }

    // Monaco: 'Bracket Pair Colorizer - mergeItems' > 'Different Height Lists 1'
    [Fact]
    public void DifferentHeightLists1()
    {
        var textNode = Text();
        var tree1 = ListAstNode.Create([textNode.DeepClone(), textNode.DeepClone()]);
        var tree2 = ListAstNode.Create([tree1.DeepClone(), tree1.DeepClone()]);

        TestMerge([tree1, tree2]);
    }

    // Monaco: 'Bracket Pair Colorizer - mergeItems' > 'Different Height Lists 2'
    [Fact]
    public void DifferentHeightLists2()
    {
        var textNode = Text();
        var tree1 = ListAstNode.Create([textNode.DeepClone(), textNode.DeepClone()]);
        var tree2 = ListAstNode.Create([tree1.DeepClone(), tree1.DeepClone()]);

        TestMerge([tree2, tree1]);
    }

    // Monaco: 'Bracket Pair Colorizer - mergeItems' > 'Different Height Lists 3'
    [Fact]
    public void DifferentHeightLists3()
    {
        var textNode = Text();
        var tree1 = ListAstNode.Create([textNode.DeepClone(), textNode.DeepClone()]);
        var tree2 = ListAstNode.Create([tree1.DeepClone(), tree1.DeepClone()]);

        TestMerge([tree2, tree1, tree1, tree2, tree2]);
    }

    [Fact]
    public void ConcatOfSameHeight_SevenLeaves_GivesBalancedTreeOfHeight2()
    {
        List<AstNode> leaves = [.. Enumerable.Range(0, 7).Select(_ => (AstNode)Text())];

        var root = Concat23Trees.ConcatOfSameHeight(leaves);

        Assert.NotNull(root);
        Assert.Equal(2, root.ListHeight);
        Assert.Equal(3, root.ChildrenLength);
        Assert.All(root.Children, c => Assert.Equal(1, c.ListHeight));
        Assert.Equal(Length.Of(7, 1), root.Length);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(19)]
    [InlineData(20)]
    public void Concat_ManyRandomHeights_FlattensToSameLeaves(int seed)
    {
        var random = new Random(seed);
        List<AstNode> items = [];
        var count = random.Next(1, 12);
        for (var i = 0; i < count; i++)
        {
            items.Add(Build(random.Next(0, 4)));
        }

        TestMerge(items, $"merge23Trees failed (seed {seed})");
    }

    private static AstNode Build(int height)
    {
        return height == 0 ? Text() : ListAstNode.Create([Build(height - 1), Build(height - 1)]);
    }
}

using LoomEditor.Core.Model.BracketPairs.Tree;
using LoomEditor.Core.Tests.Model.BracketPairs.Tree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.BracketPairs.Tree;

public class ListAstNodeTests
{
    private static TextAstNode Text(int lines = 1, int cols = 1) => new(Length.Of(lines, cols));

    // Monaco: 'Bracket Pair Colorizer - mergeItems' > 'Clone'
    [Fact]
    public void Clone()
    {
        var tree = ListAstNode.Create([Text(), Text()]);

        Assert.True(AstAssert.StructurallyEqual(tree, tree.DeepClone()));
    }

    [Fact]
    public void Create23_DifferentHeights_Throws()
    {
        var list = ListAstNode.Create([Text(), Text()]);

        Assert.Throws<InvalidOperationException>(() => ListAstNode.Create23(Text(), list, null));
    }

    [Fact]
    public void Immutable23_AppendChild_Throws()
    {
        var node = ListAstNode.Create23(Text(), Text(), null, immutable: true);

        Assert.Throws<InvalidOperationException>(() => node.AppendChildOfSameHeight(Text()));
    }

    [Fact]
    public void ImmutableArray_ToMutable_ReturnsMutableCopy()
    {
        var immutable = ListAstNode.Create([Text(0, 2), Text(0, 3)], immutable: true);

        var mutable = immutable.ToMutable();
        mutable.AppendChildOfSameHeight(Text(0, 4));

        Assert.NotSame(immutable, mutable);
        Assert.Equal(3, mutable.ChildrenLength);
        Assert.Equal(Length.Of(0, 9), mutable.Length);
        Assert.Equal(2, immutable.ChildrenLength);
        Assert.Equal(Length.Of(0, 5), immutable.Length);
    }

    [Fact]
    public void TwoThree_PrependThenUnprepend_RestoresChildrenAndLength()
    {
        var node = ListAstNode.Create23(Text(0, 2), Text(0, 3), null);
        var first = node.GetChild(0);

        node.PrependChildOfSameHeight(Text(0, 4));
        Assert.Equal(3, node.ChildrenLength);
        Assert.Equal(Length.Of(0, 9), node.Length);

        var removed = node.UnprependChild();
        Assert.Equal(Length.Of(0, 4), removed!.Length);
        Assert.Equal(2, node.ChildrenLength);
        Assert.Same(first, node.GetChild(0));
        Assert.Equal(Length.Of(0, 5), node.Length);
    }

    [Fact]
    public void HandleChildrenChanged_RecomputesLengthAndMissingIds()
    {
        var invalid = new InvalidBracketAstNode(SmallImmutableSet<int>.Empty.Add(2, IdentityKeyProvider.Instance), Length.Of(0, 1));
        var list = ListAstNode.Create([Text(0, 2)]);
        Assert.False(list.MissingOpeningBracketIds.Has(2, IdentityKeyProvider.Instance));

        list.AppendChildOfSameHeight(invalid);

        Assert.Equal(Length.Of(0, 3), list.Length);
        Assert.True(list.MissingOpeningBracketIds.Has(2, IdentityKeyProvider.Instance));
    }

    [Fact]
    public void MakeLastElementMutable_ImmutableChildList_IsReplacedByMutableCopy()
    {
        var child = ListAstNode.Create([Text(), Text()], immutable: true);
        var parent = ListAstNode.Create([child]);

        var mutable = parent.MakeLastElementMutable();

        Assert.NotSame(child, mutable);
        Assert.Same(mutable, parent.GetChild(0));
        Assert.IsType<ArrayListAstNode>(mutable);
    }

    [Fact]
    public void CanBeReused_EmptyList_IsFalse()
    {
        Assert.False(ListAstNode.GetEmpty().CanBeReused(SmallImmutableSet<int>.Empty));
    }
}

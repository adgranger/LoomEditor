using LoomEditor.Core.Model.BracketPairs.Tree;

namespace LoomEditor.Core.Tests.Model.BracketPairs.Tree;

// Monaco has no test for nodeReader.ts alone (it is covered through the parser tests).
public class NodeReaderTests
{
    private static TextAstNode Text(int columns) => new(Length.Of(0, columns));

    private static ListAstNode ThreeTexts(out TextAstNode a, out TextAstNode b, out TextAstNode c)
    {
        a = Text(2);
        b = Text(3);
        c = Text(4);
        return ListAstNode.Create([a, b, c]);
    }

    [Fact]
    public void ReadLongestNodeAt_OffsetAtNodeStart_ReturnsLongestMatchingNode()
    {
        var list = ThreeTexts(out var a, out var b, out var c);
        var reader = new NodeReader(list);

        Assert.Same(a, reader.ReadLongestNodeAt(Length.Zero, n => n.Kind == AstNodeKind.Text));
        Assert.Same(b, reader.ReadLongestNodeAt(Length.Of(0, 2), n => n.Kind == AstNodeKind.Text));
        Assert.Same(c, reader.ReadLongestNodeAt(Length.Of(0, 5), n => n.Kind == AstNodeKind.Text));

        Assert.Same(list, new NodeReader(list).ReadLongestNodeAt(Length.Zero, _ => true));
    }

    [Fact]
    public void ReadLongestNodeAt_OffsetGoesBackwards_Throws()
    {
        var reader = new NodeReader(ThreeTexts(out _, out _, out _));
        reader.ReadLongestNodeAt(Length.Of(0, 5), _ => true);

        Assert.Throws<InvalidOperationException>(() => reader.ReadLongestNodeAt(Length.Zero, _ => true));
    }

    [Fact]
    public void ReadLongestNodeAt_PredicateRejectsAll_ReturnsNull()
    {
        var reader = new NodeReader(ThreeTexts(out _, out _, out _));

        Assert.Null(reader.ReadLongestNodeAt(Length.Zero, _ => false));
    }
}

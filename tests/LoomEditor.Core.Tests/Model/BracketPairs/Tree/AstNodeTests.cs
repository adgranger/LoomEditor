using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.BracketPairs.Tree;

namespace LoomEditor.Core.Tests.Model.BracketPairs.Tree;

// Monaco has no test that targets these classes alone (covered by the later bracket pair tree cards).
public class AstNodeTests
{
    private static readonly LanguageBracketsConfiguration Config = new(
        "test",
        new LanguageConfiguration { Brackets = [new CharacterPair("{", "}")] });

    private static BracketAstNode Open() =>
        BracketAstNode.Create(Length.Of(0, 1), Config.GetOpeningBracketInfo("{")!, SmallImmutableSet<int>.Empty.Add(0, IdentityKeyProvider.Instance));

    private static BracketAstNode Close() =>
        BracketAstNode.Create(Length.Of(0, 1), Config.GetClosingBracketInfo("}")!, SmallImmutableSet<int>.Empty.Add(0, IdentityKeyProvider.Instance));

    private static SmallImmutableSet<int> Set(params int[] keys)
    {
        var set = SmallImmutableSet<int>.Empty;
        foreach (var key in keys)
        {
            set = set.Add(key, IdentityKeyProvider.Instance);
        }

        return set;
    }

    [Fact]
    public void PairAstNode_Create_SumsLengthsOfBracketsAndChild()
    {
        var pair = PairAstNode.Create(Open(), new TextAstNode(Length.Of(0, 3)), Close());

        Assert.Equal(Length.Of(0, 5), pair.Length);
        Assert.Equal(3, pair.Children.Count);
    }

    [Fact]
    public void PairAstNode_Unclosed_CannotBeReused()
    {
        var pair = PairAstNode.Create(Open(), null, null);

        Assert.False(pair.CanBeReused(SmallImmutableSet<int>.Empty));
    }

    [Fact]
    public void PairAstNode_MissingIdsIntersectOpenIds_CannotBeReused()
    {
        var child = new InvalidBracketAstNode(Set(3), Length.Of(0, 1));
        var pair = PairAstNode.Create(Open(), child, Close());

        Assert.False(pair.CanBeReused(Set(3)));
        Assert.True(pair.CanBeReused(Set(4)));
    }

    [Fact]
    public void InvalidBracketAstNode_CanBeReused_OnlyWhenNoOpenedBracketMatches()
    {
        var node = new InvalidBracketAstNode(Set(1, 2), Length.Of(0, 1));

        Assert.False(node.CanBeReused(Set(2)));
        Assert.True(node.CanBeReused(Set(5)));
    }

    [Fact]
    public void BracketAstNode_CanNeverBeReused()
    {
        Assert.False(Open().CanBeReused(SmallImmutableSet<int>.Empty));
    }

    [Fact]
    public void TextAstNode_CanAlwaysBeReused()
    {
        Assert.True(new TextAstNode(Length.Of(0, 3)).CanBeReused(Set(0, 1)));
    }

    [Fact]
    public void TextAstNode_ComputeMinIndentation_SkipsFirstPartialLine()
    {
        var model = new TextModel("ab\n    c\n  d");
        var node = new TextAstNode(Length.Of(2, 3));

        Assert.Equal(2, node.ComputeMinIndentation(Length.Of(0, 1), model));
        Assert.Equal(0, new TextAstNode(Length.Of(2, 3)).ComputeMinIndentation(Length.Of(0, 0), model));
    }

    [Fact]
    public void Leaf_DeepClone_ReturnsSameInstance()
    {
        var text = new TextAstNode(Length.Of(0, 3));
        var bracket = Open();

        Assert.Same(text, text.DeepClone());
        Assert.Same(bracket, bracket.DeepClone());
        Assert.Same(bracket, bracket.FlattenLists());
    }

    [Fact]
    public void PairAstNode_GetChild_InvalidIndex_Throws()
    {
        var pair = PairAstNode.Create(Open(), null, null);

        Assert.Throws<ArgumentOutOfRangeException>(() => pair.GetChild(3));
        Assert.Null(pair.GetChild(1));
    }
}

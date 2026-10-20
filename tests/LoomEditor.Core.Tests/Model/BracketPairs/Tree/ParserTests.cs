using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.BracketPairs.Tree;
using LoomEditor.Core.Tests.Model.BracketPairs.TestHelpers;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Model.BracketPairs.Tree;

// No upstream test targets parser.ts alone: it is covered by the tests of the bracket pairs tree.
public class ParserTests
{
    private const string Mode1 = "testMode1";

    private static readonly CharacterPair[] AllBrackets =
    [
        new("{", "}"),
        new("(", ")"),
        new("[", "]"),
    ];

    private static readonly List<TextEditInfo> NoEdits = [];

    // Every fixture registers Mode1 first in a new codec, so the id is the same in all of them.
    private static readonly int EncodedMode1 = CreateCodec().EncodeLanguageId(Mode1);

    private static LanguageIdCodec CreateCodec()
    {
        var codec = new LanguageIdCodec();
        codec.Register(Mode1);
        return codec;
    }

    private static LanguageAgnosticBracketTokens CreateBrackets(LanguageConfigurationRegistry configurations)
    {
        return new LanguageAgnosticBracketTokens(new DenseKeyProvider<string>(), configurations.GetLanguageConfiguration);
    }

    private static LanguageConfigurationRegistry CreateConfigurations()
    {
        var configurations = new LanguageConfigurationRegistry();
        configurations.Register(Mode1, new LanguageConfiguration { Brackets = AllBrackets });
        return configurations;
    }

    private static FastTokenizer CreateFastTokenizer(string text)
    {
        var brackets = CreateBrackets(CreateConfigurations());
        return new FastTokenizer(text, brackets.GetSingleLanguageBracketTokens(Mode1));
    }

    // The tokenizer must support Skip, which FastTokenizer does not.
    private static TextBufferTokenizer CreateTextBufferTokenizer(string text)
    {
        var codec = CreateCodec();
        var document = new TokenizedDocument([new TokenInfo(text, EncodedMode1, StandardTokenType.Other, true)]);

        var tokenization = new TokenizationRegistry();
        tokenization.Register(Mode1, document.GetTokenizationSupport());

        var configurations = CreateConfigurations();
        var model = new TextModel(document.GetText(), DefaultEndOfLine.LF, null, tokenization, codec, null, configurations);
        model.Tokenization.SetLanguageId(Mode1);
        model.Tokenization.ForceTokenization(model.LineCount);

        return new TextBufferTokenizer(model, CreateBrackets(configurations));
    }

    // Lists are flattened: only text (t<lines>:<columns>), brackets, invalid brackets (!) and pairs (<...>) are shown.
    private static string Dump(AstNode node)
    {
        switch (node.Kind)
        {
            case AstNodeKind.Text:
                var length = node.Length.ToObj();
                return $"t{length.LineCount}:{length.ColumnCount}";
            case AstNodeKind.Bracket:
                return ((BracketAstNode)node).Text;
            case AstNodeKind.UnexpectedClosingBracket:
                return "!";
            case AstNodeKind.Pair:
                return "<" + string.Join('|', node.Children.Select(Dump)) + ">";
            default:
                return string.Join('|', node.Children.Select(Dump));
        }
    }

    [Fact]
    public void ParseDocument_FastTokenizer_BuildsNestedPairs()
    {
        const string text = "{ ( [] ) }";
        var tokenizer = CreateFastTokenizer(text);

        var root = BracketPairParser.ParseDocument(tokenizer, NoEdits, null, false);

        Assert.Equal("<{|t0:1|<(|t0:1|<[|]>|t0:1|)>|t0:1|}>", Dump(root));
        Assert.Equal(Length.Of(0, text.Length), root.Length);
        var outer = Assert.IsType<PairAstNode>(root);
        Assert.NotNull(outer.ClosingBracket);
        var middle = Assert.IsType<PairAstNode>(outer.Child!.Children[1]);
        var inner = Assert.IsType<PairAstNode>(middle.Child!.Children[1]);
        Assert.Null(inner.Child);
        Assert.NotNull(inner.ClosingBracket);
    }

    [Fact]
    public void ParseDocument_UnclosedBracket_PairWithoutClosingBracket()
    {
        var root = BracketPairParser.ParseDocument(CreateFastTokenizer("{ a"), NoEdits, null, false);

        var pair = Assert.IsType<PairAstNode>(root);
        Assert.Null(pair.ClosingBracket);
        Assert.Equal("<{|t0:2>", Dump(root));
        Assert.Equal(Length.Of(0, 3), root.Length);
    }

    [Fact]
    public void ParseDocument_UnexpectedClosingBracket_IsInvalidBracketNode()
    {
        var root = BracketPairParser.ParseDocument(CreateFastTokenizer("a } b"), NoEdits, null, false);

        Assert.Equal("t0:2|!|t0:2", Dump(root));
        Assert.Equal(AstNodeKind.List, root.Kind);
        Assert.Equal(AstNodeKind.UnexpectedClosingBracket, root.Children[1].Kind);
        Assert.False(root.MissingOpeningBracketIds.Equals(SmallImmutableSet<int>.Empty));
    }

    [Fact]
    public void ParseDocument_MismatchedClosing_ClosesOuterPair()
    {
        var root = BracketPairParser.ParseDocument(CreateFastTokenizer("{ ( }"), NoEdits, null, false);

        // The "(" pair is unclosed, the "{" pair is closed by the "}".
        Assert.Equal("<{|t0:1|<(|t0:1>|}>", Dump(root));
        var outer = Assert.IsType<PairAstNode>(root);
        Assert.NotNull(outer.ClosingBracket);
        var inner = Assert.IsType<PairAstNode>(outer.Child!.Children[1]);
        Assert.Null(inner.ClosingBracket);
    }

    [Fact]
    public void ParseDocument_10000OpeningBrackets_DoesNotOverflowTheStack()
    {
        var text = new string('{', 10000);

        var root = BracketPairParser.ParseDocument(CreateFastTokenizer(text), NoEdits, null, false);

        Assert.Equal(Length.Of(0, text.Length), root.Length);

        // Nesting stops at level 300: the first deeper opening bracket is plain text.
        var depth = 0;
        var node = root;
        while (node is PairAstNode pair && pair.Child is not null)
        {
            depth++;
            node = pair.Child;
        }

        Assert.InRange(depth, 100, 300);
    }

    [Fact]
    public void ParseDocument_CreateImmutableLists_ListsAreImmutable()
    {
        var root = BracketPairParser.ParseDocument(CreateFastTokenizer("{ } { } { } { }"), NoEdits, null, true);

        Assert.IsType<Immutable23ListAstNode>(root);
        Assert.Throws<InvalidOperationException>(() => ((ListAstNode)root).AppendChildOfSameHeight(new TextAstNode(Length.Of(0, 1))));
    }

    [Fact]
    public void ParseDocument_EmptyText_IsAnEmptyList()
    {
        var root = BracketPairParser.ParseDocument(CreateFastTokenizer(string.Empty), NoEdits, null, false);

        Assert.Equal(AstNodeKind.List, root.Kind);
        Assert.Equal(0, root.ChildrenLength);
        Assert.True(root.Length.IsZero);
    }

    [Fact]
    public void Constructor_OldNodeWithImmutableLists_Throws()
    {
        var oldNode = BracketPairParser.ParseDocument(CreateFastTokenizer("{ }"), NoEdits, null, false);

        Assert.Throws<InvalidOperationException>(
            () => new BracketPairParser.Parser(CreateTextBufferTokenizer("{ }"), NoEdits, oldNode, true));
    }

    [Fact]
    public void ParseDocument_WithOldNodeAndNoEdit_ReusesNodes()
    {
        const string text = "{ a } { b } { c }";
        var fresh = BracketPairParser.ParseDocument(CreateFastTokenizer(text), NoEdits, null, false);

        var parser = new BracketPairParser.Parser(CreateTextBufferTokenizer(text), NoEdits, fresh, false);
        var reparsed = parser.ParseDocument();

        Assert.True(parser.NodesReused > 0);
        Assert.Equal(0, parser.NodesConstructed);
        Assert.Equal(Dump(fresh), Dump(reparsed));
        Assert.Equal(fresh.Length, reparsed.Length);
    }

    [Fact]
    public void ParseDocument_WithOldNodeAndEdit_ReusesUntouchedPairsOnly()
    {
        var oldNode = BracketPairParser.ParseDocument(CreateFastTokenizer("{ a } { b } { c }"), NoEdits, null, false);
        const string newText = "{ a } { bx } { c }";
        var expected = BracketPairParser.ParseDocument(CreateFastTokenizer(newText), NoEdits, null, false);

        // Inserts "x" after the "b".
        List<TextEditInfo> edits = [new(Length.Of(0, 9), Length.Of(0, 9), Length.Of(0, 1))];
        var parser = new BracketPairParser.Parser(CreateTextBufferTokenizer(newText), edits, oldNode, false);
        var reparsed = parser.ParseDocument();

        Assert.True(parser.NodesReused > 0);
        Assert.True(parser.NodesConstructed > 0);
        Assert.Equal(Dump(expected), Dump(reparsed));
        Assert.Equal(expected.Length, reparsed.Length);
    }
}

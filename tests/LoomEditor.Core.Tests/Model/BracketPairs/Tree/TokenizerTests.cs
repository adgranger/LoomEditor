// Ported from microsoft/vscode: src/vs/editor/test/common/model/bracketPairColorizer/tokenizer.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.BracketPairs.Tree;
using LoomEditor.Core.Tests.Model.BracketPairs.TestHelpers;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Model.BracketPairs.Tree;

// Monaco: suite 'Bracket Pair Colorizer - Tokenizer'
public class TokenizerTests
{
    private const string Mode1 = "testMode1";

    // Every fixture registers Mode1 first in a new codec, so the id is the same in all of them.
    private static readonly int EncodedMode1 = CreateCodec().EncodeLanguageId(Mode1);

    private static readonly CharacterPair[] CurlyBrackets = [new CharacterPair("{", "}")];

    private static LanguageIdCodec CreateCodec()
    {
        var codec = new LanguageIdCodec();
        codec.Register(Mode1);
        return codec;
    }

    private sealed class Fixture
    {
        public Fixture(TokenizedDocument document, params CharacterPair[] brackets)
        {
            var codec = CreateCodec();

            var tokenization = new TokenizationRegistry();
            tokenization.Register(Mode1, document.GetTokenizationSupport());

            var configurations = new LanguageConfigurationRegistry();
            configurations.Register(Mode1, new LanguageConfiguration { Brackets = brackets });

            Model = new TextModel(document.GetText(), DefaultEndOfLine.LF, null, tokenization, codec, null, configurations);
            Model.Tokenization.SetLanguageId(Mode1);
            Model.Tokenization.ForceTokenization(Model.LineCount);

            DenseKeyProvider = new DenseKeyProvider<string>();
            Brackets = new LanguageAgnosticBracketTokens(DenseKeyProvider, configurations.GetLanguageConfiguration);
        }

        public TextModel Model { get; }

        public DenseKeyProvider<string> DenseKeyProvider { get; }

        public LanguageAgnosticBracketTokens Brackets { get; }
    }

    private static TokenInfo Standard(string text) => new(text, EncodedMode1, StandardTokenType.Other, true);

    private static TokenInfo Comment(string text) => new(text, EncodedMode1, StandardTokenType.Comment, true);

    private static TokenizedDocument Document(params TokenInfo[] tokens) => new(tokens);

    // Monaco: 'Basic'
    [Fact]
    public void Basic()
    {
        var document = Document(
            Standard(" { } "), Standard("be"), Standard("gin end"), Standard("\n"),
            Standard("hello"), Comment("{"), Standard("}"));
        var fixture = new Fixture(
            document,
            new CharacterPair("{", "}"),
            new CharacterPair("[", "]"),
            new CharacterPair("(", ")"),
            new CharacterPair("begin", "end"));

        var tokens = ReadAllTokens(new TextBufferTokenizer(fixture.Model, fixture.Brackets));

        Assert.Equal(
            [
                "Text ' ' bracketId=null bracketIds=[]",
                "OpeningBracket '{' bracketId=testMode1:::{ bracketIds=[testMode1:::{]",
                "Text ' ' bracketId=null bracketIds=[]",
                "ClosingBracket '}' bracketId=testMode1:::{ bracketIds=[testMode1:::{]",
                "Text ' ' bracketId=null bracketIds=[]",
                "OpeningBracket 'begin' bracketId=testMode1:::begin bracketIds=[testMode1:::begin]",
                "Text ' ' bracketId=null bracketIds=[]",
                "ClosingBracket 'end' bracketId=testMode1:::begin bracketIds=[testMode1:::begin]",
                "Text '\\nhello{' bracketId=null bracketIds=[]",
                "ClosingBracket '}' bracketId=testMode1:::{ bracketIds=[testMode1:::{]",
            ],
            ToArr(tokens, fixture.Model, fixture.DenseKeyProvider));
    }

    [Fact]
    public void FastTokenizer_SplitsTextAndBrackets()
    {
        var fixture = new Fixture(Document(), CurlyBrackets[0]);
        var brackets = fixture.Brackets.GetSingleLanguageBracketTokens(Mode1);

        var tokenizer = new FastTokenizer("a{b\n}c", brackets);
        var tokens = ReadAllTokens(tokenizer);

        Assert.Equal(
            [
                (TokenKind.Text, Length.Of(0, 1)),
                (TokenKind.OpeningBracket, Length.Of(0, 1)),
                (TokenKind.Text, Length.Of(1, 0)),
                (TokenKind.ClosingBracket, Length.Of(0, 1)),
                (TokenKind.Text, Length.Of(0, 1)),
            ],
            tokens.Select(t => (t.Kind, t.Length)));
        Assert.Equal(Length.Of(1, 2), tokenizer.Length);
        Assert.Equal("a{b\n}c", tokenizer.GetText());
        Assert.Null(tokenizer.Read());
        Assert.Null(tokenizer.Peek());
    }

    [Fact]
    public void FastTokenizer_NoBrackets_IsOneTextToken()
    {
        var fixture = new Fixture(Document(), CurlyBrackets[0]);
        var brackets = fixture.Brackets.GetSingleLanguageBracketTokens(Mode1);

        var tokenizer = new FastTokenizer("ab\ncd", brackets);

        var peeked = tokenizer.Peek();
        var token = tokenizer.Read();
        Assert.NotNull(token);
        Assert.Same(peeked, token);
        Assert.Equal(TokenKind.Text, token.Kind);
        Assert.Equal(Length.Of(1, 2), token.Length);
        Assert.IsType<TextAstNode>(token.AstNode);
        Assert.Null(tokenizer.Read());
        Assert.Equal(Length.Of(1, 2), tokenizer.Length);
    }

    [Fact]
    public void FastTokenizer_Skip_Throws()
    {
        var fixture = new Fixture(Document(), CurlyBrackets[0]);
        var tokenizer = new FastTokenizer("a{b}", fixture.Brackets.GetSingleLanguageBracketTokens(Mode1));

        Assert.Throws<NotSupportedException>(() => tokenizer.Skip(Length.Of(0, 1)));
    }

    [Fact]
    public void TextBufferTokenizer_CommentToken_HasNoBrackets()
    {
        var document = Document(Standard("a "), Comment("{"), Standard(" b"));
        var fixture = new Fixture(document, CurlyBrackets[0]);

        var tokenizer = new TextBufferTokenizer(fixture.Model, fixture.Brackets);
        var tokens = ReadAllTokens(tokenizer);

        var token = Assert.Single(tokens);
        Assert.Equal(TokenKind.Text, token.Kind);
        Assert.Equal(Length.Of(0, 5), token.Length);
        Assert.Equal(Length.Of(0, 5), tokenizer.Length);
        Assert.Equal(Length.Of(0, 5), tokenizer.Offset);
        Assert.Equal("a { b", tokenizer.GetText());
    }

    [Fact]
    public void TextBufferTokenizer_LongLine_TextTokensAreCutAt1500()
    {
        var chunks = new List<TokenInfo>();
        for (var i = 0; i < 40; i++)
        {
            // Alternating metadata, so that the identical tokens are not merged.
            chunks.Add(new TokenInfo(new string('a', 100), EncodedMode1, StandardTokenType.Other, i % 2 == 0));
        }

        var fixture = new Fixture(Document([.. chunks]), CurlyBrackets[0]);

        var tokens = ReadAllTokens(new TextBufferTokenizer(fixture.Model, fixture.Brackets));

        Assert.All(tokens, t => Assert.Equal(TokenKind.Text, t.Kind));
        Assert.Equal([1600, 1600, 800], tokens.Select(t => t.Length.GetColumnCountIfZeroLineCount()));
    }

    [Fact]
    public void TextBufferTokenizer_PeekThenRead_ReturnsTheSameTokenAndSkipMovesTheOffset()
    {
        var document = Document(Standard("a{b}c"));
        var fixture = new Fixture(document, CurlyBrackets[0]);
        var tokenizer = new TextBufferTokenizer(fixture.Model, fixture.Brackets);

        var peeked = tokenizer.Peek();
        Assert.Equal(Length.Zero, tokenizer.Offset);
        Assert.Same(peeked, tokenizer.Peek());
        Assert.Same(peeked, tokenizer.Read());
        Assert.Equal(Length.Of(0, 1), tokenizer.Offset);

        // Skips the "{".
        tokenizer.Skip(Length.Of(0, 1));
        Assert.Equal(Length.Of(0, 2), tokenizer.Offset);

        var text = tokenizer.Peek();
        Assert.NotNull(text);
        Assert.Equal(TokenKind.Text, text.Kind);
        Assert.Equal(Length.Of(0, 1), text.Length);
        Assert.Same(text, tokenizer.Read());

        var closing = tokenizer.Peek();
        Assert.NotNull(closing);
        Assert.Equal(TokenKind.ClosingBracket, closing.Kind);
        Assert.Same(closing, tokenizer.Read());
        Assert.Equal(Length.Of(0, 4), tokenizer.Offset);
    }

    private static List<Token> ReadAllTokens(ITokenizer tokenizer)
    {
        var tokens = new List<Token>();
        while (true)
        {
            var token = tokenizer.Read();
            if (token is null)
            {
                break;
            }

            tokens.Add(token);
        }

        return tokens;
    }

    private static List<string> ToArr(List<Token> tokens, TextModel model, DenseKeyProvider<string> keyProvider)
    {
        var result = new List<string>();
        var offset = Length.Zero;
        foreach (var token in tokens)
        {
            result.Add(TokenToObj(token, offset, model, keyProvider));
            offset = Length.Add(offset, token.Length);
        }

        return result;
    }

    private static string TokenToObj(Token token, Length offset, TextModel model, DenseKeyProvider<string> keyProvider)
    {
        var text = model.GetValueInRange(Length.ToRange(offset, Length.Add(offset, token.Length))).Replace("\n", "\\n", StringComparison.Ordinal);
        var bracketId = keyProvider.ReverseLookup(token.BracketId) ?? "null";
        var bracketIds = string.Join(",", keyProvider.ReverseLookupSet(token.BracketIds));
        return $"{token.Kind} '{text}' bracketId={bracketId} bracketIds=[{bracketIds}]";
    }
}

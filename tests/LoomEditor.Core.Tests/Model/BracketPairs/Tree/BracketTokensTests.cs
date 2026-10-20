using LoomEditor.Core.Languages;
using LoomEditor.Core.Model.BracketPairs.Tree;

namespace LoomEditor.Core.Tests.Model.BracketPairs.Tree;

// Monaco: suite 'Bracket Pair Colorizer - Brackets'
public class BracketTokensTests
{
    private const string LanguageId = "testMode1";

    private sealed record TokenView(string Text, Length Length, TokenKind Kind, int BracketId, SmallImmutableSet<int> BracketIds);

    private static LanguageAgnosticBracketTokens Create(DenseKeyProvider<string> denseKeyProvider, params CharacterPair[] brackets)
    {
        var registry = new LanguageConfigurationRegistry();
        registry.Register(LanguageId, new LanguageConfiguration { Brackets = brackets });
        return new LanguageAgnosticBracketTokens(denseKeyProvider, registry.GetLanguageConfiguration);
    }

    private static BracketTokens CreateSingle(params CharacterPair[] brackets)
    {
        return Create(new DenseKeyProvider<string>(), brackets).GetSingleLanguageBracketTokens(LanguageId);
    }

    // Monaco: 'Basic'
    [Fact]
    public void Basic()
    {
        var denseKeyProvider = new DenseKeyProvider<string>();

        SmallImmutableSet<int> GetImmutableSet(params string[] elements)
        {
            var newSet = SmallImmutableSet<int>.Empty;
            foreach (var x in elements)
            {
                newSet = newSet.Add(denseKeyProvider.GetKey($"{LanguageId}:::{x}"), IdentityKeyProvider.Instance);
            }

            return newSet;
        }

        int GetKey(string value)
        {
            return denseKeyProvider.GetKey($"{LanguageId}:::{value}");
        }

        var brackets = Create(
            denseKeyProvider,
            new CharacterPair("{", "}"),
            new CharacterPair("[", "]"),
            new CharacterPair("(", ")"),
            new CharacterPair("begin", "end"),
            new CharacterPair("case", "endcase"),
            new CharacterPair("casez", "endcase"), // Verilog
            new CharacterPair("\\left(", "\\right)"),
            new CharacterPair("\\left(", "\\right."),
            new CharacterPair("\\left.", "\\right)"), // LaTeX Parentheses
            new CharacterPair("\\left[", "\\right]"),
            new CharacterPair("\\left[", "\\right."),
            new CharacterPair("\\left.", "\\right]")); // LaTeX Brackets

        List<TokenView> bracketsExpected =
        [
            new("{", Length.Of(0, 1), TokenKind.OpeningBracket, GetKey("{"), GetImmutableSet("{")),
            new("[", Length.Of(0, 1), TokenKind.OpeningBracket, GetKey("["), GetImmutableSet("[")),
            new("(", Length.Of(0, 1), TokenKind.OpeningBracket, GetKey("("), GetImmutableSet("(")),
            new("begin", Length.Of(0, 5), TokenKind.OpeningBracket, GetKey("begin"), GetImmutableSet("begin")),
            new("case", Length.Of(0, 4), TokenKind.OpeningBracket, GetKey("case"), GetImmutableSet("case")),
            new("casez", Length.Of(0, 5), TokenKind.OpeningBracket, GetKey("casez"), GetImmutableSet("casez")),
            new("\\left(", Length.Of(0, 6), TokenKind.OpeningBracket, GetKey("\\left("), GetImmutableSet("\\left(")),
            new("\\left.", Length.Of(0, 6), TokenKind.OpeningBracket, GetKey("\\left."), GetImmutableSet("\\left.")),
            new("\\left[", Length.Of(0, 6), TokenKind.OpeningBracket, GetKey("\\left["), GetImmutableSet("\\left[")),
            new("}", Length.Of(0, 1), TokenKind.ClosingBracket, GetKey("{"), GetImmutableSet("{")),
            new("]", Length.Of(0, 1), TokenKind.ClosingBracket, GetKey("["), GetImmutableSet("[")),
            new(")", Length.Of(0, 1), TokenKind.ClosingBracket, GetKey("("), GetImmutableSet("(")),
            new("end", Length.Of(0, 3), TokenKind.ClosingBracket, GetKey("begin"), GetImmutableSet("begin")),
            new("endcase", Length.Of(0, 7), TokenKind.ClosingBracket, GetKey("case"), GetImmutableSet("case", "casez")),
            new("\\right)", Length.Of(0, 7), TokenKind.ClosingBracket, GetKey("\\left("), GetImmutableSet("\\left(", "\\left.")),
            new("\\right.", Length.Of(0, 7), TokenKind.ClosingBracket, GetKey("\\left("), GetImmutableSet("\\left(", "\\left[")),
            new("\\right]", Length.Of(0, 7), TokenKind.ClosingBracket, GetKey("\\left["), GetImmutableSet("\\left[", "\\left.")),
        ];

        var bracketsActual = bracketsExpected.Select(x => TokenToObject(brackets.GetToken(x.Text, LanguageId), x.Text)!).ToList();

        Assert.Equal(bracketsExpected, bracketsActual);
    }

    [Fact]
    public void RegExpGlobal_LongerBracketFirst()
    {
        var tokens = CreateSingle(new CharacterPair("begin", "end"), new CharacterPair("if", "end if"));

        var regExp = tokens.RegExpGlobal;

        Assert.NotNull(regExp);
        Assert.Equal("end if", regExp.Match("end if;").Value);
    }

    [Fact]
    public void RegExpGlobal_WordBrackets_NeedAsciiWordBoundaries()
    {
        var tokens = CreateSingle(new CharacterPair("begin", "end"));

        var regExp = tokens.RegExpGlobal;

        Assert.NotNull(regExp);
        Assert.Equal("begin", regExp.Match("x begin y").Value);
        Assert.DoesNotMatch(regExp, "xbegin");
        Assert.DoesNotMatch(regExp, "beginx");
        // JavaScript's \w is ASCII: "é" is not a word character, so it is a boundary.
        Assert.Equal("begin", regExp.Match("ébegin").Value);
        Assert.Equal("BEGIN", regExp.Match("BEGIN").Value);
    }

    [Fact]
    public void RegExpGlobal_SymbolBrackets_HaveNoWordBoundaries()
    {
        var tokens = CreateSingle(new CharacterPair("(", ")"));

        Assert.Equal(@"\)|\(", tokens.GetRegExpStr());
        Assert.Equal(["(", ")"], tokens.RegExpGlobal!.Matches("a(b)c").Select(m => m.Value));
    }

    [Fact]
    public void GetToken_IsCaseInsensitive()
    {
        var tokens = CreateSingle(new CharacterPair("begin", "end"));

        var token = tokens.GetToken("begin");

        Assert.NotNull(token);
        Assert.Same(token, tokens.GetToken("BEGIN"));
        Assert.Same(token, tokens.GetToken("Begin"));
        Assert.Null(tokens.GetToken("other"));
    }

    [Fact]
    public void RegExpGlobal_NoBrackets_IsNull()
    {
        var tokens = CreateSingle();

        Assert.True(tokens.IsEmpty);
        Assert.Null(tokens.GetRegExpStr());
        Assert.Null(tokens.RegExpGlobal);
    }

    [Fact]
    public void FindClosingTokenText_ReturnsTheFirstClosingTextInInsertionOrder()
    {
        var denseKeyProvider = new DenseKeyProvider<string>();
        var tokens = Create(
            denseKeyProvider,
            new CharacterPair("case", "endcase"),
            new CharacterPair("case", "end")).GetSingleLanguageBracketTokens(LanguageId);
        var caseIds = tokens.GetToken("case")!.BracketIds;

        Assert.Equal("endcase", tokens.FindClosingTokenText(caseIds));
        Assert.Null(tokens.FindClosingTokenText(SmallImmutableSet<int>.Empty));
    }

    [Fact]
    public void DidLanguageChange_IsTrueOnlyForLanguagesInUse()
    {
        var tokens = Create(new DenseKeyProvider<string>(), new CharacterPair("(", ")"));

        Assert.False(tokens.DidLanguageChange(LanguageId));

        tokens.GetToken("(", LanguageId);

        Assert.True(tokens.DidLanguageChange(LanguageId));
        Assert.False(tokens.DidLanguageChange("other"));
    }

    private static TokenView? TokenToObject(Token? token, string text)
    {
        return token is null ? null : new TokenView(text, token.Length, token.Kind, token.BracketId, token.BracketIds);
    }
}

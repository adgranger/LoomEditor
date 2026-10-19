using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Tests.TestHelpers;

namespace LoomEditor.TextMate.Tests.Grammar;

public class LineTokensTests
{
    private static readonly NoThemeScopeContext Grammar = new();
    private static readonly IReadOnlyList<TokenTypeMatcher> NoOverrides = [];

    private static StateStackImpl State(params string[] scopes)
    {
        AttributedScopeStack names = Grammar.Scopes(scopes);
        return StateStackImpl.Null.Push(new RuleId(1), 0, 0, false, null, names, names);
    }

    private static TokenTypeMatcher Override(string selector, StandardTokenType type) =>
        new(Matcher.CreateMatchers<string[]>(selector, NameMatcher.Matches)[0].Matcher, type);

    [Fact]
    public void GetResult_LastTokenIsExtendedToLineLength()
    {
        var lineTokens = new LineTokens(false, "\n", NoOverrides, null);

        List<Token> tokens = lineTokens.GetResult(State("string"), 1);

        Token token = Assert.Single(tokens);
        Assert.Equal(0, token.StartIndex);
        Assert.Equal(1, token.EndIndex);
        Assert.Equal(["source.x", "string"], token.Scopes);
    }

    [Fact]
    public void GetResult_PopsTheTokenProducedForTheNewline()
    {
        var lineTokens = new LineTokens(false, "abc\n", NoOverrides, null);
        StateStackImpl stack = State("string");

        lineTokens.ProduceFromScopes(Grammar.Scopes("keyword"), 3);
        lineTokens.Produce(stack, 4);
        List<Token> tokens = lineTokens.GetResult(stack, 4);

        Token token = Assert.Single(tokens);
        Assert.Equal(0, token.StartIndex);
        Assert.Equal(3, token.EndIndex);
        Assert.Equal(["source.x", "keyword"], token.Scopes);
    }

    [Fact]
    public void Produce_BeforeLastTokenEnd_IsIgnored()
    {
        var lineTokens = new LineTokens(false, "abcd", NoOverrides, null);
        StateStackImpl stack = State("string");

        lineTokens.Produce(stack, 2);
        lineTokens.Produce(stack, 2);
        lineTokens.Produce(stack, 1);
        lineTokens.Produce(stack, 4);

        Assert.Equal([(0, 2), (2, 4)], lineTokens.GetResult(stack, 5).Select(t => (t.StartIndex, t.EndIndex)));
    }

    [Fact]
    public void GetBinaryResult_ConsecutiveTokensWithSameMetadata_AreMerged()
    {
        var lineTokens = new LineTokens(true, "abcdef", NoOverrides, null);
        StateStackImpl stack = State("string");
        AttributedScopeStack other = Grammar.Scopes("comment");

        lineTokens.Produce(stack, 2);
        lineTokens.Produce(stack, 4);
        lineTokens.ProduceFromScopes(other, 6);
        uint[] result = lineTokens.GetBinaryResult(stack, 7);

        Assert.Equal(4, result.Length);
        Assert.Equal(0u, result[0]);
        Assert.Equal(stack.ContentNameScopesList!.TokenAttributes, result[1]);
        Assert.Equal(4u, result[2]);
        Assert.Equal(other.TokenAttributes, result[3]);
    }

    [Fact]
    public void GetBinaryResult_EmptyLine_ProducesOneTokenAtZero_AndNewlineTokenIsPopped()
    {
        StateStackImpl stack = State("string");

        uint[] empty = new LineTokens(true, "\n", NoOverrides, null).GetBinaryResult(stack, 1);
        var lineTokens = new LineTokens(true, "ab\n", NoOverrides, null);
        lineTokens.Produce(stack, 2);
        lineTokens.ProduceFromScopes(Grammar.Scopes("comment"), 3);
        uint[] popped = lineTokens.GetBinaryResult(stack, 3);

        Assert.Equal([0u, stack.ContentNameScopesList!.TokenAttributes], empty);
        Assert.Equal([0u, stack.ContentNameScopesList.TokenAttributes], popped);
    }

    [Fact]
    public void GetBinaryResult_LineWithRtlText_DoesNotMergeTokens()
    {
        var lineTokens = new LineTokens(true, "אב", NoOverrides, null);
        StateStackImpl stack = State("string");

        lineTokens.Produce(stack, 1);
        lineTokens.Produce(stack, 2);

        Assert.Equal(4, lineTokens.GetBinaryResult(stack, 3).Length);
    }

    [Fact]
    public void Produce_TokenTypeOverride_SetsStandardTokenType()
    {
        TokenTypeMatcher[] overrides = [Override("string", StandardTokenType.Comment)];
        var lineTokens = new LineTokens(true, "ab", overrides, null);

        lineTokens.ProduceFromScopes(Grammar.Scopes("string"), 1);
        lineTokens.ProduceFromScopes(Grammar.Scopes("keyword"), 2);
        uint[] result = lineTokens.GetBinaryResult(State("keyword"), 3);

        Assert.Equal(StandardTokenType.Comment, EncodedTokenAttributes.GetTokenType(result[1]));
        Assert.Equal(StandardTokenType.Other, EncodedTokenAttributes.GetTokenType(result[3]));
    }

    [Fact]
    public void Produce_BalancedBracketSelector_SetsBracketBit()
    {
        var selectors = new BalancedBracketSelectors(["keyword"], []);
        var lineTokens = new LineTokens(true, "ab", NoOverrides, selectors);

        lineTokens.ProduceFromScopes(Grammar.Scopes("keyword.control"), 1);
        lineTokens.ProduceFromScopes(Grammar.Scopes("string"), 2);
        uint[] result = lineTokens.GetBinaryResult(State("string"), 3);

        Assert.True(EncodedTokenAttributes.ContainsBalancedBrackets(result[1]));
        Assert.False(EncodedTokenAttributes.ContainsBalancedBrackets(result[3]));
    }

    [Fact]
    public void Produce_BalancedBracketSelectorStar_SetsBracketBitEverywhere()
    {
        var lineTokens = new LineTokens(true, "ab", NoOverrides, new BalancedBracketSelectors(["*"], []));

        lineTokens.ProduceFromScopes(Grammar.Scopes("string"), 2);
        uint[] result = lineTokens.GetBinaryResult(State("string"), 3);

        Assert.True(EncodedTokenAttributes.ContainsBalancedBrackets(result[1]));
    }
}

using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Theme;
using LoomEditor.TextMate.Tests.TestHelpers;
using TextMateGrammar = LoomEditor.TextMate.Grammar.Grammar;

namespace LoomEditor.TextMate.Tests.Grammar;

// Not ported here: the data-driven suites of vscode-textmate (P4-21, P4-22). These are our own scenarios.
public class TokenizeLineTests
{
    // "start-end:scope scope|..." for readable assertions.
    private static string Describe(TokenizeLineResult result) =>
        string.Join('|', result.Tokens.Select(t => $"{t.StartIndex}-{t.EndIndex}:{string.Join(' ', t.Scopes)}"));

    private static string GrammarJson(string patterns) => $$"""{ "scopeName": "source.x", "patterns": [ {{patterns}} ] }""";

    [Fact]
    public void TokenizeLine_SimpleMatchRule_ProducesScopes()
    {
        using TextMateGrammar grammar = TestGrammars.Create(GrammarJson("""{ "match": "a+", "name": "letter.a" }"""));

        TokenizeLineResult result = grammar.TokenizeLine("xaay", null);

        // The tokens cover the line and its terminator (lineLength = 5).
        Assert.Equal("0-1:source.x|1-3:source.x letter.a|3-5:source.x", Describe(result));
        Assert.False(result.StoppedEarly);
        Assert.Equal(1, result.RuleStack.Depth);
    }

    [Fact]
    public void TokenizeLine_EmptyLine_ProducesOneTokenForTheTerminator()
    {
        using TextMateGrammar grammar = TestGrammars.Create(GrammarJson("""{ "match": "a", "name": "letter.a" }"""));

        Assert.Equal("0-1:source.x", Describe(grammar.TokenizeLine("", null)));
    }

    [Fact]
    public void TokenizeLine_BeginEndAcrossLines_KeepsStateAndContentName()
    {
        using TextMateGrammar grammar = TestGrammars.Create(GrammarJson(
            """{ "begin": "\"", "end": "\"", "name": "string", "contentName": "string.content" }"""));

        TokenizeLineResult first = grammar.TokenizeLine("a \"b", null);
        TokenizeLineResult second = grammar.TokenizeLine("c\" d", first.RuleStack);

        Assert.Equal("0-2:source.x|2-3:source.x string|3-5:source.x string string.content", Describe(first));
        Assert.Equal(2, first.RuleStack.Depth);
        Assert.Equal("0-1:source.x string string.content|1-2:source.x string|2-5:source.x", Describe(second));
        Assert.Equal(1, second.RuleStack.Depth);
    }

    [Fact]
    public void TokenizeLine_BeginEndWithBackReference_UsesTheCapturedEnd()
    {
        using TextMateGrammar grammar = TestGrammars.Create(GrammarJson(
            """{ "begin": "<(\\w+)>", "end": "</\\1>", "name": "tag" }"""));

        TokenizeLineResult first = grammar.TokenizeLine("<b>x</i>", null);
        TokenizeLineResult second = grammar.TokenizeLine("y</b>", first.RuleStack);

        Assert.Equal(2, first.RuleStack.Depth);
        // The end match carries the scope of its rule; the token for the terminator is dropped.
        Assert.Equal("0-1:source.x tag|1-5:source.x tag", Describe(second));
        Assert.Equal(1, second.RuleStack.Depth);
    }

    [Fact]
    public void TokenizeLine_BeginWhile_EndsWhenWhileFails()
    {
        using TextMateGrammar grammar = TestGrammars.Create(GrammarJson(
            """{ "begin": "^>", "while": "^>", "name": "quote" }"""));

        TokenizeLineResult first = grammar.TokenizeLine("> a", null);
        TokenizeLineResult second = grammar.TokenizeLine("> b", first.RuleStack);
        TokenizeLineResult third = grammar.TokenizeLine("c", second.RuleStack);

        Assert.Equal(2, first.RuleStack.Depth);
        Assert.Equal(2, second.RuleStack.Depth);
        Assert.Equal("0-1:source.x quote|1-4:source.x quote", Describe(second));
        Assert.Equal(1, third.RuleStack.Depth);
        Assert.Equal("0-2:source.x", Describe(third));
    }

    [Fact]
    public void TokenizeLine_NestedCaptures_ProduceNestedScopes()
    {
        using TextMateGrammar grammar = TestGrammars.Create(GrammarJson(
            """{ "match": "(a(b)c)", "captures": { "1": { "name": "outer" }, "2": { "name": "inner" } } }"""));

        TokenizeLineResult result = grammar.TokenizeLine("abc", null);

        Assert.Equal("0-1:source.x outer|1-2:source.x outer inner|2-3:source.x outer", Describe(result));
    }

    [Fact]
    public void TokenizeLine_CaptureWithPatterns_IsRetokenized()
    {
        using TextMateGrammar grammar = TestGrammars.Create(GrammarJson(
            """
            { "match": "x(\\d+)y", "name": "number",
              "captures": { "1": { "patterns": [ { "match": "\\d", "name": "digit" } ] } } }
            """));

        TokenizeLineResult result = grammar.TokenizeLine("x12y", null);

        Assert.Equal(
            "0-1:source.x number|1-2:source.x number digit|2-3:source.x number digit|3-4:source.x number",
            Describe(result));
    }

    [Fact]
    public void TokenizeLine_EmptyMatchAtSamePosition_DoesNotLoopForever()
    {
        using TextMateGrammar grammar = TestGrammars.Create(GrammarJson("""{ "match": "^", "name": "empty" }"""));

        TokenizeLineResult result = grammar.TokenizeLine("abc", null);

        Assert.Equal("0-4:source.x", Describe(result));
        Assert.Equal(1, result.RuleStack.Depth);
    }

    [Fact]
    public void TokenizeLine_BeginEndThatPushesWithoutAdvancing_DoesNotLoopForever()
    {
        using TextMateGrammar grammar = TestGrammars.Create(GrammarJson(
            """{ "begin": "(?=a)", "end": "(?!a)", "name": "loop", "patterns": [ { "include": "$self" } ] }"""));

        TokenizeLineResult result = grammar.TokenizeLine("aaa", null);

        Assert.NotEmpty(result.Tokens);
        Assert.False(result.StoppedEarly);
    }

    [Fact]
    public void TokenizeLine2_BinaryTokens_UseThemeColors()
    {
        var theme = new RawTheme(
        [
            new RawThemeSetting(null, null, null, new RawThemeSettings(null, "#CCCCCC", "#111111")),
            new RawThemeSetting(null, "string", null, new RawThemeSettings("bold", "#FF0000", null)),
        ]);
        var repository = new FakeGrammarRepository(theme);
        using TextMateGrammar grammar = TestGrammars.Create(
            GrammarJson("""{ "match": "\"[^\"]*\"", "name": "string.quoted" }"""),
            repository);
        string?[] colors = repository.Theme.GetColorMap();

        TokenizeLineResult2 result = grammar.TokenizeLine2("x \"y\" z", null);

        // Three tokens: "x ", "\"y\"" and " z\n"; the one for the terminator is merged with " z".
        Assert.Equal(6, result.Tokens.Length);
        Assert.Equal([0u, 2u, 5u], [result.Tokens[0], result.Tokens[2], result.Tokens[4]]);
        Assert.Equal("#CCCCCC", colors[EncodedTokenAttributes.GetForeground(result.Tokens[1])]);
        Assert.Equal("#FF0000", colors[EncodedTokenAttributes.GetForeground(result.Tokens[3])]);
        Assert.Equal("#CCCCCC", colors[EncodedTokenAttributes.GetForeground(result.Tokens[5])]);
        Assert.Equal("#111111", colors[EncodedTokenAttributes.GetBackground(result.Tokens[3])]);
        Assert.Equal((int)FontStyle.Bold, EncodedTokenAttributes.GetFontStyle(result.Tokens[3]));
        Assert.Equal(StandardTokenType.String, EncodedTokenAttributes.GetTokenType(result.Tokens[3]));
        Assert.Equal(1, EncodedTokenAttributes.GetLanguageId(result.Tokens[1]));
    }

    [Fact]
    public void TokenizeLine_TimeLimitReached_StopsEarly()
    {
        using TextMateGrammar grammar = TestGrammars.Create(GrammarJson("""{ "match": "a", "name": "letter.a" }"""));
        string longLine = new('a', 200_000);

        TokenizeLineResult limited = grammar.TokenizeLine(longLine, null, 1);
        TokenizeLineResult unlimited = grammar.TokenizeLine("aaaa", null);

        Assert.True(limited.StoppedEarly);
        Assert.NotNull(limited.RuleStack);
        Assert.False(unlimited.StoppedEarly);
    }

    [Fact]
    public void TokenizeLine_PrevStateFromAnotherImplementation_Throws()
    {
        using TextMateGrammar grammar = TestGrammars.Create(GrammarJson("""{ "match": "a" }"""));

        Assert.Throws<ArgumentException>(() => grammar.TokenizeLine("a", new ForeignState()));
    }

    private sealed class ForeignState : StateStack
    {
        public int Depth => 1;

        public StateStack Clone() => this;

        public bool Equals(StateStack? other) => ReferenceEquals(this, other);
    }
}

using LoomEditor.Core.Tokens;
using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Theme;
using LoomEditor.TextMate.Tests.TestHelpers;
using LoomEditor.TextMate.Tokenization;
using TextMateGrammar = LoomEditor.TextMate.Grammar.Grammar;

namespace LoomEditor.TextMate.Tests.Tokenization;

public class TextMateTokenizationSupportTests
{
    private static string GrammarJson(string patterns) => $$"""{ "scopeName": "source.x", "patterns": [ {{patterns}} ] }""";

    private static TextMateTokenizationSupport CreateSupport(IGrammar grammar, bool containsEmbeddedLanguages = false) =>
        new(grammar, StateStack.Initial, containsEmbeddedLanguages, null, () => false);

    /// <summary>A grammar that always stops early.</summary>
    private sealed class StoppingGrammar : IGrammar
    {
        public TokenizeLineResult TokenizeLine(string lineText, StateStack? prevState, int timeLimit = 0) =>
            throw new NotSupportedException();

        public TokenizeLineResult2 TokenizeLine2(string lineText, StateStack? prevState, int timeLimit = 0) =>
            new([0, 0], new TextMateGrammarStateProbe().Different, StoppedEarly: true);
    }

    /// <summary>Gets a state that is not equal to the initial state: the end state of a line that opens a rule.</summary>
    private sealed class TextMateGrammarStateProbe
    {
        public StateStack Different { get; } = Open();

        private static StateStack Open()
        {
            using TextMateGrammar grammar = TestGrammars.Create(
                GrammarJson("""{ "begin": "\\(", "end": "\\)", "name": "paren" }"""));
            return grammar.TokenizeLine2("(", null).RuleStack;
        }
    }

    [Fact]
    public void TokenizeEncoded_ReturnsMetadataWithThemeColors()
    {
        var theme = new RawTheme(
        [
            new RawThemeSetting(null, null, null, new RawThemeSettings(null, "#CCCCCC", "#111111")),
            new RawThemeSetting(null, "string", null, new RawThemeSettings("bold", "#FF0000", null)),
        ]);
        var repository = new FakeGrammarRepository(theme);
        using TextMateGrammar grammar = TestGrammars.Create(GrammarJson("""{ "match": "\"[^\"]*\"", "name": "string.quoted" }"""), repository);
        TextMateTokenizationSupport support = CreateSupport(grammar);
        string?[] colors = repository.Theme.GetColorMap();

        EncodedTokenizationResult result = support.TokenizeEncoded("x \"y\" z", true, support.GetInitialState());

        Assert.Equal("#CCCCCC", colors[TokenMetadata.GetForeground(result.Tokens[1])]);
        Assert.Equal("#FF0000", colors[TokenMetadata.GetForeground(result.Tokens[3])]);
        Assert.Equal(LoomEditor.Core.Tokens.StandardTokenType.String, TokenMetadata.GetTokenType(result.Tokens[3]));
    }

    [Fact]
    public void TokenizeEncoded_EndStateEqualToStart_ReturnsSameObject()
    {
        using TextMateGrammar grammar = TestGrammars.Create(GrammarJson("""{ "match": "a", "name": "letter.a" }"""));
        TextMateTokenizationSupport support = CreateSupport(grammar);
        // The first line pushes the root rule, so its end state differs from the initial state; the second line does not change it.
        IState start = support.TokenizeEncoded("x", true, support.GetInitialState()).EndState;

        EncodedTokenizationResult result = support.TokenizeEncoded("aaa", true, start);

        Assert.Same(start, result.EndState);
    }

    [Fact]
    public void TokenizeEncoded_EmbeddedLanguage_RaisesEncounteredLanguageOnce()
    {
        using TextMateGrammar grammar = TestGrammars.Create(
            GrammarJson("""{ "match": "b+", "name": "meta.embedded.x" }"""),
            embeddedLanguages: new Dictionary<string, int> { ["meta.embedded.x"] = 5 });
        TextMateTokenizationSupport support = CreateSupport(grammar, containsEmbeddedLanguages: true);
        var languages = new List<int>();
        support.EncounteredLanguage += (_, id) => languages.Add(id);

        support.TokenizeEncoded("abb", true, support.GetInitialState());
        support.TokenizeEncoded("abb", true, support.GetInitialState());

        Assert.Equal([1, 5], languages);
    }

    [Fact]
    public void TokenizeEncoded_StoppedEarly_KeepsStartState()
    {
        TextMateTokenizationSupport support = CreateSupport(new StoppingGrammar());
        IState start = support.GetInitialState();

        EncodedTokenizationResult result = support.TokenizeEncoded("line", true, start);

        Assert.Same(start, result.EndState);
    }

    private sealed class CountingSupport : ITokenizationSupport
    {
        public int Calls { get; private set; }

        public IState GetInitialState() => NullState.Instance;

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state)
        {
            Calls++;
            return new EncodedTokenizationResult([0, 0], state);
        }
    }

    [Fact]
    public void LineLimit_LongLine_ReturnsNullTokens()
    {
        var inner = new CountingSupport();
        var support = new TokenizationSupportWithLineLimit(7, inner, maxTokenizationLineLength: 10);

        EncodedTokenizationResult longLine = support.TokenizeEncoded(new string('a', 10), true, NullState.Instance);
        EncodedTokenizationResult shortLine = support.TokenizeEncoded(new string('a', 9), true, NullState.Instance);

        Assert.Equal(1, inner.Calls);
        Assert.Equal(2, longLine.Tokens.Length);
        Assert.Equal(7, TokenMetadata.GetLanguageId(longLine.Tokens[1]));
        Assert.NotNull(shortLine);
    }
}

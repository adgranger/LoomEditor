using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Parsing;
using LoomEditor.TextMate.Theme;
using LoomEditor.TextMate.Tests.TestHelpers;

namespace LoomEditor.TextMate.Tests.Grammar;

public class RegistryTests
{
    private const string SourceA = """
        { "scopeName": "source.a", "patterns": [ { "include": "source.b" }, { "include": "source.c#rule" } ] }
        """;

    private const string SourceB = """
        { "scopeName": "source.b", "patterns": [ { "match": "b", "name": "letter.b" }, { "include": "source.d" } ] }
        """;

    private const string SourceC = """
        { "scopeName": "source.c", "patterns": [], "repository": { "rule": { "match": "c", "name": "letter.c" } } }
        """;

    private const string SourceD = """
        { "scopeName": "source.d", "patterns": [ { "match": "d", "name": "letter.d" } ] }
        """;

    private sealed class Loader(params string[] grammars)
    {
        private readonly Dictionary<string, string> _grammars = grammars.ToDictionary(
            json => TestGrammars.Parse(json).ScopeName!,
            json => json,
            StringComparer.Ordinal);

        public List<string> Calls { get; } = [];

        public RawGrammar? Load(string scopeName)
        {
            Calls.Add(scopeName);
            return _grammars.TryGetValue(scopeName, out string? json) ? TestGrammars.Parse(json) : null;
        }
    }

    private static Registry NewRegistry(Loader loader, RawTheme? theme = null, Func<string, IReadOnlyList<string>?>? injections = null) =>
        new(new RegistryOptions(OnigTestLib.GetOniguruma(), loader.Load, theme, null, injections));

    private static string Describe(TokenizeLineResult result) =>
        string.Join('|', result.Tokens.Select(t => $"{t.StartIndex}-{t.EndIndex}:{string.Join(' ', t.Scopes)}"));

    [Fact]
    public void LoadGrammar_LoadsIncludedGrammarsThroughTheLoader()
    {
        var loader = new Loader(SourceA, SourceB, SourceC, SourceD);
        using Registry registry = NewRegistry(loader);

        IGrammar? grammar = registry.LoadGrammar("source.a");

        Assert.NotNull(grammar);
        Assert.Equal(["source.a", "source.b", "source.c", "source.d"], loader.Calls.Order());
        Assert.Equal(4, loader.Calls.Count);
        Assert.Equal(
            "0-1:source.a letter.b|1-2:source.a letter.c|2-3:source.a letter.d",
            Describe(grammar.TokenizeLine("bcd", null)));

        Assert.Same(grammar, registry.LoadGrammar("source.a"));
        Assert.Equal(4, loader.Calls.Count);
    }

    [Fact]
    public void LoadGrammar_UnknownScope_Throws()
    {
        // Upstream rejects as well: "No grammar provided for <scope>" (the card's "returns null" does not match it).
        var loader = new Loader();
        using Registry registry = NewRegistry(loader);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => registry.LoadGrammar("source.none"));

        Assert.Equal("No grammar provided for <source.none>", ex.Message);
        Assert.Equal(["source.none"], loader.Calls);
    }

    [Fact]
    public void LoadGrammar_MissingIncludedGrammar_IsIgnored_AndAskedOnce()
    {
        var loader = new Loader("""{ "scopeName": "source.a", "patterns": [ { "include": "source.missing" }, { "match": "a", "name": "a" } ] }""");
        using Registry registry = NewRegistry(loader);

        IGrammar? grammar = registry.LoadGrammar("source.a");

        Assert.NotNull(grammar);
        Assert.Equal(["source.a", "source.missing"], loader.Calls);
    }

    [Fact]
    public void LoadGrammar_UsesInjectionsOfTheOptions()
    {
        var loader = new Loader(
            """{ "scopeName": "source.a", "patterns": [] }""",
            """{ "scopeName": "source.inj", "injectionSelector": "source.a", "patterns": [ { "match": "x", "name": "injected" } ] }""");
        using Registry registry = NewRegistry(loader, null, scope => scope == "source.a" ? ["source.inj"] : null);

        IGrammar grammar = registry.LoadGrammar("source.a")!;

        Assert.Contains("source.inj", loader.Calls);
        Assert.Equal("0-1:source.a injected", Describe(grammar.TokenizeLine("x", null)));
    }

    [Fact]
    public void LoadGrammarWithConfiguration_AppliesEmbeddedLanguagesAndTokenTypes()
    {
        var loader = new Loader("""{ "scopeName": "source.a", "patterns": [ { "match": "a", "name": "source.css.x" }, { "match": "b", "name": "other" } ] }""");
        using Registry registry = NewRegistry(loader);

        IGrammar grammar = registry.LoadGrammarWithConfiguration(
            "source.a",
            3,
            new GrammarConfiguration(
                EmbeddedLanguages: new Dictionary<string, int> { ["source.css"] = 9 },
                TokenTypes: new Dictionary<string, StandardTokenType> { ["other"] = StandardTokenType.Comment }))!;
        TokenizeLineResult2 result = grammar.TokenizeLine2("ab", null);

        Assert.Equal(9, EncodedTokenAttributes.GetLanguageId(result.Tokens[1]));
        Assert.Equal(3, EncodedTokenAttributes.GetLanguageId(result.Tokens[3]));
        Assert.Equal(StandardTokenType.Comment, EncodedTokenAttributes.GetTokenType(result.Tokens[3]));
    }

    [Fact]
    public void AddGrammar_ReturnsTheGrammarWithoutTheLoader()
    {
        var loader = new Loader();
        using Registry registry = NewRegistry(loader);

        IGrammar grammar = registry.AddGrammar(TestGrammars.Parse(SourceD));

        Assert.Equal("0-1:source.d letter.d", Describe(grammar.TokenizeLine("d", null)));
        Assert.Empty(loader.Calls);
    }

    [Fact]
    public void SetTheme_ChangesTheColorMapAndTheMetadata()
    {
        RawTheme Theme(string stringColor) => new(
        [
            new RawThemeSetting(null, null, null, new RawThemeSettings(null, "#CCCCCC", "#111111")),
            new RawThemeSetting(null, "string", null, new RawThemeSettings(null, stringColor, null)),
        ]);
        var loader = new Loader("""{ "scopeName": "source.a", "patterns": [ { "match": "s", "name": "string.x" } ] }""");
        using Registry registry = NewRegistry(loader, Theme("#FF0000"));
        IGrammar grammar = registry.LoadGrammar("source.a")!;

        TokenizeLineResult2 before = grammar.TokenizeLine2("s", null);
        string?[] colorMapBefore = registry.GetColorMap();
        registry.SetTheme(Theme("#00FF00"));
        TokenizeLineResult2 after = grammar.TokenizeLine2("s", null);
        string?[] colorMapAfter = registry.GetColorMap();

        Assert.Contains("#FF0000", colorMapBefore);
        Assert.DoesNotContain("#FF0000", colorMapAfter);
        Assert.Equal("#FF0000", colorMapBefore[EncodedTokenAttributes.GetForeground(before.Tokens[1])]);
        Assert.Equal("#00FF00", colorMapAfter[EncodedTokenAttributes.GetForeground(after.Tokens[1])]);
    }

    [Fact]
    public void ParseRawGrammar_ParsesJson()
    {
        RawGrammar grammar = Registry.ParseRawGrammar(SourceD, "d.json");

        Assert.Equal("source.d", grammar.ScopeName);
    }
}

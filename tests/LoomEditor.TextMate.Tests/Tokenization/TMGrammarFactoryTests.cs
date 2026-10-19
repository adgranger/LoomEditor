using LoomEditor.Core.Tokens;
using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Parsing;
using LoomEditor.TextMate.Tests.TestHelpers;
using LoomEditor.TextMate.Tokenization;

namespace LoomEditor.TextMate.Tests.Tokenization;

public class TMGrammarFactoryTests
{
    private static (LanguageRegistry Languages, TMGrammarFactory Factory) Create(
        Dictionary<string, string> files,
        params (string Folder, string LanguageJson)[] languages)
    {
        var registry = new LanguageRegistry();
        foreach ((string folder, string json) in languages)
        {
            registry.Register(LanguageDefinitionReader.Read(json, folder));
        }

        var factory = new TMGrammarFactory(
            registry.CreateGrammarDefinitions(),
            location => files.GetValueOrDefault(location),
            new RawGrammarCache(),
            OnigTestLib.GetOniguruma());
        return (registry, factory);
    }

    [Fact]
    public void CreateGrammar_InjectionIsAppliedToTargetScope()
    {
        var files = new Dictionary<string, string>
        {
            ["a/a.json"] = """{ "scopeName": "source.a", "patterns": [ { "match": "foo", "name": "kw.a" } ] }""",
            ["a/inj.json"] = """{ "scopeName": "source.inj", "injectionSelector": "L:source.a", "patterns": [ { "match": "bar", "name": "inj.bar" } ] }""",
        };
        (LanguageRegistry languages, TMGrammarFactory factory) = Create(
            files,
            ("a/", """{ "id": "a", "scopeName": "source.a", "path": "a.json", "grammars": [ { "scopeName": "source.inj", "path": "inj.json", "injectTo": ["source.a"] } ] }"""));
        using TMGrammarFactory _ = factory;

        CreateGrammarResult result = factory.CreateGrammar("a", languages.Codec.EncodeLanguageId("a"));

        TokenizeLineResult tokens = Assert.IsAssignableFrom<IGrammar>(result.Grammar).TokenizeLine("foo bar", null);
        Assert.Contains(tokens.Tokens, t => t.Scopes.Contains("kw.a"));
        Assert.Contains(tokens.Tokens, t => t.Scopes.Contains("inj.bar"));
        Assert.False(result.ContainsEmbeddedLanguages);
    }

    [Fact]
    public void CreateGrammar_EmbeddedLanguagesAreEncoded()
    {
        var files = new Dictionary<string, string>
        {
            ["html/html.json"] = """
                { "scopeName": "source.html", "patterns": [
                  { "begin": "<script>", "end": "</script>", "name": "tag.script", "contentName": "meta.embedded.js" } ] }
                """,
            ["js/js.json"] = """{ "scopeName": "source.js", "patterns": [] }""",
        };
        (LanguageRegistry languages, TMGrammarFactory factory) = Create(
            files,
            ("html/", """{ "id": "html", "scopeName": "source.html", "path": "html.json", "embeddedLanguages": { "meta.embedded.js": "js" } }"""),
            ("js/", """{ "id": "js", "scopeName": "source.js", "path": "js.json" }"""));
        using TMGrammarFactory _ = factory;
        var tokenization = new TextMateTokenizationFactory(languages, factory);

        ITokenizationSupport support = Assert.IsAssignableFrom<ITokenizationSupport>(tokenization.CreateTokenizationSupport("html"));
        EncodedTokenizationResult result = support.TokenizeEncoded("<script>x", true, support.GetInitialState());

        int js = languages.Codec.EncodeLanguageId("js");
        var tokenLanguages = new List<int>();
        for (int i = 0; i < result.Tokens.Length; i += 2)
        {
            tokenLanguages.Add(TokenMetadata.GetLanguageId(result.Tokens[i + 1]));
        }

        Assert.Contains(js, tokenLanguages);
        Assert.Contains(languages.Codec.EncodeLanguageId("html"), tokenLanguages);
    }

    [Fact]
    public void CreateTokenizationSupport_UnknownLanguage_ReturnsNull()
    {
        (LanguageRegistry languages, TMGrammarFactory factory) = Create(
            [],
            ("a/", """{ "id": "a", "scopeName": "source.a", "path": "missing.json" }"""),
            ("b/", """{ "id": "b" }"""));
        using TMGrammarFactory _ = factory;
        var tokenization = new TextMateTokenizationFactory(languages, factory);

        Assert.Null(tokenization.CreateTokenizationSupport("nope")); // not a registered language
        Assert.Null(tokenization.CreateTokenizationSupport("b")); // no grammar
        Assert.Null(tokenization.CreateTokenizationSupport("a")); // the grammar file does not exist
        Assert.Null(tokenization.CreateTokenizationSupport("plaintext"));
        Assert.Throws<InvalidOperationException>(() => factory.CreateGrammar("b", 2));
    }

    [Fact]
    public void RawGrammarCache_ParsesOnce_FromTwoThreads()
    {
        var cache = new RawGrammarCache();
        int reads = 0;
        string? Read(string location)
        {
            Interlocked.Increment(ref reads);
            Thread.Sleep(20);
            return """{ "scopeName": "source.a", "patterns": [] }""";
        }

        var results = new RawGrammar?[8];
        Parallel.For(0, results.Length, new ParallelOptions { MaxDegreeOfParallelism = 2 }, i => results[i] = cache.Get("a.json", Read));

        Assert.Equal(1, reads);
        Assert.NotNull(results[0]);
        Assert.All(results, r => Assert.Same(results[0], r));
        Assert.Equal(1, cache.ParseCount);
    }
}

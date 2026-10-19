using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Tokenization;

namespace LoomEditor.TextMate.Tests.Tokenization;

public class LanguageRegistryTests
{
    [Fact]
    public void LanguageRegistry_ByExtensionAndFileName()
    {
        var registry = new LanguageRegistry();
        registry.Register(LanguageDefinitionReader.Read("""{ "id": "csharp", "extensions": [".cs", ".csx"], "filenames": ["Makefile.cs"] }""", "csharp/"));
        registry.Register(LanguageDefinitionReader.Read("""{ "id": "make", "filenames": ["Makefile"] }""", "make/"));

        Assert.Equal("csharp", registry.GetLanguageIdByExtension(".CS"));
        Assert.Equal("csharp", registry.GetLanguageIdByExtension("csx"));
        Assert.Equal("csharp", registry.GetLanguageIdByFileName("Program.cs"));
        Assert.Equal("make", registry.GetLanguageIdByFileName("makefile"));
        Assert.Equal("csharp", registry.GetLanguageIdByFileName("Makefile.cs"));
        Assert.Null(registry.GetLanguageIdByFileName("readme.unknown"));
        Assert.Equal("plaintext", registry.GetLanguageIdByExtension(".txt"));
        Assert.True(registry.IsRegisteredLanguageId("plaintext"));
        Assert.Equal(registry.Codec.EncodeLanguageId("csharp"), registry.Codec.EncodeLanguageId("csharp"));
        Assert.NotEqual(0, registry.Codec.EncodeLanguageId("csharp"));
    }

    [Fact]
    public void ReadLanguageJson_AllFields()
    {
        LanguageDefinition definition = LanguageDefinitionReader.Read(
            """
            {
              "id": "html",
              "aliases": ["HTML", "xhtml"],
              "extensions": [".html", ".htm"],
              "filenames": ["index"],
              "scopeName": "text.html",
              "path": "syntaxes/html.tmLanguage.json",
              "embeddedLanguages": { "meta.embedded.js": "js", "bad": 3 },
              "tokenTypes": { "meta.tag": "other", "string.quoted": "string" },
              "injectTo": ["source.x"],
              "balancedBracketScopes": ["meta.tag"],
              "unbalancedBracketScopes": ["punctuation"],
              "grammars": [
                { "scopeName": "source.inj", "path": "inj.json", "injectTo": ["text.html"] },
                { "path": "no-scope.json" }
              ]
            }
            """,
            "html/");

        Assert.Equal("html", definition.Id);
        Assert.Equal(["HTML", "xhtml"], definition.Aliases);
        Assert.Equal([".html", ".htm"], definition.Extensions);
        Assert.Equal(["index"], definition.FileNames);
        GrammarDefinition grammar = Assert.IsType<GrammarDefinition>(definition.Grammar);
        Assert.Equal(("html/syntaxes/html.tmLanguage.json", "text.html"), (grammar.Location, grammar.ScopeName));
        Assert.Equal(new Dictionary<string, string> { ["meta.embedded.js"] = "js" }, grammar.EmbeddedLanguages); // "bad" is not a string
        Assert.Equal(new Dictionary<string, string> { ["meta.tag"] = "other", ["string.quoted"] = "string" }, grammar.TokenTypes);
        Assert.Equal(["source.x"], grammar.InjectTo);
        Assert.Equal(["meta.tag"], grammar.BalancedBracketScopes);
        Assert.Equal(["punctuation"], grammar.UnbalancedBracketScopes);
        GrammarDefinition extra = Assert.Single(definition.ExtraGrammars); // the one without a scopeName is dropped
        Assert.Equal(("html/inj.json", "source.inj"), (extra.Location, extra.ScopeName));
        Assert.Null(extra.BalancedBracketScopes);
    }

    [Fact]
    public void CreateGrammarDefinitions_AppliesDefaultsAndDropsUnknownValues()
    {
        var registry = new LanguageRegistry();
        registry.Register(LanguageDefinitionReader.Read(
            """
            { "id": "a", "scopeName": "source.a", "path": "a.json",
              "embeddedLanguages": { "x": "a", "y": "nope" }, "tokenTypes": { "c": "comment", "d": "weird" } }
            """,
            "a/"));

        ValidGrammarDefinition valid = Assert.Single(registry.CreateGrammarDefinitions());

        Assert.Equal("a", valid.Language);
        Assert.Equal(["*"], valid.BalancedBracketSelectors);
        Assert.Empty(valid.UnbalancedBracketSelectors);
        Assert.Equal(new Dictionary<string, int> { ["x"] = registry.Codec.EncodeLanguageId("a") }, valid.EmbeddedLanguages);
        Assert.Equal(new Dictionary<string, StandardTokenType> { ["c"] = StandardTokenType.Comment }, valid.TokenTypes);
    }
}

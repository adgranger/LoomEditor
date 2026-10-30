using LoomEditor.Core.Languages;
using LoomEditor.Core.Tokens;
using LoomEditor.TextMate.Grammars;
using LoomEditor.TextMate.Tests.TestHelpers;
using LoomEditor.TextMate.Tokenization;

namespace LoomEditor.TextMate.Tests.Grammars;

public class BuiltInLanguagesTests
{
    private static (LanguageRegistry Languages, TMGrammarFactory Factory, TextMateTokenizationFactory Tokenization) Create()
    {
        var languages = new LanguageRegistry();
        BuiltInLanguages.RegisterAll(languages);
        var factory = new TMGrammarFactory(
            languages.CreateGrammarDefinitions(),
            BuiltInLanguages.ReadFile,
            new RawGrammarCache(),
            OnigTestLib.GetOniguruma());
        return (languages, factory, new TextMateTokenizationFactory(languages, factory));
    }

    // Tokenizes the lines in order and returns the language id of every token.
    private static List<int> TokenLanguages(ITokenizationSupport support, params string[] lines)
    {
        var result = new List<int>();
        IState state = support.GetInitialState();
        foreach (string line in lines)
        {
            EncodedTokenizationResult tokens = support.TokenizeEncoded(line, true, state);
            for (int i = 0; i < tokens.Tokens.Length; i += 2)
            {
                result.Add(TokenMetadata.GetLanguageId(tokens.Tokens[i + 1]));
            }

            state = tokens.EndState;
        }

        return result;
    }

    public static TheoryData<string, string, string> Samples => new()
    {
        { "csharp", "using System;", "class A { int x = 1; }" },
        { "typescript", "const x: number = 1;", "function f(a: string) { return a; }" },
        { "javascript", "const x = /ab+c/g;", "function f(a) { return a; }" },
        { "json", "{ \"a\": [1, true, null] }", "{ \"b\": \"c\" }" },
        { "jsonc", "// comment", "{ \"a\": 1 }" },
        { "html", "<div class=\"a\">text</div>", "<!-- comment -->" },
        { "css", "a { color: red; }", "@media screen { b { margin: 0 } }" },
        { "xml", "<?xml version=\"1.0\"?>", "<a b=\"c\">d</a>" },
        { "markdown", "# Title", "Some **bold** and `code`." },
        { "python", "def f(x): return x + 1", "class A: pass" },
        { "yaml", "key: value", "list:\n  - a" },
    };

    // vscode: every language of the extension point must produce a tokenization support.
    [Theory]
    [MemberData(nameof(Samples))]
    public void EveryBuiltInGrammar_LoadsAndTokenizesAShortSample(string languageId, string line1, string line2)
    {
        (LanguageRegistry languages, TMGrammarFactory factory, TextMateTokenizationFactory tokenization) = Create();
        using TMGrammarFactory _ = factory;

        ITokenizationSupport support = Assert.IsAssignableFrom<ITokenizationSupport>(tokenization.CreateTokenizationSupport(languageId));
        EncodedTokenizationResult first = support.TokenizeEncoded(line1, true, support.GetInitialState());
        EncodedTokenizationResult second = support.TokenizeEncoded(line2, true, first.EndState);

        Assert.True(first.Tokens.Length >= 2, $"{languageId}: no token for the first line");
        Assert.True(second.Tokens.Length >= 2, $"{languageId}: no token for the second line");
        Assert.True(languages.IsRegisteredLanguageId(languageId));
    }

    [Fact]
    public void Html_ScriptTag_IsTokenizedAsJavaScript()
    {
        (LanguageRegistry languages, TMGrammarFactory factory, TextMateTokenizationFactory tokenization) = Create();
        using TMGrammarFactory _ = factory;
        ITokenizationSupport support = Assert.IsAssignableFrom<ITokenizationSupport>(tokenization.CreateTokenizationSupport("html"));

        List<int> ids = TokenLanguages(support, "<script>", "var x = 1;", "</script>");

        Assert.Contains(languages.Codec.EncodeLanguageId("javascript"), ids);
        Assert.Contains(languages.Codec.EncodeLanguageId("html"), ids);
    }

    [Fact]
    public void Markdown_FencedCsharp_IsTokenizedAsCsharp()
    {
        (LanguageRegistry languages, TMGrammarFactory factory, TextMateTokenizationFactory tokenization) = Create();
        using TMGrammarFactory _ = factory;
        ITokenizationSupport support = Assert.IsAssignableFrom<ITokenizationSupport>(tokenization.CreateTokenizationSupport("markdown"));

        List<int> ids = TokenLanguages(support, "```csharp", "int x = 1;", "```");

        Assert.Contains(languages.Codec.EncodeLanguageId("csharp"), ids);
    }

    [Fact]
    public void EveryEmbeddedResource_IsListedByALanguage()
    {
        var listed = new HashSet<string>(StringComparer.Ordinal);
        foreach (LanguageDefinition language in BuiltInLanguages.All)
        {
            foreach (GrammarDefinition grammar in language.ExtraGrammars.Append(language.Grammar!))
            {
                listed.Add(grammar.Location);
            }

            if (language.ConfigurationLocation is { } configuration)
            {
                listed.Add(configuration);
            }
        }

        string[] grammarResources = typeof(BuiltInLanguages).Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(BuiltInLanguages.ResourcePrefix, StringComparison.Ordinal))
            .Where(n => !n.EndsWith("language.json", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(grammarResources);
        Assert.Empty(grammarResources.Except(listed)); // no orphan file
        Assert.All(listed, location => Assert.NotNull(BuiltInLanguages.ReadFile(location))); // no missing file
    }

    public static TheoryData<string> LanguageIds => [.. BuiltInLanguages.All.Select(language => language.Id)];

    private static LanguageConfiguration ReadConfiguration(string languageId, List<string>? warnings = null)
    {
        LanguageDefinition language = BuiltInLanguages.All.Single(l => l.Id == languageId);
        string json = BuiltInLanguages.ReadFile(language.ConfigurationLocation!)!;
        return LanguageConfigurationReader.Read(json, languageId, warnings is null ? null : warnings.Add);
    }

    [Theory]
    [MemberData(nameof(LanguageIds))]
    public void EveryBundledRegex_Compiles(string languageId)
    {
        var warnings = new List<string>();

        ReadConfiguration(languageId, warnings);

        Assert.Empty(warnings);
    }

    [Fact]
    public void EveryLanguageWithAGrammar_HasAConfiguration()
    {
        Assert.Equal(11, BuiltInLanguages.All.Count);
        Assert.All(
            BuiltInLanguages.All.Where(language => language.Grammar is not null),
            language => Assert.NotNull(BuiltInLanguages.ReadFile(language.ConfigurationLocation ?? string.Empty)));
    }

    [Fact]
    public void Markdown_WordPattern_MatchesUnicodeWords()
    {
        LanguageConfiguration configuration = ReadConfiguration("markdown");

        Assert.NotNull(configuration.WordPattern);
        Assert.Equal("héllo_wörld", configuration.WordPattern.Match("  héllo_wörld!").Value);
    }

    [Fact]
    public void Csharp_Configuration_HasCommentsBracketsAndRegionMarkers()
    {
        LanguageConfiguration configuration = ReadConfiguration("csharp");

        Assert.Equal("//", configuration.Comments?.LineComment?.Comment);
        Assert.Equal(new CharacterPair("/*", "*/"), configuration.Comments?.BlockComment);
        Assert.Equal([new CharacterPair("{", "}"), new CharacterPair("[", "]"), new CharacterPair("(", ")")], configuration.Brackets);
        FoldingMarkers? markers = configuration.Folding?.Markers;
        Assert.NotNull(markers);
        Assert.Matches(markers.Start, "  #region Fields");
        Assert.Matches(markers.End, "  #endregion");
        Assert.DoesNotMatch(markers.Start, "  #endregion");
    }

    [Theory]
    [InlineData("Program.cs", "csharp")]
    [InlineData(@"C:\repo\src\App.TS", "typescript")]
    [InlineData("README.md", "markdown")]
    [InlineData("notes.txt", "plaintext")]
    [InlineData("tsconfig.json", "jsonc")]
    [InlineData("src/tsconfig.json", "jsonc")]
    [InlineData("package.json", "json")]
    [InlineData("SConstruct", "python")]
    [InlineData("Makefile", null)]
    [InlineData("", null)]
    public void GetLanguageIdByFileName_KnownAndUnknownNames_ReturnsExpectedId(string fileName, string? expected)
    {
        string? id = BuiltInLanguages.GetLanguageIdByFileName(fileName);

        Assert.Equal(expected, id);
        if (id is not null)
        {
            Assert.Contains(id, BuiltInLanguages.Ids);
        }
    }

    [Fact]
    public void GetLanguageIdByFileName_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => BuiltInLanguages.GetLanguageIdByFileName(null!));
    }
}

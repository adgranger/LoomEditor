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
        { "typescriptreact", "const x: number = 1;", "const e = <div className=\"a\">{x}</div>;" },
        { "javascript", "const x = /ab+c/g;", "function f(a) { return a; }" },
        { "json", "{ \"a\": [1, true, null] }", "{ \"b\": \"c\" }" },
        { "jsonc", "// comment", "{ \"a\": 1 }" },
        { "html", "<div class=\"a\">text</div>", "<!-- comment -->" },
        { "css", "a { color: red; }", "@media screen { b { margin: 0 } }" },
        { "xml", "<?xml version=\"1.0\"?>", "<a b=\"c\">d</a>" },
        { "markdown", "# Title", "Some **bold** and `code`." },
        { "python", "def f(x): return x + 1", "class A: pass" },
        { "lua", "local x = 1", "function f(a) return a end" },
        { "yaml", "key: value", "list:\n  - a" },
        { "c", "#include <stdio.h>", "int main(void) { return 0; }" },
        { "cpp", "#include <vector>", "class A { public: int x = 1; };" },
        { "java", "package a.b;", "public class A { int x = 1; }" },
        { "go", "package main", "func f(a int) int { return a }" },
        { "rust", "fn main() {}", "let x: i32 = 1;" },
        { "php", "<?php", "$a = 1; echo $a;" },
        { "ruby", "def f(a)", "  a + 1" },
        { "sql", "SELECT * FROM t", "WHERE a = 1;" },
        { "shellscript", "#!/bin/bash", "echo \"$HOME\" | grep a" },
        { "powershell", "$a = 1", "Write-Host \"hi\"" },
        { "scss", "$c: red;", "a { b { color: $c; } }" },
        { "less", "@c: red;", ".a { .b { color: @c; } }" },
        { "swift", "import Foundation", "let x: Int = 1" },
        { "dart", "import 'a.dart';", "void main() { var x = 1; }" },
        { "dockerfile", "FROM alpine", "RUN echo hi" },
        { "ini", "[section]", "key=value" },
        { "diff", "--- a/f", "+++ b/f" },
        { "objective-c", "#import <Foundation/Foundation.h>", "@interface A : NSObject" },
        { "objective-cpp", "#import <Foundation/Foundation.h>", "@interface A : NSObject" },
        { "cuda-cpp", "__global__ void k(int* a)", "{ a[threadIdx.x] = 1; }" },
        { "r", "x <- c(1, 2, 3)", "print(mean(x))" },
        { "perl", "use strict;", "my $a = 1;" },
        { "raku", "my $a = 1;", "say $a;" },
        { "fsharp", "let x = 1", "let f a = a + 1" },
        { "vb", "Dim x As Integer = 1", "Public Class A" },
        { "razor", "<p>@Model.Name</p>", "@{ var x = 1; }" },
        { "clojure", "(defn f [a] a)", "(println \"hi\")" },
        { "coffeescript", "x = 1", "f = (a) -> a" },
        { "groovy", "def x = 1", "println \"hi\"" },
        { "julia", "x = 1", "function f(a) a end" },
        { "handlebars", "<p>{{name}}</p>", "{{#if a}}b{{/if}}" },
        { "jade", "div.a", "  p text" },
        { "hlsl", "float4 main() : SV_Target", "{ return 1; }" },
        { "shaderlab", "Shader \"A\"", "{ Properties { } }" },
        { "tex", "\\def\\a{b}", "% comment" },
        { "latex", "\\section{A}", "\\begin{document}" },
        { "bibtex", "@article{a,", "  title = {B}" },
        { "restructuredtext", "Title", "=====" },
        { "makefile", "all: a.o", "\t$(CC) -o a a.o" },
        { "bat", "@echo off", "set A=1" },
        { "dotenv", "A=1", "# comment" },
        { "properties", "a.b=c", "# comment" },
        { "javascriptreact", "const x = <div className=\"a\">{1}</div>;", "function f(a) { return a; }" },
        { "jsonl", "{ \"a\": 1 }", "{ \"b\": [true, null] }" },
        { "snippets", "{ \"Log\": {", "  \"prefix\": \"log\", \"body\": [\"console.log($1);\"] } }" },
        { "xsl", "<xsl:template match=\"/\">", "</xsl:template>" },
        { "dockercompose", "services:", "  web: nginx" },
        { "git-commit", "Fix a bug", "# Please enter the commit message" },
        { "git-rebase", "pick abc1234 Fix a bug", "# Rebase abc..def onto 123" },
        { "ignore", "bin/", "!keep.txt" },
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
    public void Markdown_FencedCpp_IsTokenizedAsCpp()
    {
        (LanguageRegistry languages, TMGrammarFactory factory, TextMateTokenizationFactory tokenization) = Create();
        using TMGrammarFactory _ = factory;
        ITokenizationSupport support = Assert.IsAssignableFrom<ITokenizationSupport>(tokenization.CreateTokenizationSupport("markdown"));

        List<int> ids = TokenLanguages(support, "```cpp", "int x = 1;", "```");

        Assert.Contains(languages.Codec.EncodeLanguageId("cpp"), ids);
    }

    [Fact]
    public void Markdown_FencedR_IsTokenizedAsR()
    {
        (LanguageRegistry languages, TMGrammarFactory factory, TextMateTokenizationFactory tokenization) = Create();
        using TMGrammarFactory _ = factory;
        ITokenizationSupport support = Assert.IsAssignableFrom<ITokenizationSupport>(tokenization.CreateTokenizationSupport("markdown"));

        List<int> ids = TokenLanguages(support, "```r", "x <- 1", "```");

        Assert.Contains(languages.Codec.EncodeLanguageId("r"), ids);
    }

    [Fact]
    public void Razor_StyleAndScript_AreTokenizedAsEmbeddedLanguages()
    {
        (LanguageRegistry languages, TMGrammarFactory factory, TextMateTokenizationFactory tokenization) = Create();
        using TMGrammarFactory _ = factory;
        ITokenizationSupport support = Assert.IsAssignableFrom<ITokenizationSupport>(tokenization.CreateTokenizationSupport("razor"));

        List<int> ids = TokenLanguages(support, "<style>a { color: red; }</style>", "<script>", "var y = 2;", "</script>");

        Assert.Contains(languages.Codec.EncodeLanguageId("css"), ids);
        Assert.Contains(languages.Codec.EncodeLanguageId("javascript"), ids);
    }

    [Fact]
    public void Php_HtmlOutsideTheTags_IsTokenizedAsHtml()
    {
        (LanguageRegistry languages, TMGrammarFactory factory, TextMateTokenizationFactory tokenization) = Create();
        using TMGrammarFactory _ = factory;
        ITokenizationSupport support = Assert.IsAssignableFrom<ITokenizationSupport>(tokenization.CreateTokenizationSupport("php"));

        List<int> ids = TokenLanguages(support, "<div class=\"a\"><?php echo 1; ?></div>");

        Assert.Contains(languages.Codec.EncodeLanguageId("html"), ids);
        Assert.Contains(languages.Codec.EncodeLanguageId("php"), ids);
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
        Assert.Equal(63, BuiltInLanguages.All.Count);
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
    [InlineData("App.tsx", "typescriptreact")]
    [InlineData("App.jsx", "javascriptreact")]
    [InlineData("a.js", "javascript")]
    [InlineData("a.jsonl", "jsonl")]
    [InlineData("a.ndjson", "jsonl")]
    [InlineData("a.code-snippets", "snippets")]
    [InlineData("a.xslt", "xsl")]
    [InlineData("COMMIT_EDITMSG", "git-commit")]
    [InlineData("git-rebase-todo", "git-rebase")]
    [InlineData(".gitignore", "ignore")]
    [InlineData("README.md", "markdown")]
    [InlineData("notes.txt", "plaintext")]
    [InlineData("tsconfig.json", "jsonc")]
    [InlineData("src/tsconfig.json", "jsonc")]
    [InlineData("package.json", "json")]
    [InlineData("SConstruct", "python")]
    [InlineData("a.cpp", "cpp")]
    [InlineData("a.h", "cpp")]
    [InlineData("a.c", "c")]
    [InlineData("A.java", "java")]
    [InlineData("a.go", "go")]
    [InlineData("a.rs", "rust")]
    [InlineData("a.php", "php")]
    [InlineData("a.rb", "ruby")]
    [InlineData("Rakefile", "ruby")]
    [InlineData("Gemfile", "ruby")]
    [InlineData("a.sql", "sql")]
    [InlineData("a.sh", "shellscript")]
    [InlineData("a.ps1", "powershell")]
    [InlineData("a.scss", "scss")]
    [InlineData("a.less", "less")]
    [InlineData("a.swift", "swift")]
    [InlineData("a.dart", "dart")]
    [InlineData("Dockerfile", "dockerfile")]
    [InlineData("a.dockerfile", "dockerfile")]
    [InlineData("a.ini", "ini")]
    [InlineData("a.patch", "diff")]
    [InlineData("a.m", "objective-c")]
    [InlineData("a.mm", "objective-cpp")]
    [InlineData("a.cu", "cuda-cpp")]
    [InlineData("a.R", "r")]
    [InlineData("a.pl", "perl")]
    [InlineData("a.raku", "raku")]
    [InlineData("a.fs", "fsharp")]
    [InlineData("a.vb", "vb")]
    [InlineData("a.cshtml", "razor")]
    [InlineData("a.razor", "razor")]
    [InlineData("a.clj", "clojure")]
    [InlineData("a.coffee", "coffeescript")]
    [InlineData("a.groovy", "groovy")]
    [InlineData("Jenkinsfile", "groovy")]
    [InlineData("a.jl", "julia")]
    [InlineData("a.hbs", "handlebars")]
    [InlineData("a.pug", "jade")]
    [InlineData("a.hlsl", "hlsl")]
    [InlineData("a.shader", "shaderlab")]
    [InlineData("a.sty", "tex")]
    [InlineData("a.tex", "latex")]
    [InlineData("a.bib", "bibtex")]
    [InlineData("a.rst", "restructuredtext")]
    [InlineData("Makefile", "makefile")]
    [InlineData("a.mk", "makefile")]
    [InlineData("a.bat", "bat")]
    [InlineData(".env", "dotenv")]
    [InlineData("a.properties", "properties")]
    [InlineData("gitconfig", "properties")]
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

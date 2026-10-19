using LoomEditor.TextMate.Parsing;
using LoomEditor.TextMate.Tests.TestHelpers;

namespace LoomEditor.TextMate.Tests.Parsing;

public class RawGrammarReaderTests
{
    public static TheoryData<string> FirstMateFixtures =>
        new(TestCaseFiles.FileNames("first-mate/fixtures", "*.json"));

    [Theory]
    [MemberData(nameof(FirstMateFixtures))]
    public void ParseJsonGrammar_EveryFirstMateFixture_Parses(string fileName)
    {
        string path = TestCaseFiles.PathOf("first-mate/fixtures/" + fileName);

        RawGrammar grammar = RawGrammarReader.ParseRawGrammar(File.ReadAllText(path), path);

        Assert.False(string.IsNullOrEmpty(grammar.ScopeName));
    }

    [Fact]
    public void ParseJsonGrammar_ReadsTheRuleProperties()
    {
        RawGrammar grammar = RawGrammarReader.ParseJsonGrammar("""
            {
              "scopeName": "source.test", "name": "Test", "fileTypes": ["t", 3], "firstLineMatch": "^#!",
              "injectionSelector": "L:source.x",
              "patterns": [ { "include": "#block" }, 42 ],
              "repository": {
                "block": {
                  "name": "meta.block", "contentName": "meta.content", "begin": "\\{", "end": "\\}",
                  "beginCaptures": { "0": { "name": "punctuation" } },
                  "patterns": [ { "match": "x", "captures": { "1": { "name": "x" } } } ]
                },
                "loop": { "begin": "a", "while": "b", "whileCaptures": { "0": { "name": "w" } } }
              },
              "injections": { "L:comment": { "patterns": [ { "match": "TODO" } ] } }
            }
            """, "test.json");

        Assert.Equal("source.test", grammar.ScopeName);
        Assert.Equal("Test", grammar.Name);
        Assert.Equal(["t"], grammar.FileTypes);
        Assert.Equal("^#!", grammar.FirstLineMatch);
        Assert.Equal("L:source.x", grammar.InjectionSelector);
        RawRule include = Assert.Single(grammar.Patterns!);
        Assert.Equal("#block", include.Include);
        Assert.Equal(["block", "loop"], grammar.Repository!.Names);
        RawRule block = grammar.Repository["block"];
        Assert.Equal(("meta.block", "meta.content", "\\{", "\\}"), (block.Name, block.ContentName, block.Begin, block.End));
        Assert.Equal("punctuation", block.BeginCaptures!["0"].Name);
        Assert.Equal("x", block.Patterns![0].Captures!["1"].Name);
        RawRule loop = grammar.Repository["loop"];
        Assert.Equal("b", loop.While);
        Assert.Equal("w", loop.WhileCaptures!["0"].Name);
        Assert.Equal("TODO", grammar.Injections!["L:comment"].Patterns![0].Match);
    }

    [Fact]
    public void ParseJsonGrammar_CapturesAreSortedNumerically()
    {
        RawGrammar grammar = RawGrammarReader.ParseJsonGrammar("""
            { "scopeName": "s", "patterns": [ { "match": "x", "captures": {
                "name": { "name": "n" }, "10": { "name": "ten" }, "01": { "name": "zero-one" }, "2": { "name": "two" }, "0": { "name": "zero" }
            } } ] }
            """, null);

        RawCaptures captures = grammar.Patterns![0].Captures!;
        Assert.Equal(["0", "2", "10", "name", "01"], captures.Keys);
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("-2", true)]
    [InlineData("0", false)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("\"yes\"", false)]
    public void ParseJsonGrammar_ApplyEndPatternLastAsNumber_IsTrue(string value, bool expected)
    {
        RawGrammar grammar = RawGrammarReader.ParseJsonGrammar(
            $$"""{ "scopeName": "s", "patterns": [ { "begin": "a", "end": "b", "applyEndPatternLast": {{value}} } ] }""", null);

        Assert.Equal(expected, grammar.Patterns![0].ApplyEndPatternLast);
    }

    [Fact]
    public void ParseJsonGrammar_CommentsAndTrailingCommas_AreAccepted()
    {
        RawGrammar grammar = RawGrammarReader.ParseJsonGrammar("""
            {
              // a comment
              "scopeName": "source.c", /* another */
              "patterns": [ { "match": "x", }, ],
            }
            """, null);

        Assert.Equal("source.c", grammar.ScopeName);
        Assert.Equal("x", Assert.Single(grammar.Patterns!).Match);
    }

    [Fact]
    public void ParseJsonGrammar_WrongPropertyTypes_AreIgnored()
    {
        RawGrammar grammar = RawGrammarReader.ParseJsonGrammar(
            """{ "scopeName": 3, "patterns": { "match": "x" }, "repository": [], "name": "n", "unknown": 1 }""", null);

        Assert.Null(grammar.ScopeName);
        Assert.Null(grammar.Patterns);
        Assert.Null(grammar.Repository);
        Assert.Equal("n", grammar.Name);
    }

    [Fact]
    public void ParseJsonGrammar_RootNotAnObject_Throws()
    {
        Assert.Throws<FormatException>(() => RawGrammarReader.ParseJsonGrammar("[]", null));
    }

    [Fact]
    public void ParseRawGrammar_NotJson_IsReadAsPlist()
    {
        const string Plist = "<plist><dict><key>scopeName</key><string>source.p</string></dict></plist>";

        Assert.Equal("source.p", RawGrammarReader.ParseRawGrammar(Plist, "a.tmLanguage").ScopeName);
        Assert.Equal("source.p", RawGrammarReader.ParseRawGrammar(Plist).ScopeName);
    }

    [Fact]
    public void DeepClone_CopiesNestedRepositoriesAndIds()
    {
        RawGrammar grammar = RawGrammarReader.ParseJsonGrammar("""
            { "scopeName": "s", "patterns": [ { "include": "#a" } ],
              "repository": { "a": { "begin": "x", "end": "y", "beginCaptures": { "1": { "name": "c" } },
                                     "repository": { "inner": { "match": "z" } } } },
              "injections": { "L:x": { "match": "i" } } }
            """, null);
        grammar.Repository!["a"].Id = 7;
        grammar.Repository["a"].Repository!["inner"].Id = 8;

        RawGrammar clone = grammar.DeepClone();
        grammar.Repository["a"].Id = 70;
        grammar.Repository["a"].Repository!["inner"].Name = "changed";
        grammar.Repository["a"].BeginCaptures!["1"].Name = "changed";
        grammar.Patterns![0].Include = "changed";
        grammar.Injections!["L:x"].Match = "changed";
        grammar.Repository["b"] = new RawRule();

        RawRule a = clone.Repository!["a"];
        Assert.Equal(7, a.Id);
        Assert.Equal(8, a.Repository!["inner"].Id);
        Assert.Null(a.Repository["inner"].Name);
        Assert.Equal("c", a.BeginCaptures!["1"].Name);
        Assert.Equal("#a", clone.Patterns![0].Include);
        Assert.Equal("i", clone.Injections!["L:x"].Match);
        Assert.Equal(["a"], clone.Repository.Names);
    }
}

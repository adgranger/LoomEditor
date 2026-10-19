using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using LoomEditor.TextMate.Parsing;
using LoomEditor.TextMate.Tests.TestHelpers;

namespace LoomEditor.TextMate.Tests.Parsing;

public class PlistParserTests
{
    public static TheoryData<string> Suite1PlistFixtures =>
        new(TestCaseFiles.FileNames("suite1/fixtures", "*.plist").Concat(TestCaseFiles.FileNames("suite1/fixtures", "*.tmLanguage")));

    [Theory]
    [MemberData(nameof(Suite1PlistFixtures))]
    public void ParsePlistGrammar_EverySuite1PlistFixture_Parses(string fileName)
    {
        string path = TestCaseFiles.PathOf("suite1/fixtures/" + fileName);

        RawGrammar grammar = RawGrammarReader.ParseRawGrammar(File.ReadAllText(path), path);

        Assert.False(string.IsNullOrEmpty(grammar.ScopeName));
        Assert.NotNull(grammar.Patterns);
    }

    // vscode-textmate decodes character references and the five named entities; a CDATA section inside a value is not
    // an XML construct for this parser and is kept as is.
    [Fact]
    public void ParsePlist_EntitiesAndCData_AreDecoded()
    {
        JsonNode? node = PlistParser.Parse("""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <!-- a comment -->
            <dict>
              <key>named</key><string>&lt;a&gt; &amp; &quot;b&quot; &apos;c&apos;</string>
              <key>numeric</key><string>&#65;&#x42;&#x1f600;&#X43;</string>
              <key>double</key><string>&amp;lt;</string>
              <key>cdata</key><string><![CDATA[x]]></string>
            </dict>
            </plist>
            """);

        JsonObject dict = Assert.IsType<JsonObject>(node);
        Assert.Equal("<a> & \"b\" 'c'", (string?)dict["named"]);
        Assert.Equal("AB\U0001F600&#X43;", (string?)dict["numeric"]);
        Assert.Equal("&lt;", (string?)dict["double"]);
        Assert.Equal("<![CDATA[x]]>", (string?)dict["cdata"]);
    }

    [Fact]
    public void ParsePlist_Values_HaveTheirJsonKinds()
    {
        JsonNode? node = PlistParser.Parse("""
            <plist><dict>
              <key>i</key><integer> 12abc</integer>
              <key>r</key><real>-1.5e2x</real>
              <key>t</key><true/>
              <key>f</key><false></false>
              <key>e</key><dict/>
              <key>a</key><array><string>s</string><array/><dict><key>k</key><string>v</string></dict></array>
            </dict></plist>
            """);

        JsonObject dict = Assert.IsType<JsonObject>(node);
        Assert.Equal(12, (double)dict["i"]!);
        Assert.Equal(-150, (double)dict["r"]!);
        Assert.True((bool)dict["t"]!);
        Assert.False((bool)dict["f"]!);
        Assert.Empty(Assert.IsType<JsonObject>(dict["e"]));
        Assert.Equal("""["s",[],{"k":"v"}]""", dict["a"]!.ToJsonString());
    }

    [Theory]
    [InlineData("<dict><string>x</string></dict>", "missing <key>")]
    [InlineData("<dict><key>a</key><key>b</key></dict>", "too many <key>")]
    [InlineData("<array></dict>", "unexpected </dict>")]
    [InlineData("<dict><key>a</key><integer>x</integer></dict>", "cannot parse integer")]
    [InlineData("<foo>", "unexpected opened tag foo")]
    [InlineData("x", "expected <")]
    [InlineData("</array>", "unexpected </array>")]
    public void ParsePlist_Malformed_FailsLikeUpstream(string content, string message)
    {
        FormatException exception = Assert.Throws<FormatException>(() => PlistParser.Parse(content));

        Assert.Contains(message, exception.Message, StringComparison.Ordinal);
        Assert.StartsWith("Near offset ", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ParsePlist_SameGrammarAsJson_GivesEqualRawGrammar()
    {
        const string Json = """
            {
              "name": "Test", "scopeName": "source.test", "fileTypes": ["t"],
              "patterns": [ { "include": "#block" }, { "match": "a(b)", "name": "x", "captures": { "1": { "name": "b" }, "0": { "name": "all" } } } ],
              "repository": { "block": { "begin": "\\{", "end": "\\}", "applyEndPatternLast": 1,
                                         "endCaptures": { "0": { "name": "close" } }, "patterns": [ { "include": "$self" } ] } }
            }
            """;
        const string Plist = """
            <?xml version="1.0" encoding="UTF-8"?>
            <plist version="1.0">
            <dict>
              <key>name</key><string>Test</string>
              <key>scopeName</key><string>source.test</string>
              <key>fileTypes</key><array><string>t</string></array>
              <key>patterns</key>
              <array>
                <dict><key>include</key><string>#block</string></dict>
                <dict>
                  <key>match</key><string>a(b)</string>
                  <key>name</key><string>x</string>
                  <key>captures</key>
                  <dict>
                    <key>1</key><dict><key>name</key><string>b</string></dict>
                    <key>0</key><dict><key>name</key><string>all</string></dict>
                  </dict>
                </dict>
              </array>
              <key>repository</key>
              <dict>
                <key>block</key>
                <dict>
                  <key>begin</key><string>\{</string>
                  <key>end</key><string>\}</string>
                  <key>applyEndPatternLast</key><integer>1</integer>
                  <key>endCaptures</key><dict><key>0</key><dict><key>name</key><string>close</string></dict></dict>
                  <key>patterns</key><array><dict><key>include</key><string>$self</string></dict></array>
                </dict>
              </dict>
            </dict>
            </plist>
            """;

        RawGrammar fromJson = RawGrammarReader.ParseRawGrammar(Json, "test.json");
        RawGrammar fromPlist = RawGrammarReader.ParseRawGrammar(Plist, "test.tmLanguage");

        Assert.Equal(Describe(fromJson), Describe(fromPlist));
    }

    private static string Describe(RawGrammar grammar)
    {
        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture, $"name={grammar.Name};scope={grammar.ScopeName};types={string.Join(",", grammar.FileTypes ?? [])};");
        text.Append(CultureInfo.InvariantCulture, $"first={grammar.FirstLineMatch};selector={grammar.InjectionSelector};");
        DescribeRules(text, "patterns", grammar.Patterns);
        DescribeRepository(text, "repository", grammar.Repository);
        DescribeRepository(text, "injections", grammar.Injections);
        return text.ToString();
    }

    private static void DescribeRule(StringBuilder text, RawRule rule)
    {
        text.Append(CultureInfo.InvariantCulture, $"{{id={rule.Id};include={rule.Include};name={rule.Name};content={rule.ContentName};match={rule.Match};");
        text.Append(CultureInfo.InvariantCulture, $"begin={rule.Begin};end={rule.End};while={rule.While};last={rule.ApplyEndPatternLast};");
        DescribeCaptures(text, "captures", rule.Captures);
        DescribeCaptures(text, "beginCaptures", rule.BeginCaptures);
        DescribeCaptures(text, "endCaptures", rule.EndCaptures);
        DescribeCaptures(text, "whileCaptures", rule.WhileCaptures);
        DescribeRules(text, "patterns", rule.Patterns);
        DescribeRepository(text, "repository", rule.Repository);
        text.Append('}');
    }

    private static void DescribeRules(StringBuilder text, string label, List<RawRule>? rules)
    {
        text.Append(label).Append('[');
        foreach (RawRule rule in rules ?? [])
        {
            DescribeRule(text, rule);
        }

        text.Append(']');
    }

    private static void DescribeRepository(StringBuilder text, string label, RawRepository? repository)
    {
        text.Append(label).Append('[');
        foreach (KeyValuePair<string, RawRule> entry in repository?.Entries ?? [])
        {
            text.Append(entry.Key).Append('=');
            DescribeRule(text, entry.Value);
        }

        text.Append(']');
    }

    private static void DescribeCaptures(StringBuilder text, string label, RawCaptures? captures)
    {
        text.Append(label).Append('[');
        foreach (string key in captures?.Keys ?? [])
        {
            text.Append(key).Append('=');
            DescribeRule(text, captures![key]);
        }

        text.Append(']');
    }
}

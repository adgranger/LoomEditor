using LoomEditor.Core.Snippets;

namespace LoomEditor.Core.Tests.Snippets;

// Monaco: suite 'SnippetParser': the tests of TextmateSnippet, Transform, FormatString and Variable, and the regressions.
public partial class SnippetParserTests
{
    private sealed class UnicodeResolver : IVariableResolver
    {
        private static readonly Dictionary<string, string> Values = new()
        {
            ["RUSSIAN"] = "одинДва",
            ["GREEK"] = "έναςΔύο",
            ["TURKISH"] = "istanbulLı",
            ["JAPANESE"] = "こんにちは",
        };

        public string? Resolve(Variable variable) => Values.GetValueOrDefault(variable.Name);
    }

    // Monaco: 'parser, parent node'
    [Fact]
    public void Parser_ParentNode()
    {
        TextmateSnippet snippet = new SnippetParser().Parse("This ${1:is ${2:nested}}$0", true);

        Assert.Equal(3, snippet.Placeholders.Count);
        Placeholder first = snippet.Placeholders[0];
        Placeholder second = snippet.Placeholders[1];
        Assert.Equal(1, first.Index);
        Assert.Equal(2, second.Index);
        Assert.Same(first, second.Parent);
        Assert.Same(snippet, first.Parent);

        snippet = new SnippetParser().Parse("${VAR:default${1:value}}$0", true);
        Assert.Equal(2, snippet.Placeholders.Count);
        first = snippet.Placeholders[0];
        Assert.Equal(1, first.Index);

        Assert.IsType<Variable>(snippet.Children[0]);
        Assert.Same(snippet.Children[0], first.Parent);
    }

    // Monaco: 'TextmateSnippet#enclosingPlaceholders'
    [Fact]
    public void TextmateSnippet_EnclosingPlaceholders()
    {
        TextmateSnippet snippet = new SnippetParser().Parse("This ${1:is ${2:nested}}$0", true);
        Placeholder first = snippet.Placeholders[0];
        Placeholder second = snippet.Placeholders[1];

        Assert.Empty(TextmateSnippet.EnclosingPlaceholders(first));
        Assert.Equal([first], TextmateSnippet.EnclosingPlaceholders(second));
    }

    // Monaco: 'TextmateSnippet#offset'
    [Fact]
    public void TextmateSnippet_Offset()
    {
        TextmateSnippet snippet = new SnippetParser().Parse("te$1xt", true);
        Assert.Equal(0, snippet.Offset(snippet.Children[0]));
        Assert.Equal(2, snippet.Offset(snippet.Children[1]));
        Assert.Equal(2, snippet.Offset(snippet.Children[2]));

        snippet = new SnippetParser().Parse("${TM_SELECTED_TEXT:def}", true);
        Assert.Equal(0, snippet.Offset(snippet.Children[0]));
        Assert.Equal(0, snippet.Offset(snippet.Children[0].Children[0]));

        // foreign marker
        Assert.Equal(-1, snippet.Offset(new TextMarker("foo")));
    }

    // Monaco: 'TextmateSnippet#placeholder'
    [Fact]
    public void TextmateSnippet_Placeholder()
    {
        Assert.Equal(2, new SnippetParser().Parse("te$1xt$0", true).Placeholders.Count);
        Assert.Equal(3, new SnippetParser().Parse("te$1xt$1$0", true).Placeholders.Count);
        Assert.Equal(3, new SnippetParser().Parse("te$1xt$2$0", true).Placeholders.Count);
        Assert.Equal(3, new SnippetParser().Parse("${1:bar${2:foo}bar}$0", true).Placeholders.Count);
    }

    // Monaco: 'TextmateSnippet#replace 1/2'
    [Fact]
    public void TextmateSnippet_Replace_1()
    {
        TextmateSnippet snippet = new SnippetParser().Parse("aaa${1:bbb${2:ccc}}$0", true);

        Assert.Equal(3, snippet.Placeholders.Count);
        Placeholder second = snippet.Placeholders[1];
        Assert.Equal(2, second.Index);

        var enclosing = TextmateSnippet.EnclosingPlaceholders(second);
        Assert.Single(enclosing);
        Assert.Equal(1, enclosing[0].Index);

        TextmateSnippet nested = new SnippetParser().Parse("ddd$1eee$0", true);
        snippet.Replace(second, nested.Children);

        Assert.Equal("aaabbbdddeee", snippet.ToString());
        Assert.Equal(4, snippet.Placeholders.Count);
        Assert.Equal(1, snippet.Placeholders[0].Index);
        Assert.Equal(1, snippet.Placeholders[1].Index);
        Assert.Equal(0, snippet.Placeholders[2].Index);
        Assert.Equal(0, snippet.Placeholders[3].Index);

        var newEnclosing = TextmateSnippet.EnclosingPlaceholders(snippet.Placeholders[1]);
        Assert.Same(snippet.Placeholders[0], newEnclosing[0]);
        Assert.Single(newEnclosing);
        Assert.Equal(1, newEnclosing[0].Index);
    }

    // Monaco: 'TextmateSnippet#replace 2/2'
    [Fact]
    public void TextmateSnippet_Replace_2()
    {
        TextmateSnippet snippet = new SnippetParser().Parse("aaa${1:bbb${2:ccc}}$0", true);

        Assert.Equal(3, snippet.Placeholders.Count);
        Placeholder second = snippet.Placeholders[1];
        Assert.Equal(2, second.Index);

        TextmateSnippet nested = new SnippetParser().Parse("dddeee$0", true);
        snippet.Replace(second, nested.Children);

        Assert.Equal("aaabbbdddeee", snippet.ToString());
        Assert.Equal(3, snippet.Placeholders.Count);
    }

    // Monaco: 'Snippet order for placeholders, #28185'
    [Fact]
    public void SnippetOrderForPlaceholders_28185() =>
        Assert.Equal(1, Placeholder.CompareByIndex(new Placeholder(10), new Placeholder(2)));

    // Monaco: 'Maximum call stack size exceeded, #28983'
    [Fact]
    public void MaximumCallStackSizeExceeded_28983() => new SnippetParser().Parse("${1:${foo:${1}}}");

    // Monaco: 'Snippet can freeze the editor, #30407'
    [Fact]
    public void SnippetCanFreezeTheEditor_30407()
    {
        var seen = new HashSet<SnippetMarker>();
        new SnippetParser().Parse("class ${1:${TM_FILENAME/(?:\\A|_)([A-Za-z0-9]+)(?:\\.rb)?/(?2::\\u$1)/g}} < ${2:Application}Controller\n  $3\nend").Walk(marker =>
        {
            Assert.True(seen.Add(marker));
            return true;
        });

        seen.Clear();
        new SnippetParser().Parse("${1:${FOO:abc$1def}}").Walk(marker =>
        {
            Assert.True(seen.Add(marker));
            return true;
        });
    }

    // Monaco: 'Transform -> FormatString#resolve'
    [Fact]
    public void Transform_FormatString_Resolve()
    {
        // shorthand functions
        Assert.Equal("FOO", new FormatString(1, "upcase").Resolve("foo"));
        Assert.Equal("foo", new FormatString(1, "downcase").Resolve("FOO"));
        Assert.Equal("Bar", new FormatString(1, "capitalize").Resolve("bar"));
        Assert.Equal("Bar no repeat", new FormatString(1, "capitalize").Resolve("bar no repeat"));
        Assert.Equal("BarFoo", new FormatString(1, "pascalcase").Resolve("bar-foo"));
        Assert.Equal("Bar42Foo", new FormatString(1, "pascalcase").Resolve("bar-42-foo"));
        Assert.Equal("SnakeAndPascalCase", new FormatString(1, "pascalcase").Resolve("snake_AndPascalCase"));
        Assert.Equal("KebabAndPascalCase", new FormatString(1, "pascalcase").Resolve("kebab-AndPascalCase"));
        Assert.Equal("JustPascalCase", new FormatString(1, "pascalcase").Resolve("_justPascalCase"));
        Assert.Equal("barFoo", new FormatString(1, "camelcase").Resolve("bar-foo"));
        Assert.Equal("bar42Foo", new FormatString(1, "camelcase").Resolve("bar-42-foo"));
        Assert.Equal("snakeAndCamelCase", new FormatString(1, "camelcase").Resolve("snake_AndCamelCase"));
        Assert.Equal("kebabAndCamelCase", new FormatString(1, "camelcase").Resolve("kebab-AndCamelCase"));
        Assert.Equal("justCamelCase", new FormatString(1, "camelcase").Resolve("_JustCamelCase"));
        Assert.Equal("bar-foo", new FormatString(1, "kebabcase").Resolve("barFoo"));
        Assert.Equal("bar-foo", new FormatString(1, "kebabcase").Resolve("BarFoo"));
        Assert.Equal("a-bar-foo", new FormatString(1, "kebabcase").Resolve("ABarFoo"));
        Assert.Equal("bar42-foo", new FormatString(1, "kebabcase").Resolve("bar42Foo"));
        Assert.Equal("snake-and-pascal-case", new FormatString(1, "kebabcase").Resolve("snake_AndPascalCase"));
        Assert.Equal("kebab-and-camel-case", new FormatString(1, "kebabcase").Resolve("kebab-AndCamelCase"));
        Assert.Equal("just-pascal-case", new FormatString(1, "kebabcase").Resolve("_justPascalCase"));
        Assert.Equal("upcase", new FormatString(1, "kebabcase").Resolve("__UPCASE__"));
        Assert.Equal("bar-foo", new FormatString(1, "kebabcase").Resolve("__BAR_FOO__"));
        Assert.Equal("bar_foo", new FormatString(1, "snakecase").Resolve("bar-foo"));
        Assert.Equal("bar_42_foo", new FormatString(1, "snakecase").Resolve("bar-42-foo"));
        Assert.Equal("snake_and_pascal_case", new FormatString(1, "snakecase").Resolve("snake_AndPascalCase"));
        Assert.Equal("kebab_and_pascal_case", new FormatString(1, "snakecase").Resolve("kebab-AndPascalCase"));
        Assert.Equal("_just_pascal_case", new FormatString(1, "snakecase").Resolve("_justPascalCase"));
        Assert.Equal("input", new FormatString(1, "notKnown").Resolve("input"));

        // if
        Assert.Equal(string.Empty, new FormatString(1, null, "foo", null).Resolve(null));
        Assert.Equal(string.Empty, new FormatString(1, null, "foo", null).Resolve(string.Empty));
        Assert.Equal("foo", new FormatString(1, null, "foo", null).Resolve("bar"));

        // else
        Assert.Equal("foo", new FormatString(1, null, null, "foo").Resolve(null));
        Assert.Equal("foo", new FormatString(1, null, null, "foo").Resolve(string.Empty));
        Assert.Equal("bar", new FormatString(1, null, null, "foo").Resolve("bar"));

        // if-else
        Assert.Equal("foo", new FormatString(1, null, "bar", "foo").Resolve(null));
        Assert.Equal("foo", new FormatString(1, null, "bar", "foo").Resolve(string.Empty));
        Assert.Equal("bar", new FormatString(1, null, "bar", "foo").Resolve("baz"));
    }

    // Monaco: 'Unicode Variable Transformations'
    [Theory]
    [InlineData("kebabcase", "RUSSIAN", "один-два")]
    [InlineData("kebabcase", "GREEK", "ένας-δύο")]
    [InlineData("snakecase", "RUSSIAN", "один_два")]
    [InlineData("snakecase", "GREEK", "ένας_δύο")]
    [InlineData("camelcase", "RUSSIAN", "одинДва")]
    [InlineData("camelcase", "GREEK", "έναςΔύο")]
    [InlineData("pascalcase", "RUSSIAN", "ОдинДва")]
    [InlineData("pascalcase", "GREEK", "ΈναςΔύο")]
    [InlineData("upcase", "RUSSIAN", "ОДИНДВА")]
    [InlineData("downcase", "RUSSIAN", "одиндва")]
    [InlineData("kebabcase", "TURKISH", "istanbul-lı")]
    [InlineData("pascalcase", "TURKISH", "IstanbulLı")]
    [InlineData("upcase", "JAPANESE", "こんにちは")]
    [InlineData("kebabcase", "JAPANESE", "こんにちは")]
    public void UnicodeVariableTransformations(string transformName, string varName, string expected)
    {
        TextmateSnippet snippet = new SnippetParser().Parse("${" + varName + "/(.*)/${1:/" + transformName + "}/}");
        var variable = (Variable)snippet.Children[0];

        variable.Resolve(new UnicodeResolver());

        Assert.Equal(expected, variable.ToString());
    }

    // Monaco: 'Snippet variable transformation doesn't work if regex is complicated and snippet body contains '$$' #55627'
    [Fact]
    public void SnippetVariableTransformationWithComplicatedRegex_55627()
    {
        TextmateSnippet snippet = new SnippetParser().Parse("const fileName = \"${TM_FILENAME/(.*)\\..+$/$1/}\"");

        Assert.Equal("const fileName = \"${TM_FILENAME/(.*)\\..+$/${1}/}\"", snippet.ToTextmateString());
    }

    // Monaco: '[BUG] HTML attribute suggestions: Snippet session does not have end-position set, #33147'
    [Fact]
    public void HtmlAttributeSuggestionsNoEndPosition_33147()
    {
        var placeholders = new SnippetParser().Parse("src=\"$1\"", true).Placeholders;

        Assert.Equal(2, placeholders.Count);
        Assert.Equal(1, placeholders[0].Index);
        Assert.Equal(0, placeholders[1].Index);
    }

    // Monaco: 'Snippet optional transforms are not applied correctly when reusing the same variable, #37702'
    [Fact]
    public void SnippetOptionalTransformsWhenReusingTheSameVariable_37702()
    {
        var transform = new Transform();
        transform.AppendChild(new FormatString(1, "upcase"));
        transform.AppendChild(new FormatString(2, "upcase"));
        transform.SetRegexp("^(.)|-(.)", "g");

        Assert.Equal("MyFileName", transform.Resolve("my-file-name"));

        var clone = (Transform)transform.Clone();
        Assert.Equal("MyFileName", clone.Resolve("my-file-name"));
    }

    // Monaco: 'Variable transformation doesn't work if undefined variables are used in the same snippet #51769'
    [Fact]
    public void VariableTransformationWithUndefinedVariables_51769()
    {
        var transform = new Transform();
        transform.AppendChild(new TextMarker("bar"));
        transform.SetRegexp("foo", "gi");

        Assert.Equal("/foo/bar/ig", transform.ToTextmateString());
    }

    // Monaco: 'transform serialization joins children without comma'
    [Fact]
    public void TransformSerializationJoinsChildrenWithoutComma()
    {
        var transformWithFormatString = new Transform();
        transformWithFormatString.AppendChild(new FormatString(1, "upcase"));
        transformWithFormatString.AppendChild(new TextMarker("_"));
        transformWithFormatString.SetRegexp("foo", "g");
        string serialized = transformWithFormatString.ToTextmateString();
        Assert.Equal("/foo/${1:/upcase}_/g", serialized);

        TextmateSnippet snippet = new SnippetParser().Parse("${TM_FILENAME" + serialized + "}");
        Assert.Equal("${TM_FILENAME" + serialized + "}", snippet.ToTextmateString());
    }

    // Monaco: 'Mirroring sequence of nested placeholders not selected properly on backjumping #58736'
    [Fact]
    public void MirroringSequenceOfNestedPlaceholders_58736()
    {
        TextmateSnippet snippet = new SnippetParser().Parse("${3:nest1 ${1:nest2 ${2:nest3}}} $3");
        Assert.Equal(3, snippet.Children.Count);
        Assert.IsType<Placeholder>(snippet.Children[0]);
        Assert.IsType<TextMarker>(snippet.Children[1]);
        Assert.IsType<Placeholder>(snippet.Children[2]);

        void AssertParent(SnippetMarker marker)
        {
            foreach (SnippetMarker child in marker.Children)
            {
                AssertParent(child);
            }

            if (marker is not Placeholder)
            {
                return;
            }

            bool found = false;
            SnippetMarker? m = marker;
            while (m is not null && !found)
            {
                if (ReferenceEquals(m.Parent, snippet))
                {
                    found = true;
                }

                m = m.Parent;
            }

            Assert.True(found);
        }

        AssertParent(snippet.Children[2]);
    }

    [Fact]
    public void Parse_Empty_ReturnsEmptySnippet()
    {
        TextmateSnippet snippet = new SnippetParser().Parse(string.Empty);

        Assert.Empty(snippet.Children);
        Assert.Empty(snippet.Placeholders);
        Assert.Equal(string.Empty, snippet.ToString());
    }

    [Fact]
    public void Parse_InsertFinalTabstop_AppendsDollarZero()
    {
        TextmateSnippet snippet = new SnippetParser().Parse("a ${1:b}", insertFinalTabstop: true);

        Assert.Equal("a ${1:b}$0", snippet.ToTextmateString());
        Assert.Equal(0, snippet.Placeholders[^1].Index);
    }

    [Fact]
    public void Parse_InsertFinalTabstop_ExistingFinalKept()
    {
        TextmateSnippet snippet = new SnippetParser().Parse("$1 $0 x", insertFinalTabstop: true);

        Assert.Equal(2, snippet.Placeholders.Count);
        Assert.Equal("$1 $0 x", snippet.ToTextmateString());
    }

    [Fact]
    public void Parse_EnforceFinalTabstop_AppendsEvenWithoutPlaceholders() =>
        Assert.Equal("text$0", new SnippetParser().Parse("text", enforceFinalTabstop: true).ToTextmateString());

    [Fact]
    public void Parse_InvalidRegexInTransform_FallsBackToText() =>
        AssertTextAndMarker("${foo/(/x/}", "${foo/(/x/}", TextType);

    [Fact]
    public void Parse_UnterminatedPlaceholder_FallsBackToText() =>
        AssertTextAndMarker("a ${1:b", "a ${1:b", TextType);

    [Fact]
    public void Escape_Roundtrips()
    {
        const string raw = "a$b}c\\d${1:x}";

        Assert.Equal(raw, SnippetParser.Text(SnippetParser.Escape(raw)));
    }

    [Fact]
    public void GuessNeedsClipboard_True_ForClipboardVariable()
    {
        Assert.True(SnippetParser.GuessNeedsClipboard("a ${CLIPBOARD} b"));
        Assert.True(SnippetParser.GuessNeedsClipboard("$CLIPBOARD"));
        Assert.False(SnippetParser.GuessNeedsClipboard("${TM_FILENAME}"));
    }
}

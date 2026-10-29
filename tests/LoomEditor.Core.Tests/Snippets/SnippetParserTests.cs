using LoomEditor.Core.Snippets;

namespace LoomEditor.Core.Tests.Snippets;

// Monaco: suite 'SnippetParser' (snippetParser.test.ts), every test after 'Scanner'. The TextmateSnippet ones are in
// SnippetParserTests.TextmateSnippet.cs.
public partial class SnippetParserTests
{
    private static void AssertText(string value, string expected) => Assert.Equal(expected, SnippetParser.Text(value));

    private static void AssertMarker(IReadOnlyList<SnippetMarker> markers, params Type[] types)
    {
        Assert.Equal(types.Length, markers.Count);
        for (int i = 0; i < types.Length; i++)
        {
            Assert.True(types[i].IsInstanceOfType(markers[i]), $"child {i}: expected {types[i].Name}, got {markers[i].GetType().Name}");
        }
    }

    private static void AssertMarker(string input, params Type[] types) => AssertMarker(new SnippetParser().Parse(input).Children, types);

    private static void AssertTextAndMarker(string value, string escaped, params Type[] types)
    {
        AssertText(value, escaped);
        AssertMarker(value, types);
    }

    private static void AssertEscaped(string value, string expected) => Assert.Equal(expected, SnippetParser.Escape(value));

    private static readonly Type TextType = typeof(TextMarker);
    private static readonly Type PlaceholderType = typeof(Placeholder);
    private static readonly Type VariableType = typeof(Variable);

    // Monaco: 'Parser, escaped'
    [Fact]
    public void Parser_Escaped()
    {
        AssertEscaped("foo$0", "foo\\$0");
        AssertEscaped("foo\\$0", "foo\\\\\\$0");
        AssertEscaped("f$1oo$0", "f\\$1oo\\$0");
        AssertEscaped("${1:foo}$0", "\\${1:foo\\}\\$0");
        AssertEscaped("$", "\\$");
    }

    // Monaco: 'Parser, text'
    [Fact]
    public void Parser_Text()
    {
        AssertText("$", "$");
        AssertText("\\\\$", "\\$");
        AssertText("{", "{");
        AssertText("\\}", "}");
        AssertText("\\abc", "\\abc");
        AssertText("foo${f:\\}}bar", "foo}bar");
        AssertText("\\{", "\\{");
        AssertText("I need \\\\\\$", "I need \\$");
        AssertText("\\", "\\");
        AssertText("\\{{", "\\{{");
        AssertText("{{", "{{");
        AssertText("{{dd", "{{dd");
        AssertText("}}", "}}");
        AssertText("ff}}", "ff}}");

        AssertText("farboo", "farboo");
        AssertText("far{{}}boo", "far{{}}boo");
        AssertText("far{{123}}boo", "far{{123}}boo");
        AssertText("far\\{{123}}boo", "far\\{{123}}boo");
        AssertText("far{{id:bern}}boo", "far{{id:bern}}boo");
        AssertText("far{{id:bern {{basel}}}}boo", "far{{id:bern {{basel}}}}boo");
        AssertText("far{{id:bern {{id:basel}}}}boo", "far{{id:bern {{id:basel}}}}boo");
        AssertText("far{{id:bern {{id2:basel}}}}boo", "far{{id:bern {{id2:basel}}}}boo");
    }

    // Monaco: 'Parser, TM text'
    [Fact]
    public void Parser_TmText()
    {
        AssertTextAndMarker("foo${1:bar}}", "foobar}", TextType, PlaceholderType, TextType);
        AssertTextAndMarker("foo${1:bar}${2:foo}}", "foobarfoo}", TextType, PlaceholderType, PlaceholderType, TextType);

        AssertTextAndMarker("foo${1:bar\\}${2:foo}}", "foobar}foo", TextType, PlaceholderType);

        var placeholder = (Placeholder)new SnippetParser().Parse("foo${1:bar\\}${2:foo}}").Children[1];
        var children = placeholder.Children;

        Assert.Equal(1, placeholder.Index);
        Assert.IsType<TextMarker>(children[0]);
        Assert.Equal("bar}", children[0].ToString());
        Assert.IsType<Placeholder>(children[1]);
        Assert.Equal("foo", children[1].ToString());
    }

    // Monaco: 'Parser, placeholder'
    [Fact]
    public void Parser_Placeholder()
    {
        AssertTextAndMarker("farboo", "farboo", TextType);
        AssertTextAndMarker("far{{}}boo", "far{{}}boo", TextType);
        AssertTextAndMarker("far{{123}}boo", "far{{123}}boo", TextType);
        AssertTextAndMarker("far\\{{123}}boo", "far\\{{123}}boo", TextType);
    }

    // Monaco: 'Parser, literal code'
    [Fact]
    public void Parser_LiteralCode()
    {
        AssertTextAndMarker("far`123`boo", "far`123`boo", TextType);
        AssertTextAndMarker("far\\`123\\`boo", "far\\`123\\`boo", TextType);
    }

    // Monaco: 'Parser, variables/tabstop'
    [Fact]
    public void Parser_VariablesTabstop()
    {
        AssertTextAndMarker("$far-boo", "-boo", VariableType, TextType);
        AssertTextAndMarker("\\$far-boo", "$far-boo", TextType);
        AssertTextAndMarker("far$farboo", "far", TextType, VariableType);
        AssertTextAndMarker("far${farboo}", "far", TextType, VariableType);
        AssertTextAndMarker("$123", string.Empty, PlaceholderType);
        AssertTextAndMarker("$farboo", string.Empty, VariableType);
        AssertTextAndMarker("$far12boo", string.Empty, VariableType);
        AssertTextAndMarker("000_${far}_000", "000__000", TextType, VariableType, TextType);
        AssertTextAndMarker("FFF_${TM_SELECTED_TEXT}_FFF$0", "FFF__FFF", TextType, VariableType, TextType, PlaceholderType);
    }

    // Monaco: 'Parser, variables/placeholder with defaults'
    [Fact]
    public void Parser_VariablesPlaceholderWithDefaults()
    {
        AssertTextAndMarker("${name:value}", "value", VariableType);
        AssertTextAndMarker("${1:value}", "value", PlaceholderType);
        AssertTextAndMarker("${1:bar${2:foo}bar}", "barfoobar", PlaceholderType);

        AssertTextAndMarker("${name:value", "${name:value", TextType);
        AssertTextAndMarker("${1:bar${2:foobar}", "${1:barfoobar", TextType, PlaceholderType);
    }

    // Monaco: 'Parser, variable transforms'
    [Fact]
    public void Parser_VariableTransforms()
    {
        AssertTextAndMarker("${foo///}", string.Empty, VariableType);
        AssertTextAndMarker("${foo/regex/format/gmi}", string.Empty, VariableType);
        AssertTextAndMarker("${foo/([A-Z][a-z])/format/}", string.Empty, VariableType);

        // invalid regex
        AssertTextAndMarker("${foo/([A-Z][a-z])/format/GMI}", "${foo/([A-Z][a-z])/format/GMI}", TextType);
        AssertTextAndMarker("${foo/([A-Z][a-z])/format/funky}", "${foo/([A-Z][a-z])/format/funky}", TextType);
        AssertTextAndMarker("${foo/([A-Z][a-z]/format/}", "${foo/([A-Z][a-z]/format/}", TextType);

        // tricky regex
        AssertTextAndMarker("${foo/m\\/atch/$1/i}", string.Empty, VariableType);
        AssertMarker("${foo/regex/format/options}", TextType);

        // incomplete
        AssertTextAndMarker("${foo///", "${foo///", TextType);
        AssertTextAndMarker("${foo/regex/format/options", "${foo/regex/format/options", TextType);

        // format string
        AssertMarker("${foo/.*/${0:fooo}/i}", VariableType);
        AssertMarker("${foo/.*/${1}/i}", VariableType);
        AssertMarker("${foo/.*/$1/i}", VariableType);
        AssertMarker("${foo/.*/This-$1-encloses/i}", VariableType);
        AssertMarker("${foo/.*/complex${1:else}/i}", VariableType);
        AssertMarker("${foo/.*/complex${1:-else}/i}", VariableType);
        AssertMarker("${foo/.*/complex${1:+if}/i}", VariableType);
        AssertMarker("${foo/.*/complex${1:?if:else}/i}", VariableType);
        AssertMarker("${foo/.*/complex${1:/upcase}/i}", VariableType);
    }

    // Monaco: 'Parser, placeholder transforms'
    [Fact]
    public void Parser_PlaceholderTransforms()
    {
        AssertTextAndMarker("${1///}", string.Empty, PlaceholderType);
        AssertTextAndMarker("${1/regex/format/gmi}", string.Empty, PlaceholderType);
        AssertTextAndMarker("${1/([A-Z][a-z])/format/}", string.Empty, PlaceholderType);

        // tricky regex
        AssertTextAndMarker("${1/m\\/atch/$1/i}", string.Empty, PlaceholderType);
        AssertMarker("${1/regex/format/options}", TextType);

        // incomplete
        AssertTextAndMarker("${1///", "${1///", TextType);
        AssertTextAndMarker("${1/regex/format/options", "${1/regex/format/options", TextType);
    }

    // Monaco: 'No way to escape forward slash in snippet regex #36715'
    [Fact]
    public void NoWayToEscapeForwardSlashInSnippetRegex_36715() => AssertMarker("${TM_DIRECTORY/src\\//$1/}", VariableType);

    // Monaco: 'No way to escape forward slash in snippet format section #37562'
    [Fact]
    public void NoWayToEscapeForwardSlashInSnippetFormatSection_37562()
    {
        AssertMarker("${TM_SELECTED_TEXT/a/\\/$1/g}", VariableType);
        AssertMarker("${TM_SELECTED_TEXT/a/in\\/$1ner/g}", VariableType);
        AssertMarker("${TM_SELECTED_TEXT/a/end\\//g}", VariableType);
    }

    // Monaco: 'Parser, placeholder with choice'
    [Fact]
    public void Parser_PlaceholderWithChoice()
    {
        AssertTextAndMarker("${1|one,two,three|}", "one", PlaceholderType);
        AssertTextAndMarker("${1|one|}", "one", PlaceholderType);
        AssertTextAndMarker("${1|one1,two2|}", "one1", PlaceholderType);
        AssertTextAndMarker("${1|one1\\,two2|}", "one1,two2", PlaceholderType);
        AssertTextAndMarker("${1|one1\\|two2|}", "one1|two2", PlaceholderType);
        AssertTextAndMarker("${1|one1\\atwo2|}", "one1\\atwo2", PlaceholderType);
        AssertTextAndMarker("${1|one,two,three,|}", "${1|one,two,three,|}", TextType);
        AssertTextAndMarker("${1|one,", "${1|one,", TextType);

        TextmateSnippet snippet = new SnippetParser().Parse("${1|one,two,three|}");
        var expected = new Queue<Func<SnippetMarker, bool>>(
        [
            marker => marker is Placeholder,
            marker => marker is Choice { Options.Count: 3 },
        ]);
        snippet.Walk(marker =>
        {
            Assert.True(expected.Dequeue()(marker));
            return true;
        });
    }

    // Monaco: 'Snippet choices: unable to escape comma and pipe, #31521'
    [Fact]
    public void SnippetChoices_UnableToEscapeCommaAndPipe_31521() =>
        AssertTextAndMarker("console.log(${1|not\\, not, five, 5, 1   23|});", "console.log(not, not);", TextType, PlaceholderType, TextType);

    // Monaco: 'Marker, toTextmateString()'
    [Fact]
    public void Marker_ToTextmateString()
    {
        static void AssertTextsnippetString(string input, string expected) =>
            Assert.Equal(expected, new SnippetParser().Parse(input).ToTextmateString());

        AssertTextsnippetString("$1", "$1");
        AssertTextsnippetString("\\$1", "\\$1");
        AssertTextsnippetString("console.log(${1|not\\, not, five, 5, 1   23|});", "console.log(${1|not\\, not, five, 5, 1   23|});");
        AssertTextsnippetString("console.log(${1|not\\, not, \\| five, 5, 1   23|});", "console.log(${1|not\\, not, \\| five, 5, 1   23|});");
        AssertTextsnippetString("${1|cho\\,ices,wi\\|th,esc\\\\aping,chall\\\\\\,enges|}", "${1|cho\\,ices,wi\\|th,esc\\\\aping,chall\\\\\\,enges|}");
        AssertTextsnippetString("this is text", "this is text");
        AssertTextsnippetString("this ${1:is ${2:nested with $var}}", "this ${1:is ${2:nested with ${var}}}");
        AssertTextsnippetString("this ${1:is ${2:nested with $var}}}", "this ${1:is ${2:nested with ${var}}}\\}");
    }

    // Monaco: 'Marker, toTextmateString() <-> identity'
    [Fact]
    public void Marker_ToTextmateString_Identity()
    {
        static void AssertIdent(string input)
        {
            // Full loop: (1) parse input, (2) generate textmate string, (3) parse, (4) ensure both trees are equal.
            TextmateSnippet snippet = new SnippetParser().Parse(input);
            string input2 = snippet.ToTextmateString();
            TextmateSnippet snippet2 = new SnippetParser().Parse(input2);

            static void CheckChildren(SnippetMarker marker1, SnippetMarker marker2)
            {
                Assert.Equal(marker1.GetType(), marker2.GetType());
                Assert.Equal(marker1.Children.Count, marker2.Children.Count);
                Assert.Equal(marker1.ToString(), marker2.ToString());
                for (int i = 0; i < marker1.Children.Count; i++)
                {
                    CheckChildren(marker1.Children[i], marker2.Children[i]);
                }
            }

            CheckChildren(snippet, snippet2);
        }

        AssertIdent("$1");
        AssertIdent("\\$1");
        AssertIdent("console.log(${1|not\\, not, five, 5, 1   23|});");
        AssertIdent("console.log(${1|not\\, not, \\| five, 5, 1   23|});");
        AssertIdent("this is text");
        AssertIdent("this ${1:is ${2:nested with $var}}");
        AssertIdent("this ${1:is ${2:nested with $var}}}");
        AssertIdent("this ${1:is ${2:nested with $var}} and repeating $1");
    }

    // Monaco: 'Parser, choise marker'
    [Fact]
    public void Parser_ChoiceMarker()
    {
        var placeholders = new SnippetParser().Parse("${1|one,two,three|}").Placeholders;

        Assert.Single(placeholders);
        Assert.IsType<Choice>(placeholders[0].Choice);
        Assert.IsType<Choice>(placeholders[0].Children[0]);
        Assert.Equal(3, ((Choice)placeholders[0].Children[0]).Options.Count);

        AssertText("${1|one,two,three|}", "one");
        AssertText("\\${1|one,two,three|}", "${1|one,two,three|}");
        AssertText("${1\\|one,two,three|}", "${1\\|one,two,three|}");
        AssertText("${1||}", "${1||}");
    }

    // Monaco: 'Backslash character escape in choice tabstop doesn't work #58494'
    [Fact]
    public void BackslashCharacterEscapeInChoiceTabstop_58494()
    {
        var placeholders = new SnippetParser().Parse("${1|\\,,},$,\\|,\\\\|}").Placeholders;

        Assert.Single(placeholders);
        Assert.IsType<Choice>(placeholders[0].Choice);
    }

    // Monaco: 'Parser, only textmate'
    [Fact]
    public void Parser_OnlyTextmate()
    {
        var p = new SnippetParser();
        AssertMarker(p.Parse("far{{}}boo").Children, TextType);
        AssertMarker(p.Parse("far{{123}}boo").Children, TextType);
        AssertMarker(p.Parse("far\\{{123}}boo").Children, TextType);

        AssertMarker(p.Parse("far$0boo").Children, TextType, PlaceholderType, TextType);
        AssertMarker(p.Parse("far${123}boo").Children, TextType, PlaceholderType, TextType);
        AssertMarker(p.Parse("far\\${123}boo").Children, TextType);
    }

    // Monaco: 'Parser, real world'
    [Fact]
    public void Parser_RealWorld()
    {
        var marker = new SnippetParser().Parse("console.warn(${1: $TM_SELECTED_TEXT })").Children;

        Assert.Equal("console.warn(", marker[0].ToString());
        Assert.IsType<Placeholder>(marker[1]);
        Assert.Equal(")", marker[2].ToString());

        var placeholder = (Placeholder)marker[1];
        Assert.Equal(1, placeholder.Index);
        Assert.Equal(3, placeholder.Children.Count);
        Assert.IsType<TextMarker>(placeholder.Children[0]);
        Assert.IsType<Variable>(placeholder.Children[1]);
        Assert.IsType<TextMarker>(placeholder.Children[2]);
        Assert.Equal(" ", placeholder.Children[0].ToString());
        Assert.Equal(string.Empty, placeholder.Children[1].ToString());
        Assert.Equal(" ", placeholder.Children[2].ToString());

        var nestedVariable = (Variable)placeholder.Children[1];
        Assert.Equal("TM_SELECTED_TEXT", nestedVariable.Name);
        Assert.Empty(nestedVariable.Children);

        marker = new SnippetParser().Parse("$TM_SELECTED_TEXT").Children;
        Assert.Single(marker);
        Assert.IsType<Variable>(marker[0]);
    }

    // Monaco: 'Parser, transform example'
    [Fact]
    public void Parser_TransformExample()
    {
        var children = new SnippetParser().Parse("${1:name} : ${2:type}${3/\\s:=(.*)/${1:+ :=}${1}/};\n$0").Children;

        // ${1:name}
        Assert.IsType<Placeholder>(children[0]);
        Assert.Single(children[0].Children);
        Assert.Equal("name", children[0].Children[0].ToString());
        Assert.Null(((Placeholder)children[0]).Transform);

        // :
        Assert.IsType<TextMarker>(children[1]);
        Assert.Equal(" : ", children[1].ToString());

        // ${2:type}
        Assert.IsType<Placeholder>(children[2]);
        Assert.Single(children[2].Children);
        Assert.Equal("type", children[2].Children[0].ToString());

        // ${3/\\s:=(.*)/${1:+ :=}${1}/}
        Assert.IsType<Placeholder>(children[3]);
        Assert.Empty(children[3].Children);
        Transform transform = ((Placeholder)children[3]).Transform!;
        Assert.NotNull(transform);
        Assert.Equal("\\s:=(.*)", transform.Source);
        Assert.Equal(2, transform.Children.Count);
        Assert.IsType<FormatString>(transform.Children[0]);
        Assert.Equal(1, ((FormatString)transform.Children[0]).Index);
        Assert.Equal(" :=", ((FormatString)transform.Children[0]).IfValue);
        Assert.IsType<FormatString>(transform.Children[1]);
        Assert.Equal(1, ((FormatString)transform.Children[1]).Index);
        Assert.IsType<TextMarker>(children[4]);
        Assert.Equal(";\n", children[4].ToString());
    }

    // Monaco: 'Parser, default placeholder values'
    [Fact]
    public void Parser_DefaultPlaceholderValues()
    {
        AssertMarker("errorContext: `${1:err}`, error: $1", TextType, PlaceholderType, TextType, PlaceholderType);

        var children = new SnippetParser().Parse("errorContext: `${1:err}`, error:$1").Children;
        var p1 = (Placeholder)children[1];
        var p2 = (Placeholder)children[3];

        Assert.Equal(1, p1.Index);
        Assert.Single(p1.Children);
        Assert.Equal("err", p1.Children[0].ToString());

        Assert.Equal(1, p2.Index);
        Assert.Single(p2.Children);
        Assert.Equal("err", p2.Children[0].ToString());
    }

    // Monaco: 'Parser, default placeholder values and one transform'
    [Fact]
    public void Parser_DefaultPlaceholderValuesAndOneTransform()
    {
        AssertMarker("errorContext: `${1:err}`, error: ${1/err/ok/}", TextType, PlaceholderType, TextType, PlaceholderType);

        var children = new SnippetParser().Parse("errorContext: `${1:err}`, error:${1/err/ok/}").Children;
        var p3 = (Placeholder)children[1];
        var p4 = (Placeholder)children[3];

        Assert.Equal(1, p3.Index);
        Assert.Single(p3.Children);
        Assert.Equal("err", p3.Children[0].ToString());
        Assert.Null(p3.Transform);

        Assert.Equal(1, p4.Index);
        Assert.Single(p4.Children);
        Assert.Equal("err", p4.Children[0].ToString());
        Assert.NotNull(p4.Transform);
    }

    // Monaco: 'Repeated snippet placeholder should always inherit, #31040'
    [Fact]
    public void RepeatedSnippetPlaceholderShouldAlwaysInherit_31040()
    {
        AssertText("${1:foo}-abc-$1", "foo-abc-foo");
        AssertText("${1:foo}-abc-${1}", "foo-abc-foo");
        AssertText("${1:foo}-abc-${1:bar}", "foo-abc-foo");
        AssertText("${1}-abc-${1:foo}", "foo-abc-foo");
    }

    // Monaco: 'backspace esapce in TM only, #16212'
    [Fact]
    public void BackspaceEscapeInTmOnly_16212() => Assert.Equal("Foo \\bar", SnippetParser.Text("Foo \\\\${abc}bar"));

    // Monaco: 'colon as variable/placeholder value, #16717'
    [Fact]
    public void ColonAsVariablePlaceholderValue_16717()
    {
        Assert.Equal("foo:bar", SnippetParser.Text("${TM_SELECTED_TEXT:foo:bar}"));
        Assert.Equal("foo:bar", SnippetParser.Text("${1:foo:bar}"));
    }

    // Monaco: 'incomplete placeholder'
    [Fact]
    public void IncompletePlaceholder() => AssertTextAndMarker("${1:}", string.Empty, PlaceholderType);

    // Monaco: 'marker#len'
    [Fact]
    public void Marker_Len()
    {
        static void AssertLen(string template, params int[] lengths)
        {
            var remaining = new Queue<int>(lengths);
            new SnippetParser().Parse(template, true).Walk(marker =>
            {
                Assert.Equal(remaining.Dequeue(), marker.Length);
                return true;
            });
            Assert.Empty(remaining);
        }

        AssertLen("text$0", 4, 0);
        AssertLen("$1text$0", 0, 4, 0);
        AssertLen("te$1xt$0", 2, 0, 2, 0);
        AssertLen("errorContext: `${1:err}`, error: $0", 15, 0, 3, 10, 0);
        AssertLen("errorContext: `${1:err}`, error: $1$0", 15, 0, 3, 10, 0, 3, 0);
        AssertLen("$TM_SELECTED_TEXT$0", 0, 0);
        AssertLen("${TM_SELECTED_TEXT:def}$0", 0, 3, 0);
    }

    // Monaco: 'Snippets: make parser ignore `${0|choice|}`, #31599'
    [Fact]
    public void SnippetsMakeParserIgnoreZeroChoice_31599()
    {
        AssertTextAndMarker("${0|foo,bar|}", "${0|foo,bar|}", TextType);
        AssertTextAndMarker("${1|foo,bar|}", "foo", PlaceholderType);
    }

    // Monaco: 'Snippet choices are incorrectly escaped/applied #180132'
    [Fact]
    public void SnippetChoicesAreIncorrectlyEscapedApplied_180132() =>
        AssertTextAndMarker("${1|aaa$aaa|}bbb\\$bbb", "aaa$aaabbb$bbb", PlaceholderType, TextType);

    // Monaco: 'snippets variable not resolved in JSON proposal #52931'
    [Fact]
    public void SnippetsVariableNotResolvedInJsonProposal_52931() =>
        AssertTextAndMarker("FOO${1:/bin/bash}", "FOO/bin/bash", TextType, PlaceholderType);

    // Monaco: 'Snippet snippet parser freeze #53144'
    [Fact]
    public void SnippetParserFreeze_53144() => AssertMarker("${1/(void$)|(.+)/${1:?-\treturn nil;}/}", PlaceholderType);

    // Monaco: 'problem with snippets regex #40570'
    [Fact]
    public void ProblemWithSnippetsRegex_40570() => AssertMarker("${TM_DIRECTORY/.*src[\\/](.*)/$1/}", VariableType);

    // Monaco: 'Backspace can't be escaped in snippet variable transforms #65412'
    [Fact]
    public void BackspaceCannotBeEscapedInSnippetVariableTransforms_65412() =>
        AssertMarker("namespace ${TM_DIRECTORY/[\\/]/\\\\/g};", TextType, VariableType, TextType);

    // Monaco: 'Snippet cannot escape closing bracket inside conditional insertion variable replacement #78883'
    [Fact]
    public void SnippetCannotEscapeClosingBracketInsideConditionalInsertion_78883()
    {
        TextmateSnippet snippet = new SnippetParser().Parse("${TM_DIRECTORY/(.+)/${1:+import { hello \\} from world}/}");

        Assert.Single(snippet.Children);
        var variable = Assert.IsType<Variable>(snippet.Children[0]);
        Assert.NotNull(variable.Transform);
        Assert.Single(variable.Transform.Children);
        var format = Assert.IsType<FormatString>(variable.Transform.Children[0]);
        Assert.Equal("import { hello } from world", format.IfValue);
        Assert.Null(format.ElseValue);
    }

    // Monaco: 'Snippet escape backslashes inside conditional insertion variable replacement #80394'
    [Fact]
    public void SnippetEscapeBackslashesInsideConditionalInsertion_80394()
    {
        TextmateSnippet snippet = new SnippetParser().Parse("${CURRENT_YEAR/(.+)/${1:+\\\\}/}");

        Assert.Single(snippet.Children);
        var variable = Assert.IsType<Variable>(snippet.Children[0]);
        Assert.NotNull(variable.Transform);
        Assert.Single(variable.Transform.Children);
        var format = Assert.IsType<FormatString>(variable.Transform.Children[0]);
        Assert.Equal("\\", format.IfValue);
        Assert.Null(format.ElseValue);
    }

    // Monaco: 'Snippet placeholder empty right after expansion #152553'
    [Fact]
    public void SnippetPlaceholderEmptyRightAfterExpansion_152553()
    {
        Assert.Equal("prog: prog.cc - prog.cc", new SnippetParser().Parse("${1:prog}: ${2:$1.cc} - $2").ToString());
        Assert.Equal(
            "prog: prog.cc.33 - prog.cc prog.cc.33",
            new SnippetParser().Parse("${1:prog}: ${3:${2:$1.cc}.33} - $2 $3").ToString());

        // cyclic references of placeholders
        Assert.Equal(".two.one.two.one <> .one.two.one.two", new SnippetParser().Parse("${1:$2.one} <> ${2:$1.two}").ToString());
    }
}

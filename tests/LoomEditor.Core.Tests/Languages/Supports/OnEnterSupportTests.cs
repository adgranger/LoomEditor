// Ported from microsoft/vscode: src/vs/editor/test/common/modes/supports/onEnter.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Tests.Languages.TestHelpers;

namespace LoomEditor.Core.Tests.Languages.Supports;

public class OnEnterSupportTests
{
    private static void TestIndentAction(OnEnterSupport support, string beforeText, string afterText, IndentAction expected)
    {
        EnterAction? actual = support.OnEnter(EditorAutoIndentStrategy.Advanced, "", beforeText, afterText);
        if (expected == IndentAction.None)
        {
            Assert.Null(actual);
        }
        else
        {
            Assert.Equal(expected, actual!.Value.IndentAction);
        }
    }

    private static void TestIndentAction(OnEnterSupport support, string previousLineText, string beforeText, string afterText, IndentAction? expectedIndentAction, string? expectedAppendText, int removeText = 0)
    {
        EnterAction? actual = support.OnEnter(EditorAutoIndentStrategy.Advanced, previousLineText, beforeText, afterText);
        if (expectedIndentAction is null)
        {
            Assert.Null(actual);
        }
        else
        {
            Assert.NotNull(actual);
            Assert.Equal(expectedIndentAction, actual.Value.IndentAction);
            if (expectedAppendText is not null)
            {
                Assert.Equal(expectedAppendText, actual.Value.AppendText);
            }

            if (removeText != 0)
            {
                Assert.Equal(removeText, actual.Value.RemoveText);
            }
        }
    }

    // Monaco: 'uses brackets'
    [Fact]
    public void UsesBrackets()
    {
        CharacterPair[] brackets =
        [
            new("(", ")"),
            new("begin", "end"),
        ];
        var support = new OnEnterSupport(brackets, null);

        TestIndentAction(support, "a", "", IndentAction.None);
        TestIndentAction(support, "", "b", IndentAction.None);
        TestIndentAction(support, "(", "b", IndentAction.Indent);
        TestIndentAction(support, "a", ")", IndentAction.None);
        TestIndentAction(support, "begin", "ending", IndentAction.Indent);
        TestIndentAction(support, "abegin", "end", IndentAction.None);
        TestIndentAction(support, "begin", ")", IndentAction.Indent);
        TestIndentAction(support, "begin", "end", IndentAction.IndentOutdent);
        TestIndentAction(support, "begin ", " end", IndentAction.IndentOutdent);
        TestIndentAction(support, " begin", "end//as", IndentAction.IndentOutdent);
        TestIndentAction(support, "(", ")", IndentAction.IndentOutdent);
        TestIndentAction(support, "( ", ")", IndentAction.IndentOutdent);
        TestIndentAction(support, "a(", ")b", IndentAction.IndentOutdent);

        TestIndentAction(support, "(", "", IndentAction.Indent);
        TestIndentAction(support, "(", "foo", IndentAction.Indent);
        TestIndentAction(support, "begin", "foo", IndentAction.Indent);
        TestIndentAction(support, "begin", "", IndentAction.Indent);
    }

    // Monaco: 'Issue #121125: onEnterRules with global modifier'
    [Fact]
    public void Issue121125_OnEnterRulesWithGlobalModifier()
    {
        var support = new OnEnterSupport(null,
        [
            new OnEnterRule(
                Action: new EnterAction(IndentAction.Outdent, AppendText: "/// "),
                BeforeText: new Regex(@"^\s*\/{3}.*$", RegexOptions.CultureInvariant | RegexOptions.Multiline)),
        ]);

        TestIndentAction(support, "/// line", "/// line", "", IndentAction.Outdent, "/// ");
        TestIndentAction(support, "/// line", "/// line", "", IndentAction.Outdent, "/// ");
    }

    // Monaco: 'uses regExpRules'
    [Fact]
    public void UsesRegExpRules()
    {
        var support = new OnEnterSupport(null, OnEnterRules.JavascriptOnEnterRules);

        TestIndentAction(support, "", "\t/**", " */", IndentAction.IndentOutdent, " * ");
        TestIndentAction(support, "", "\t/**", "", IndentAction.None, " * ");
        TestIndentAction(support, "", "\t/** * / * / * /", "", IndentAction.None, " * ");
        TestIndentAction(support, "", "\t/** /*", "", IndentAction.None, " * ");
        TestIndentAction(support, "", "/**", "", IndentAction.None, " * ");
        TestIndentAction(support, "", "\t/**/", "", null, null);
        TestIndentAction(support, "", "\t/***/", "", null, null);
        TestIndentAction(support, "", "\t/*******/", "", null, null);
        TestIndentAction(support, "", "\t/** * * * * */", "", null, null);
        TestIndentAction(support, "", "\t/** */", "", null, null);
        TestIndentAction(support, "", "\t/** asdfg */", "", null, null);
        TestIndentAction(support, "", "\t/* asdfg */", "", null, null);
        TestIndentAction(support, "", "\t/* asdfg */", "", null, null);
        TestIndentAction(support, "", "\t/** asdfg */", "", null, null);
        TestIndentAction(support, "", "*/", "", null, null);
        TestIndentAction(support, "", "\t/*", "", null, null);
        TestIndentAction(support, "", "\t*", "", null, null);

        TestIndentAction(support, "\t/**", "\t *", "", IndentAction.None, "* ");
        TestIndentAction(support, "\t * something", "\t *", "", IndentAction.None, "* ");
        TestIndentAction(support, "\t *", "\t *", "", IndentAction.None, "* ");

        TestIndentAction(support, "", "\t */", "", IndentAction.None, null, 1);
        TestIndentAction(support, "", "\t * */", "", IndentAction.None, null, 1);
        TestIndentAction(support, "", "\t * * / * / * / */", "", null, null);

        TestIndentAction(support, "\t/**", "\t * ", "", IndentAction.None, "* ");
        TestIndentAction(support, "\t * something", "\t * ", "", IndentAction.None, "* ");
        TestIndentAction(support, "\t *", "\t * ", "", IndentAction.None, "* ");

        TestIndentAction(support, "/**", " * ", "", IndentAction.None, "* ");
        TestIndentAction(support, " * something", " * ", "", IndentAction.None, "* ");
        TestIndentAction(support, " *", " * asdfsfagadfg", "", IndentAction.None, "* ");

        TestIndentAction(support, "/**", " * asdfsfagadfg * * * ", "", IndentAction.None, "* ");
        TestIndentAction(support, " * something", " * asdfsfagadfg * * * ", "", IndentAction.None, "* ");
        TestIndentAction(support, " *", " * asdfsfagadfg * * * ", "", IndentAction.None, "* ");

        TestIndentAction(support, "/**", " * /*", "", IndentAction.None, "* ");
        TestIndentAction(support, " * something", " * /*", "", IndentAction.None, "* ");
        TestIndentAction(support, " *", " * /*", "", IndentAction.None, "* ");

        TestIndentAction(support, "/**", " * asdfsfagadfg * / * / * /", "", IndentAction.None, "* ");
        TestIndentAction(support, " * something", " * asdfsfagadfg * / * / * /", "", IndentAction.None, "* ");
        TestIndentAction(support, " *", " * asdfsfagadfg * / * / * /", "", IndentAction.None, "* ");

        TestIndentAction(support, "/**", " * asdfsfagadfg * / * / * /*", "", IndentAction.None, "* ");
        TestIndentAction(support, " * something", " * asdfsfagadfg * / * / * /*", "", IndentAction.None, "* ");
        TestIndentAction(support, " *", " * asdfsfagadfg * / * / * /*", "", IndentAction.None, "* ");

        TestIndentAction(support, "", " */", "", IndentAction.None, null, 1);
        TestIndentAction(support, " */", " * test() {", "", IndentAction.Indent, null, 0);
        TestIndentAction(support, "", "\t */", "", IndentAction.None, null, 1);
        TestIndentAction(support, "", "\t\t */", "", IndentAction.None, null, 1);
        TestIndentAction(support, "", "   */", "", IndentAction.None, null, 1);
        TestIndentAction(support, "", "     */", "", IndentAction.None, null, 1);
        TestIndentAction(support, "", "\t     */", "", IndentAction.None, null, 1);
        TestIndentAction(support, "", " *--------------------------------------------------------------------------------------------*/", "", IndentAction.None, null, 1);

        // issue #43469
        TestIndentAction(support, "class A {", "    * test() {", "", IndentAction.Indent, null, 0);
        TestIndentAction(support, "", "    * test() {", "", IndentAction.Indent, null, 0);
        TestIndentAction(support, "    ", "    * test() {", "", IndentAction.Indent, null, 0);
        TestIndentAction(support, "class A {", "  * test() {", "", IndentAction.Indent, null, 0);
        TestIndentAction(support, "", "  * test() {", "", IndentAction.Indent, null, 0);
        TestIndentAction(support, "  ", "  * test() {", "", IndentAction.Indent, null, 0);
    }

    // Monaco: 'issue #141816'
    [Fact]
    public void Issue141816()
    {
        var support = new OnEnterSupport(null, OnEnterRules.JavascriptOnEnterRules);

        TestIndentAction(support, "const r = /{/;", "", IndentAction.None);
        TestIndentAction(support, "const r = /{[0-9]/;", "", IndentAction.None);
        TestIndentAction(support, "const r = /[a-zA-Z]{/;", "", IndentAction.None);
    }

    [Fact]
    public void OnEnter_BelowAdvanced_IgnoresRegExpRules()
    {
        var support = new OnEnterSupport(null, OnEnterRules.JavascriptOnEnterRules);

        Assert.Equal(new EnterAction(IndentAction.IndentOutdent, AppendText: " * "), support.OnEnter(EditorAutoIndentStrategy.Advanced, "", "/**", " */"));
        Assert.Null(support.OnEnter(EditorAutoIndentStrategy.Brackets, "", "/**", " */"));
        Assert.Equal(new EnterAction(IndentAction.IndentOutdent), support.OnEnter(EditorAutoIndentStrategy.Brackets, "", "{", "}"));
    }

    [Fact]
    public void OnEnter_None_ReturnsNull()
    {
        var support = new OnEnterSupport(null, OnEnterRules.JavascriptOnEnterRules);

        Assert.Null(support.OnEnter(EditorAutoIndentStrategy.None, "", "{", "}"));
        Assert.Null(support.OnEnter(EditorAutoIndentStrategy.Keep, "", "(", ""));
    }
}

// Ported from microsoft/vscode: src/vs/editor/contrib/find/test/browser/replacePattern.test.ts (suite 'Replace Pattern test')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Tests.Features.Find.TestHelpers;

namespace LoomEditor.View.Tests.Features.Find;

public class ReplacePatternTests
{
    private static readonly string Tab = ((char)9).ToString();
    private static readonly string Lf = ((char)10).ToString();

    private static ReplacePiece S(string value) => ReplacePiece.CreateStaticValue(value);

    private static ReplacePiece M(int index) => ReplacePiece.CreateMatchIndex(index);

    private static ReplacePiece C(int index, params char[] ops) => ReplacePiece.CreateCaseOps(index, ops);

    // Monaco compares the patterns with deepStrictEqual: compare their states.
    private static string Describe(ReplacePattern pattern) =>
        pattern.StaticValue is { } s
            ? "static:" + s
            : "pieces:" + string.Join("|", pattern.Pieces!.Select(p =>
                p.StaticValue is { } v ? "S(" + v + ")" : "M" + p.MatchIndex + "[" + string.Join("", p.CaseOps ?? []) + "]"));

    private static void TestParse(string input, params ReplacePiece[] expectedPieces) =>
        Assert.Equal(Describe(new ReplacePattern(expectedPieces)), Describe(ReplacePattern.Parse(input)));

    private static void AssertReplace(string target, string pattern, string replaceString, string expected, bool preserveCase = false)
    {
        var actual = ReplacePattern.Parse(replaceString).BuildReplaceString(RegexMatches.Exec(target, pattern), preserveCase);
        Assert.Equal(expected, actual);
    }

    // Monaco: 'parse replace string'
    [Fact]
    public void Parse_ReplaceString_BuildsPieces()
    {
        // no backslash => no treatment
        TestParse("hello", S("hello"));

        // \t => TAB
        TestParse(@"\thello", S(Tab + "hello"));
        TestParse(@"h\tello", S("h" + Tab + "ello"));
        TestParse(@"hello\t", S("hello" + Tab));

        // \n => LF
        TestParse(@"\nhello", S(Lf + "hello"));

        // \\t => \t
        TestParse(@"\\thello", S(@"\thello"));
        TestParse(@"h\\tello", S(@"h\tello"));
        TestParse(@"hello\\t", S(@"hello\t"));

        // \\\t => \TAB
        TestParse(@"\\\thello", S(@"\" + Tab + "hello"));

        // \\\\t => \\t
        TestParse(@"\\\\thello", S(@"\\thello"));

        // \ at the end => no treatment
        TestParse(@"hello\", S(@"hello\"));

        // \ with unknown char => no treatment
        TestParse(@"hello\x", S(@"hello\x"));

        // \ with back reference => no treatment
        TestParse(@"hello\0", S(@"hello\0"));

        TestParse("hello$&", S("hello"), M(0));
        TestParse("hello$0", S("hello"), M(0));
        TestParse("hello$02", S("hello"), M(0), S("2"));
        TestParse("hello$1", S("hello"), M(1));
        TestParse("hello$2", S("hello"), M(2));
        TestParse("hello$9", S("hello"), M(9));
        TestParse("$9hello", M(9), S("hello"));

        TestParse("hello$12", S("hello"), M(12));
        TestParse("hello$99", S("hello"), M(99));
        TestParse("hello$99a", S("hello"), M(99), S("a"));
        TestParse("hello$1a", S("hello"), M(1), S("a"));
        TestParse("hello$100", S("hello"), M(10), S("0"));
        TestParse("hello$100a", S("hello"), M(10), S("0a"));
        TestParse("hello$10a0", S("hello"), M(10), S("a0"));
        TestParse("hello$$", S("hello$"));
        TestParse("hello$$0", S("hello$0"));

        TestParse("hello$`", S("hello$`"));
        TestParse("hello$'", S("hello$'"));
    }

    // Monaco: 'parse replace string with case modifiers'
    [Fact]
    public void Parse_CaseModifiers_BuildsCaseOps()
    {
        // \U, \u => uppercase  \L, \l => lowercase  \E => cancel
        TestParse(@"hello\U$1", S("hello"), C(1, 'U'));
        AssertReplace("func privateFunc(", @"func (\w+)\(", @"func \U$1(", "func PRIVATEFUNC(");

        TestParse(@"hello\u$1", S("hello"), C(1, 'u'));
        AssertReplace("func privateFunc(", @"func (\w+)\(", @"func \u$1(", "func PrivateFunc(");

        TestParse(@"hello\L$1", S("hello"), C(1, 'L'));
        AssertReplace("func privateFunc(", @"func (\w+)\(", @"func \L$1(", "func privatefunc(");

        TestParse(@"hello\l$1", S("hello"), C(1, 'l'));
        AssertReplace("func PrivateFunc(", @"func (\w+)\(", @"func \l$1(", "func privateFunc(");

        TestParse(@"hello$1\u\u\U$4goodbye", S("hello"), M(1), C(4, 'u', 'u', 'U'), S("goodbye"));
        AssertReplace("hellogooDbye", @"hello(\w+)", @"hello\u\u\l\l\U$1", "helloGOodBYE");
    }

    // Monaco: 'replace has JavaScript semantics' (the JavaScript results are literals)
    [Fact]
    public void BuildReplaceString_JavaScriptSemantics_MatchesJavaScriptReplace()
    {
        AssertReplace("hi", "hi", "hello", "hello");
        AssertReplace("hi", "hi", @"\t", Tab);
        AssertReplace("hi", "hi", @"\n", Lf);
        AssertReplace("hi", "hi", @"\\t", @"\t");
        AssertReplace("hi", "hi", @"\\n", @"\n");

        // implicit capture group 0
        AssertReplace("hi", "hi", "hello$&", "hellohi");
        AssertReplace("hi", "hi", "hello$0", "hellohi");
        AssertReplace("hi", "hi", "hello$&1", "hellohi1");
        AssertReplace("hi", "hi", "hello$01", "hellohi1");

        // the replace string interprets $nn as a captured group only if it exists in the search regex
        AssertReplace("hi", "(hi)", "hello$10", "hellohi0");
        AssertReplace("hi", "(hi)()()()()()()()()()", "hello$10", "hello");
        AssertReplace("hi", "(hi)", "hello$100", "hellohi00");
        AssertReplace("hi", "(hi)", "hello$20", "hello$20");
    }

    // Monaco: 'get replace string if given text is a complete match'
    [Fact]
    public void BuildReplaceString_CompleteMatch_ReplacesGroups()
    {
        AssertReplace("bla", "bla", "hello", "hello");
        AssertReplace("bla", "(bla)", "hello", "hello");
        AssertReplace("bla", "(bla)", "hello$0", "hellobla");

        const string searchRegex = @"let\s+(\w+)\s*=\s*require\s*\(\s*['""]([\w\.\-/]+)\s*['""]\s*\)\s*";
        AssertReplace("let fs = require('fs')", searchRegex, "import * as $1 from '$2';", "import * as fs from 'fs';");
        AssertReplace("let something = require('fs')", searchRegex, "import * as $1 from '$2';", "import * as something from 'fs';");
        AssertReplace("let something = require('fs')", searchRegex, "import * as $1 from '$1';", "import * as something from 'something';");
        AssertReplace("let something = require('fs')", searchRegex, "import * as $2 from '$1';", "import * as fs from 'something';");
        AssertReplace("let something = require('fs')", searchRegex, "import * as $0 from '$0';", "import * as let something = require('fs') from 'let something = require('fs')';");
        AssertReplace("let fs = require('fs')", searchRegex, "import * as $1 from '$2';", "import * as fs from 'fs';");
        AssertReplace("for ()", "for(.*)", "cat$1", "cat ()");

        // issue #18111
        AssertReplace("HRESULT OnAmbientPropertyChange(DISPID   dispid);", @"\b\s{3}\b", " ", " ");
    }

    // Monaco: 'get replace string if match is sub-string of the text'
    [Fact]
    public void BuildReplaceString_SubStringMatch_ReplacesGroups()
    {
        const string text = "this is a bla text";
        AssertReplace(text, "bla", "hello", "hello");
        AssertReplace(text, "this(?=.*bla)", "that", "that");
        AssertReplace(text, "(th)is(?=.*bla)", "$1at", "that");
        AssertReplace(text, "(th)is(?=.*bla)", "$1e", "the");
        AssertReplace(text, "(th)is(?=.*bla)", "$1ere", "there");
        AssertReplace(text, "(th)is(?=.*bla)", "$1", "th");
        AssertReplace(text, "(th)is(?=.*bla)", "ma$1", "math");
        AssertReplace(text, "(th)is(?=.*bla)", "ma$1s", "maths");
        AssertReplace(text, "(th)is(?=.*bla)", "$0", "this");
        AssertReplace(text, "(th)is(?=.*bla)", "$0$1", "thisth");
        AssertReplace(text, @"bla(?=\stext$)", "foo", "foo");
        AssertReplace(text, @"b(la)(?=\stext$)", "f$1", "fla");
        AssertReplace(text, @"b(la)(?=\stext$)", "f$0", "fbla");
        AssertReplace(text, @"b(la)(?=\stext$)", "$0ah", "blaah");
    }

    // Monaco: 'issue #19740 Find and replace capture group/backreference inserts `undefined` instead of empty string'
    [Fact]
    public void BuildReplaceString_Issue19740_UnmatchedGroupIsEmpty() =>
        AssertReplace("abcd", "a(z)?", "a{$1}", "a{}");

    private static readonly (string[] Target, string Pattern, string Expected)[] CasePreservedCases =
    [
        (["abc"], "Def", "def"),
        (["Abc"], "Def", "Def"),
        (["ABC"], "Def", "DEF"),
        (["abc", "Abc"], "Def", "def"),
        (["Abc", "abc"], "Def", "Def"),
        (["ABC", "abc"], "Def", "DEF"),
        (["aBc", "abc"], "Def", "def"),
        (["AbC"], "Def", "Def"),
        (["aBC"], "Def", "def"),
        (["aBc"], "DeF", "deF"),
        (["Foo-Bar"], "newfoo-newbar", "Newfoo-Newbar"),
        (["Foo-Bar-Abc"], "newfoo-newbar-newabc", "Newfoo-Newbar-Newabc"),
        (["Foo-Bar-abc"], "newfoo-newbar", "Newfoo-newbar"),
        (["foo-Bar"], "newfoo-newbar", "newfoo-Newbar"),
        (["foo-BAR"], "newfoo-newbar", "newfoo-NEWBAR"),
        (["foO-BAR"], "NewFoo-NewBar", "newFoo-NEWBAR"),
        (["Foo_Bar"], "newfoo_newbar", "Newfoo_Newbar"),
        (["Foo_Bar_Abc"], "newfoo_newbar_newabc", "Newfoo_Newbar_Newabc"),
        (["Foo_Bar_abc"], "newfoo_newbar", "Newfoo_newbar"),
        (["Foo_Bar-abc"], "newfoo_newbar-abc", "Newfoo_newbar-abc"),
        (["foo_Bar"], "newfoo_newbar", "newfoo_Newbar"),
    ];

    // Monaco: 'buildReplaceStringWithCasePreserved test'
    [Fact]
    public void BuildReplaceStringWithCasePreserved_Cases_PreservesCase()
    {
        foreach (var (target, pattern, expected) in CasePreservedCases)
        {
            Assert.Equal(expected, SearchStrings.BuildReplaceStringWithCasePreserved(target, pattern));
        }

        Assert.Equal("Newfoo_NEWBAR", SearchStrings.BuildReplaceStringWithCasePreserved(["Foo_BAR"], "newfoo_newbar"));
    }

    // Monaco: 'preserve case'
    [Fact]
    public void BuildReplaceString_PreserveCase_PreservesCase()
    {
        foreach (var (target, pattern, expected) in CasePreservedCases)
        {
            Assert.Equal(expected, ReplacePattern.Parse(pattern).BuildReplaceString(target, true));
        }

        Assert.Equal("newfoo_NEWBAR", ReplacePattern.Parse("newfoo_newbar").BuildReplaceString(["foo_BAR"], true));
    }
}

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Guides;
using LoomEditor.Core.Tests.Model.BracketPairs.TestHelpers;

namespace LoomEditor.Core.Tests.Model.Guides;

// Monaco: suite 'TextModel.getLineIndentGuide' (textModelWithTokens.test.ts)
public class TextModelGetLineIndentGuideTests
{
    private static TextModel CreateModel(string text) =>
        TestLanguageModels.Create(text, "testLang", new LanguageConfiguration()).Model;

    // Monaco: assertIndentGuides
    private static void AssertIndentGuides((int Indent, int Start, int End, int Level, string Text)[] lines, int indentSize)
    {
        var model = CreateModel(string.Join('\n', lines.Select(l => l.Text)));
        model.UpdateOptions(new TextModelUpdateOptions(IndentSize: indentSize));

        int[] actualIndents = model.Guides.GetLinesIndentGuides(1, model.LineCount);

        var actual = new (int, int, int, int, string)[model.LineCount];
        for (int line = 1; line <= model.LineCount; line++)
        {
            var active = model.Guides.GetActiveIndentGuide(line, 1, model.LineCount);
            actual[line - 1] = (actualIndents[line - 1], active.StartLineNumber, active.EndLineNumber, active.Indent, model.GetLineContent(line));
        }

        Assert.Equal(lines, actual);
    }

    // Monaco: 'getLineIndentGuide one level 2'
    [Fact]
    public void GetLineIndentGuide_OneLevel2()
    {
        AssertIndentGuides(
            [
                (0, 2, 4, 1, "A"),
                (1, 2, 4, 1, "  A"),
                (1, 2, 4, 1, "  A"),
                (1, 2, 4, 1, "  A"),
            ],
            2);
    }

    // Monaco: 'getLineIndentGuide two levels'
    [Fact]
    public void GetLineIndentGuide_TwoLevels()
    {
        AssertIndentGuides(
            [
                (0, 2, 5, 1, "A"),
                (1, 2, 5, 1, "  A"),
                (1, 4, 5, 2, "  A"),
                (2, 4, 5, 2, "    A"),
                (2, 4, 5, 2, "    A"),
            ],
            2);
    }

    // Monaco: 'getLineIndentGuide three levels'
    [Fact]
    public void GetLineIndentGuide_ThreeLevels()
    {
        AssertIndentGuides(
            [
                (0, 2, 4, 1, "A"),
                (1, 3, 4, 2, "  A"),
                (2, 4, 4, 3, "    A"),
                (3, 4, 4, 3, "      A"),
                (0, 5, 5, 0, "A"),
            ],
            2);
    }

    // Monaco: 'getLineIndentGuide decreasing indent'
    [Fact]
    public void GetLineIndentGuide_DecreasingIndent()
    {
        AssertIndentGuides(
            [
                (2, 1, 1, 2, "    A"),
                (1, 1, 1, 2, "  A"),
                (0, 1, 2, 1, "A"),
            ],
            2);
    }

    // Monaco: 'getLineIndentGuide Java'
    [Fact]
    public void GetLineIndentGuide_Java()
    {
        AssertIndentGuides(
            [
                (0, 2, 9, 1, "class A {"),
                (1, 3, 4, 2, "  void foo() {"),
                (2, 3, 4, 2, "    console.log(1);"),
                (2, 3, 4, 2, "    console.log(2);"),
                (1, 3, 4, 2, "  }"),
                (1, 2, 9, 1, ""),
                (1, 8, 8, 2, "  void bar() {"),
                (2, 8, 8, 2, "    console.log(3);"),
                (1, 8, 8, 2, "  }"),
                (0, 2, 9, 1, "}"),
                (0, 12, 12, 1, "interface B {"),
                (1, 12, 12, 1, "  void bar();"),
                (0, 12, 12, 1, "}"),
            ],
            2);
    }

    // Monaco: 'getLineIndentGuide Javadoc'
    [Fact]
    public void GetLineIndentGuide_Javadoc()
    {
        AssertIndentGuides(
            [
                (0, 2, 3, 1, "/**"),
                (1, 2, 3, 1, " * Comment"),
                (1, 2, 3, 1, " */"),
                (0, 5, 6, 1, "class A {"),
                (1, 5, 6, 1, "  void foo() {"),
                (1, 5, 6, 1, "  }"),
                (0, 5, 6, 1, "}"),
            ],
            2);
    }

    // Monaco: 'getLineIndentGuide Whitespace'
    [Fact]
    public void GetLineIndentGuide_Whitespace()
    {
        AssertIndentGuides(
            [
                (0, 2, 7, 1, "class A {"),
                (1, 2, 7, 1, ""),
                (1, 4, 5, 2, "  void foo() {"),
                (2, 4, 5, 2, "    "),
                (2, 4, 5, 2, "    return 1;"),
                (1, 4, 5, 2, "  }"),
                (1, 2, 7, 1, "      "),
                (0, 2, 7, 1, "}"),
            ],
            2);
    }

    // Monaco: 'getLineIndentGuide Tabs'
    [Fact]
    public void GetLineIndentGuide_Tabs()
    {
        AssertIndentGuides(
            [
                (0, 2, 7, 1, "class A {"),
                (1, 2, 7, 1, "\t\t"),
                (1, 4, 5, 2, "\tvoid foo() {"),
                (2, 4, 5, 2, "\t \t//hello"),
                (2, 4, 5, 2, "\t    return 2;"),
                (1, 4, 5, 2, "  \t}"),
                (1, 2, 7, 1, "      "),
                (0, 2, 7, 1, "}"),
            ],
            4);
    }

    // Monaco: 'getLineIndentGuide checker.ts'
    [Fact]
    public void GetLineIndentGuide_CheckerTs()
    {
        AssertIndentGuides(
            [
                (0, 1, 1, 0, "/// <reference path=\"binder.ts\"/>"),
                (0, 2, 2, 0, ""),
                (0, 3, 3, 0, "/* @internal */"),
                (0, 5, 16, 1, "namespace ts {"),
                (1, 5, 16, 1, "    let nextSymbolId = 1;"),
                (1, 5, 16, 1, "    let nextNodeId = 1;"),
                (1, 5, 16, 1, "    let nextMergeId = 1;"),
                (1, 5, 16, 1, "    let nextFlowId = 1;"),
                (1, 5, 16, 1, ""),
                (1, 11, 15, 2, "    export function getNodeId(node: Node): number {"),
                (2, 12, 13, 3, "        if (!node.id) {"),
                (3, 12, 13, 3, "            node.id = nextNodeId;"),
                (3, 12, 13, 3, "            nextNodeId++;"),
                (2, 12, 13, 3, "        }"),
                (2, 11, 15, 2, "        return node.id;"),
                (1, 11, 15, 2, "    }"),
                (0, 5, 16, 1, "}"),
            ],
            4);
    }

    // Monaco: 'issue #8425 - Missing indentation lines for first level indentation'
    [Fact]
    public void Issue8425_MissingIndentationLinesForFirstLevelIndentation()
    {
        AssertIndentGuides(
            [
                (1, 2, 3, 2, "\tindent1"),
                (2, 2, 3, 2, "\t\tindent2"),
                (2, 2, 3, 2, "\t\tindent2"),
                (1, 2, 3, 2, "\tindent1"),
            ],
            4);
    }

    // Monaco: 'issue #8952 - Indentation guide lines going through text on .yml file'
    [Fact]
    public void Issue8952_IndentationGuideLinesGoingThroughTextOnYmlFile()
    {
        AssertIndentGuides(
            [
                (0, 2, 5, 1, "properties:"),
                (1, 3, 5, 2, "    emailAddress:"),
                (2, 3, 5, 2, "        - bla"),
                (2, 5, 5, 3, "        - length:"),
                (3, 5, 5, 3, "            max: 255"),
                (0, 6, 6, 0, "getters:"),
            ],
            4);
    }

    // Monaco: 'issue #11892 - Indent guides look funny'
    [Fact]
    public void Issue11892_IndentGuidesLookFunny()
    {
        AssertIndentGuides(
            [
                (0, 2, 7, 1, "function test(base) {"),
                (1, 3, 6, 2, "\tswitch (base) {"),
                (2, 4, 4, 3, "\t\tcase 1:"),
                (3, 4, 4, 3, "\t\t\treturn 1;"),
                (2, 6, 6, 3, "\t\tcase 2:"),
                (3, 6, 6, 3, "\t\t\treturn 2;"),
                (1, 2, 7, 1, "\t}"),
                (0, 2, 7, 1, "}"),
            ],
            4);
    }

    // Monaco: 'issue #12398 - Problem in indent guidelines'
    [Fact]
    public void Issue12398_ProblemInIndentGuidelines()
    {
        AssertIndentGuides(
            [
                (2, 2, 2, 3, "\t\t.bla"),
                (3, 2, 2, 3, "\t\t\tlabel(for)"),
                (0, 3, 3, 0, "include script"),
            ],
            4);
    }

    // Monaco: 'issue #49173'
    [Fact]
    public void Issue49173()
    {
        var model = CreateModel(string.Join(
            '\n',
            "class A {",
            "\tpublic m1(): void {",
            "\t}",
            "\tpublic m2(): void {",
            "\t}",
            "\tpublic m3(): void {",
            "\t}",
            "\tpublic m4(): void {",
            "\t}",
            "\tpublic m5(): void {",
            "\t}",
            "}"));

        var actual = model.Guides.GetActiveIndentGuide(2, 4, 9);
        Assert.Equal(new ActiveIndentGuideInfo(2, 9, 1), actual);
    }

    // Monaco: 'tweaks - no active'
    [Fact]
    public void Tweaks_NoActive()
    {
        AssertIndentGuides(
            [
                (0, 1, 1, 0, "A"),
                (0, 2, 2, 0, "A"),
            ],
            2);
    }

    // Monaco: 'tweaks - inside scope'
    [Fact]
    public void Tweaks_InsideScope()
    {
        AssertIndentGuides(
            [
                (0, 2, 2, 1, "A"),
                (1, 2, 2, 1, "  A"),
            ],
            2);
    }

    // Monaco: 'tweaks - scope start'
    [Fact]
    public void Tweaks_ScopeStart()
    {
        AssertIndentGuides(
            [
                (0, 2, 2, 1, "A"),
                (1, 2, 2, 1, "  A"),
                (0, 2, 2, 1, "A"),
            ],
            2);
    }

    // Monaco: 'tweaks - empty line'
    [Fact]
    public void Tweaks_EmptyLine()
    {
        AssertIndentGuides(
            [
                (0, 2, 4, 1, "A"),
                (1, 2, 4, 1, "  A"),
                (1, 2, 4, 1, ""),
                (1, 2, 4, 1, "  A"),
                (0, 2, 4, 1, "A"),
            ],
            2);
    }

    [Fact]
    public void GetActiveIndentGuide_InvalidLine_Throws()
    {
        var model = CreateModel("a\nb");
        Assert.Throws<ArgumentOutOfRangeException>(() => model.Guides.GetActiveIndentGuide(3, 1, 2));
    }
}

// Ported from microsoft/vscode: src/vs/editor/contrib/indentation/test/browser/indentation.test.ts (suites 'Change Indentation to Spaces - TypeScript/Javascript' and 'Change Indentation to Tabs -  TypeScript/Javascript')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Features.Indentation;
using LoomEditor.View.Tests.Cursors.TestHelpers;

namespace LoomEditor.View.Tests.Features.Indentation;

public class IndentationCommandTests
{
    private static void TestIndentationToSpaces(string[] lines, Selection selection, int tabSize, string[] expectedLines, Selection expectedSelection) =>
        CommandTester.TestCommand(lines, selection, sel => new IndentationToSpacesCommand(sel, tabSize), expectedLines, expectedSelection);

    private static void TestIndentationToTabs(string[] lines, Selection selection, int tabSize, string[] expectedLines, Selection expectedSelection) =>
        CommandTester.TestCommand(lines, selection, sel => new IndentationToTabsCommand(sel, tabSize), expectedLines, expectedSelection);

    // Monaco: 'Change Indentation to Spaces' › 'single tabs only at start of line'
    [Fact]
    public void ToSpaces_SingleTabsOnlyAtStartOfLine() =>
        TestIndentationToSpaces(
            ["first", "second line", "third line", "\tfourth line", "\tfifth"],
            new Selection(2, 3, 2, 3),
            4,
            ["first", "second line", "third line", "    fourth line", "    fifth"],
            new Selection(2, 3, 2, 3));

    // Monaco: 'Change Indentation to Spaces' › 'multiple tabs at start of line'
    [Fact]
    public void ToSpaces_MultipleTabsAtStartOfLine() =>
        TestIndentationToSpaces(
            ["\t\tfirst", "\tsecond line", "\t\t\t third line", "fourth line", "fifth"],
            new Selection(1, 5, 1, 5),
            3,
            ["      first", "   second line", "          third line", "fourth line", "fifth"],
            new Selection(1, 9, 1, 9));

    // Monaco: 'Change Indentation to Spaces' › 'multiple tabs'
    [Fact]
    public void ToSpaces_MultipleTabs() =>
        TestIndentationToSpaces(
            ["\t\tfirst\t", "\tsecond  \t line \t", "\t\t\t third line", " \tfourth line", "fifth"],
            new Selection(1, 5, 1, 5),
            2,
            ["    first\t", "  second  \t line \t", "       third line", "   fourth line", "fifth"],
            new Selection(1, 7, 1, 7));

    // Monaco: 'Change Indentation to Spaces' › 'empty lines'
    [Fact]
    public void ToSpaces_EmptyLines() =>
        TestIndentationToSpaces(
            ["\t\t\t", "\t", "\t\t"],
            new Selection(1, 4, 1, 4),
            2,
            ["      ", "  ", "    "],
            new Selection(1, 4, 1, 4));

    // Monaco: 'Change Indentation to Tabs' › 'spaces only at start of line'
    [Fact]
    public void ToTabs_SpacesOnlyAtStartOfLine() =>
        TestIndentationToTabs(
            ["    first", "second line", "    third line", "fourth line", "fifth"],
            new Selection(2, 3, 2, 3),
            4,
            ["\tfirst", "second line", "\tthird line", "fourth line", "fifth"],
            new Selection(2, 3, 2, 3));

    // Monaco: 'Change Indentation to Tabs' › 'multiple spaces at start of line'
    [Fact]
    public void ToTabs_MultipleSpacesAtStartOfLine() =>
        TestIndentationToTabs(
            ["first", "   second line", "          third line", "fourth line", "     fifth"],
            new Selection(1, 5, 1, 5),
            3,
            ["first", "\tsecond line", "\t\t\t third line", "fourth line", "\t  fifth"],
            new Selection(1, 5, 1, 5));

    // Monaco: 'Change Indentation to Tabs' › 'multiple spaces'
    [Fact]
    public void ToTabs_MultipleSpaces() =>
        TestIndentationToTabs(
            ["      first   ", "  second     line \t", "       third line", "   fourth line", "fifth"],
            new Selection(1, 8, 1, 8),
            2,
            ["\t\t\tfirst   ", "\tsecond     line \t", "\t\t\t third line", "\t fourth line", "fifth"],
            new Selection(1, 5, 1, 5));

    // Monaco: 'Change Indentation to Tabs' › 'issue #45996' (it runs the to-spaces command, as Monaco's test does)
    [Fact]
    public void Issue45996() =>
        TestIndentationToSpaces(["\tabc"], new Selection(1, 3, 1, 3), 4, ["    abc"], new Selection(1, 6, 1, 6));
}

// Ported from microsoft/vscode: src/vs/editor/contrib/comment/test/browser/blockCommentCommand.test.ts (suite 'Editor Contrib - Block Comment Command')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Text;
using LoomEditor.View.Features.Comment;
using LoomEditor.View.Tests.Cursors.TestHelpers;

namespace LoomEditor.View.Tests.Features.Comment;

public class BlockCommentCommandTests
{
    private const string CommentModeId = "commentMode";

    // Monaco: _testCommentCommand + testBlockCommentCommand
    private static void Test(string[] lines, Selection selection, string[] expectedLines, Selection expectedSelection, bool insertSpace = true) =>
        CommandTester.TestCommand(
            lines,
            selection,
            (services, sel) => new BlockCommentCommand(sel, insertSpace, services.LanguageConfigurationRegistry),
            expectedLines,
            expectedSelection,
            languageId: CommentModeId,
            prepare: services =>
            {
                services.RegisterLanguage(CommentModeId);
                services.Add(services.LanguageConfigurationRegistry.Register(CommentModeId, new LanguageConfiguration
                {
                    Comments = new CommentRule(new LineCommentConfig("!@#"), new CharacterPair("<0", "0>")),
                }));
            });

    // The two first lines vary in the tests; the three last ones never do.
    private static string[] Doc(params string[] head) => [.. head, "third line", "fourth line", "fifth"];

    private static string[] Doc(string first) => Doc(first, "\tsecond line");

    // Monaco: 'empty selection wraps itself'
    [Fact]
    public void EmptySelectionWrapsItself() =>
        Test(Doc("first"), new Selection(1, 3, 1, 3), Doc("fi<0  0>rst"), new Selection(1, 6, 1, 6));

    // Monaco: 'invisible selection ignored'
    [Fact]
    public void InvisibleSelectionIgnored() =>
        Test(Doc("first"), new Selection(2, 1, 1, 1), Doc("<0 first", " 0>\tsecond line"), new Selection(1, 4, 2, 1));

    // Monaco: 'bug9511'
    [Fact]
    public void Bug9511()
    {
        Test(Doc("first"), new Selection(1, 6, 1, 1), Doc("<0 first 0>"), new Selection(1, 4, 1, 9));
        Test(Doc("<0first0>"), new Selection(1, 8, 1, 3), Doc("first"), new Selection(1, 1, 1, 6));
    }

    // Monaco: 'one line selection'
    [Fact]
    public void OneLineSelection() =>
        Test(Doc("first"), new Selection(1, 6, 1, 3), Doc("fi<0 rst 0>"), new Selection(1, 6, 1, 9));

    // Monaco: 'one line selection toggle'
    [Fact]
    public void OneLineSelectionToggle()
    {
        Test(Doc("first"), new Selection(1, 6, 1, 3), Doc("fi<0 rst 0>"), new Selection(1, 6, 1, 9));
        Test(Doc("fi<0rst0>"), new Selection(1, 8, 1, 5), Doc("first"), new Selection(1, 3, 1, 6));
        Test(Doc("<0 first 0>"), new Selection(1, 10, 1, 1), Doc("first"), new Selection(1, 1, 1, 6));
        Test(Doc("<0 first0>"), new Selection(1, 9, 1, 1), Doc("first"), new Selection(1, 1, 1, 6));
        Test(Doc("<0first 0>"), new Selection(1, 9, 1, 1), Doc("first"), new Selection(1, 1, 1, 6));
        Test(Doc("fi<0rst0>"), new Selection(1, 8, 1, 5), Doc("first"), new Selection(1, 3, 1, 6));
    }

    // Monaco: 'multi line selection'
    [Fact]
    public void MultiLineSelection() =>
        Test(Doc("first"), new Selection(2, 4, 1, 1), Doc("<0 first", "\tse 0>cond line"), new Selection(1, 4, 2, 4));

    // Monaco: 'multi line selection toggle'
    [Fact]
    public void MultiLineSelectionToggle()
    {
        Test(Doc("first"), new Selection(2, 4, 1, 1), Doc("<0 first", "\tse 0>cond line"), new Selection(1, 4, 2, 4));
        Test(Doc("<0first", "\tse0>cond line"), new Selection(2, 4, 1, 3), Doc("first"), new Selection(1, 1, 2, 4));
        Test(Doc("<0 first", "\tse0>cond line"), new Selection(2, 4, 1, 3), Doc("first"), new Selection(1, 1, 2, 4));
        Test(Doc("<0first", "\tse 0>cond line"), new Selection(2, 4, 1, 3), Doc("first"), new Selection(1, 1, 2, 4));
        Test(Doc("<0 first", "\tse 0>cond line"), new Selection(2, 4, 1, 3), Doc("first"), new Selection(1, 1, 2, 4));
    }

    // Monaco: 'fuzzy removes'
    [Fact]
    public void FuzzyRemoves()
    {
        string[] input = ["asd <0 qwe", "asd 0> qwe"];
        string[] expected = ["asd qwe", "asd qwe"];
        Selection[] selections =
        [
            new(2, 5, 1, 7),
            new(2, 5, 1, 6),
            new(2, 5, 1, 5),
            new(2, 5, 1, 11),
            new(2, 1, 1, 11),
            new(2, 7, 1, 11),
        ];
        foreach (Selection selection in selections)
        {
            Test(input, selection, expected, new Selection(1, 5, 2, 4));
        }
    }

    // Monaco: 'bug #30358'
    [Fact]
    public void Bug30358()
    {
        Test(["<0 start 0> middle end"], new Selection(1, 20, 1, 23), ["<0 start 0> middle <0 end 0>"], new Selection(1, 23, 1, 26));
        Test(["<0 start 0> middle <0 end 0>"], new Selection(1, 13, 1, 19), ["<0 start 0> <0 middle 0> <0 end 0>"], new Selection(1, 16, 1, 22));
    }

    // Monaco: 'issue #34618'
    [Fact]
    public void Issue34618() =>
        Test(["<0  0> middle end"], new Selection(1, 4, 1, 4), [" middle end"], new Selection(1, 1, 1, 1));

    // Monaco: 'insertSpace false'
    [Fact]
    public void InsertSpaceFalse() =>
        Test(["some text"], new Selection(1, 1, 1, 5), ["<0some0> text"], new Selection(1, 3, 1, 7), insertSpace: false);

    // Monaco: 'insertSpace false does not remove space'
    [Fact]
    public void InsertSpaceFalseDoesNotRemoveSpace() =>
        Test(["<0 some 0> text"], new Selection(1, 4, 1, 8), [" some  text"], new Selection(1, 1, 1, 7), insertSpace: false);
}

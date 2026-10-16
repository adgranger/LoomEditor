// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Commands;

public class MouseCommandTests
{
    private static void WordSelect(ViewModel viewModel, int lineNumber, int column) =>
        CoreNavigationCommands.WordSelect.RunCoreEditorCommand(viewModel, new MoveCommandArgs(new Position(lineNumber, column)));

    private static void WordSelectDrag(ViewModel viewModel, int lineNumber, int column) =>
        CoreNavigationCommands.WordSelectDrag.RunCoreEditorCommand(viewModel, new MoveCommandArgs(new Position(lineNumber, column)));

    // Monaco: 'issue #33788: Wrong cursor position when double click to select a word'
    [Fact]
    public void WordSelect_DoubleClickOnAWord_SelectsTheWordAndKeepsItWhenDragging()
    {
        TestCodeEditor.WithTestCodeEditor("Just some text", editor =>
        {
            WordSelect(editor.ViewModel, 1, 8);
            Assert.Equal(new Selection(1, 6, 1, 10), editor.ViewModel.GetSelection());

            WordSelectDrag(editor.ViewModel, 1, 8);
            Assert.Equal(new Selection(1, 6, 1, 10), editor.ViewModel.GetSelection());
        });
    }

    // Monaco: 'issue #12887: Double-click highlighting separating white space'
    [Fact]
    public void WordSelect_DoubleClickOnAWordAfterASpace_DoesNotSelectTheSpace()
    {
        TestCodeEditor.WithTestCodeEditor("abc def", editor =>
        {
            WordSelect(editor.ViewModel, 1, 5);
            Assert.Equal(new Selection(1, 5, 1, 8), editor.ViewModel.GetSelection());
        });
    }

    // Monaco: 'Double-click on punctuation should select the character, not adjacent space'
    [Fact]
    public void WordSelect_DoubleClickOnPunctuation_SelectsTheCharacterNotTheAdjacentSpace()
    {
        TestCodeEditor.WithTestCodeEditor("// a b c 1 2 3 ~ ! @ # $ % ^ & * ( ) _ + \\ /", editor =>
        {
            // Test double-click on '@' at position 20
            WordSelect(editor.ViewModel, 1, 20);
            Assert.Equal(new Selection(1, 20, 1, 21), editor.ViewModel.GetSelection());

            // Test double-click on '#' at position 22
            WordSelect(editor.ViewModel, 1, 22);
            Assert.Equal(new Selection(1, 22, 1, 23), editor.ViewModel.GetSelection());

            // Test double-click on '!' at position 18
            WordSelect(editor.ViewModel, 1, 18);
            Assert.Equal(new Selection(1, 18, 1, 19), editor.ViewModel.GetSelection());

            // Test double-click on first '/' in '//' at position 1
            WordSelect(editor.ViewModel, 1, 1);
            Assert.Equal(new Selection(1, 1, 1, 3), editor.ViewModel.GetSelection());

            // Test double-click on second '/' in '//' at position 2
            WordSelect(editor.ViewModel, 1, 2);
            Assert.Equal(new Selection(1, 1, 1, 3), editor.ViewModel.GetSelection());

            // Test double-click on '\' at position 42
            WordSelect(editor.ViewModel, 1, 42);
            Assert.Equal(new Selection(1, 42, 1, 43), editor.ViewModel.GetSelection());
        });
    }

    // Monaco: 'issue #7100: Mouse word selection is strange when non-word character is at the end of line'
    [Fact]
    public void WordSelect_NonWordCharacterAtTheEndOfTheLine_SelectsLikeMonaco()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Join('\n', "before.a", "before", "hello:", "there:", "this is strange:", "here", "it", "is"),
            editor =>
            {
                WordSelect(editor.ViewModel, 3, 7);
                AssertCursor(editor.ViewModel, new Selection(3, 7, 3, 7));

                WordSelectDrag(editor.ViewModel, 4, 7);
                AssertCursor(editor.ViewModel, new Selection(3, 7, 4, 7));
            });
    }

    // Monaco: 'issue #112039: shift-continuing a double/triple-click and drag selection does not remember its starting mode'
    [Fact]
    public void MoveToSelect_AfterAWordSelect_RemembersTheWordStartingMode()
    {
        TestCodeEditor.WithTestCodeEditor(string.Join('\n', "just some text", "and another line", "and another one"), editor =>
        {
            WordSelect(editor.ViewModel, 2, 6);
            CoreNavigationCommands.MoveToSelect.RunCoreEditorCommand(editor.ViewModel, new MoveCommandArgs(new Position(1, 8)));
            AssertCursor(editor.ViewModel, new Selection(2, 12, 1, 6));
        });
    }

    // Monaco: 'issue #158236: Shift click selection does not work on line number indicator'
    [Fact]
    public void LineSelectDrag_AfterAMove_ExtendsFromTheCursor()
    {
        TestCodeEditor.WithTestCodeEditor(string.Join('\n', "just some text", "and another line", "and another one"), editor =>
        {
            CoreNavigationCommands.MoveTo.RunCoreEditorCommand(editor.ViewModel, new MoveCommandArgs(new Position(3, 5)));
            CoreNavigationCommands.LineSelectDrag.RunCoreEditorCommand(editor.ViewModel, new MoveCommandArgs(new Position(2, 1)));
            AssertCursor(editor.ViewModel, new Selection(3, 5, 2, 1));
        });
    }
}

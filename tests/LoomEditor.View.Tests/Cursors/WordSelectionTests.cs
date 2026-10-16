// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller', word selection)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

public class WordSelectionTests
{
    // Monaco runs CoreNavigationCommands.WordSelect / WordSelectDrag (the mouse commands, P3-44), which set the cursor
    // states from CursorMoveCommands.word: the tests do the same.
    private static void WordSelect(ViewModel vm, int lineNumber, int column, bool inSelectionMode)
    {
        vm.Model.PushStackElement();
        vm.SetCursorStates(
            null,
            CursorChangeReason.Explicit,
            [CursorMoveCommands.Word(vm, vm.GetPrimaryCursorState(), inSelectionMode, new Position(lineNumber, column))]);
    }

    private static void AssertWordRight(ViewModel vm, int col, string expectedPrefix)
    {
        WordSelect(vm, 1, col, inSelectionMode: col != 1);

        Selection selection = vm.GetSelection();
        Assert.True(selection.Range.StartColumn == 1, $"TEST FOR {col}");
        Assert.True(selection.Range.EndColumn == expectedPrefix.Length + 1, $"TEST FOR {col}: {selection.Range.EndColumn} != {expectedPrefix.Length + 1}");
    }

    // Monaco: 'issue #832: word right'
    [Fact]
    public void Issue832_WordRight() =>
        TestCodeEditor.WithTestCodeEditor("   /* Just some   more   text a+= 3 +5-3 + 7 */  ", editor =>
        {
            ViewModel vm = editor.ViewModel;
            MoveTo(vm, 1, 1, false);

            AssertWordRight(vm, 1, "   ");
            AssertWordRight(vm, 2, "   ");
            AssertWordRight(vm, 3, "   ");
            AssertWordRight(vm, 4, "   ");
            AssertWordRight(vm, 5, "   /");
            AssertWordRight(vm, 6, "   /*");
            AssertWordRight(vm, 7, "   /* ");
            AssertWordRight(vm, 8, "   /* Just");
            AssertWordRight(vm, 9, "   /* Just");
            AssertWordRight(vm, 10, "   /* Just");
            AssertWordRight(vm, 11, "   /* Just");
            AssertWordRight(vm, 12, "   /* Just ");
            AssertWordRight(vm, 13, "   /* Just some");
            AssertWordRight(vm, 14, "   /* Just some");
            AssertWordRight(vm, 15, "   /* Just some");
            AssertWordRight(vm, 16, "   /* Just some");
            AssertWordRight(vm, 17, "   /* Just some ");
            AssertWordRight(vm, 18, "   /* Just some  ");
            AssertWordRight(vm, 19, "   /* Just some   ");
            AssertWordRight(vm, 20, "   /* Just some   more");
            AssertWordRight(vm, 21, "   /* Just some   more");
            AssertWordRight(vm, 22, "   /* Just some   more");
            AssertWordRight(vm, 23, "   /* Just some   more");
            AssertWordRight(vm, 24, "   /* Just some   more ");
            AssertWordRight(vm, 25, "   /* Just some   more  ");
            AssertWordRight(vm, 26, "   /* Just some   more   ");
            AssertWordRight(vm, 27, "   /* Just some   more   text");
            AssertWordRight(vm, 28, "   /* Just some   more   text");
            AssertWordRight(vm, 29, "   /* Just some   more   text");
            AssertWordRight(vm, 30, "   /* Just some   more   text");
            AssertWordRight(vm, 31, "   /* Just some   more   text ");
            AssertWordRight(vm, 32, "   /* Just some   more   text a");
            AssertWordRight(vm, 33, "   /* Just some   more   text a+");
            AssertWordRight(vm, 34, "   /* Just some   more   text a+=");
            AssertWordRight(vm, 35, "   /* Just some   more   text a+= ");
            AssertWordRight(vm, 36, "   /* Just some   more   text a+= 3");
            AssertWordRight(vm, 37, "   /* Just some   more   text a+= 3 ");
            AssertWordRight(vm, 38, "   /* Just some   more   text a+= 3 +");
            AssertWordRight(vm, 39, "   /* Just some   more   text a+= 3 +5");
            AssertWordRight(vm, 40, "   /* Just some   more   text a+= 3 +5-");
            AssertWordRight(vm, 41, "   /* Just some   more   text a+= 3 +5-3");
            AssertWordRight(vm, 42, "   /* Just some   more   text a+= 3 +5-3 ");
            AssertWordRight(vm, 43, "   /* Just some   more   text a+= 3 +5-3 +");
            AssertWordRight(vm, 44, "   /* Just some   more   text a+= 3 +5-3 + ");
            AssertWordRight(vm, 45, "   /* Just some   more   text a+= 3 +5-3 + 7");
            AssertWordRight(vm, 46, "   /* Just some   more   text a+= 3 +5-3 + 7 ");
            AssertWordRight(vm, 47, "   /* Just some   more   text a+= 3 +5-3 + 7 *");
            AssertWordRight(vm, 48, "   /* Just some   more   text a+= 3 +5-3 + 7 */");
            AssertWordRight(vm, 49, "   /* Just some   more   text a+= 3 +5-3 + 7 */ ");
            AssertWordRight(vm, 50, "   /* Just some   more   text a+= 3 +5-3 + 7 */  ");
        });

    [Fact]
    public void Word_OnHelloWorld_SelectsTheWordUnderTheCursor() =>
        TestCodeEditor.WithTestCodeEditor("hello world", editor =>
        {
            ViewModel vm = editor.ViewModel;

            PartialCursorState result = CursorMoveCommands.Word(vm, vm.GetPrimaryCursorState(), false, new Position(1, 8));

            Assert.Equal(new Selection(1, 7, 1, 12), result.ModelState!.Selection);
            Assert.Equal(SelectionStartKind.Word, result.ModelState.SelectionStartKind);
        });
}

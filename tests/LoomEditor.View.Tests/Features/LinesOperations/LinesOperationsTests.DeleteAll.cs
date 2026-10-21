using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Features.LinesOperations;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.LinesOperations;

// Monaco: suites 'DeleteAllLeftAction' and 'DeleteAllRightAction'
public partial class LinesOperationsTests
{
    private static void DeleteAllLeft(TestCodeEditor editor) =>
        LinesOperationsActions.DeleteAllToBoundary(editor.ViewModel, LinesOperationsCommands.DeleteAllLeft, false);

    private static void DeleteAllRight(TestCodeEditor editor) =>
        LinesOperationsActions.DeleteAllToBoundary(editor.ViewModel, LinesOperationsCommands.DeleteAllRight, true);

    private static void SetSelections(TestCodeEditor editor, params Selection[] selections) =>
        editor.ViewModel.SetSelections(null, selections);

    // Monaco: 'DeleteAllLeftAction' › 'should delete to the left of the cursor'
    [Fact]
    public void DeleteAllLeft_ShouldDeleteToTheLeftOfTheCursor() =>
        TestCodeEditor.WithTestCodeEditor(["one", "two", "three"], editor =>
        {
            TextModel model = editor.Model;
            SetSelections(editor, new Selection(1, 2, 1, 2));
            DeleteAllLeft(editor);
            Assert.Equal("ne", model.GetLineContent(1));

            SetSelections(editor, new Selection(2, 2, 2, 2), new Selection(3, 2, 3, 2));
            DeleteAllLeft(editor);
            Assert.Equal("wo", model.GetLineContent(2));
            Assert.Equal("hree", model.GetLineContent(3));
        });

    // Monaco: 'DeleteAllLeftAction' › 'should jump to the previous line when on first column'
    [Fact]
    public void DeleteAllLeft_ShouldJumpToThePreviousLineWhenOnFirstColumn() =>
        TestCodeEditor.WithTestCodeEditor(["one", "two", "three"], editor =>
        {
            TextModel model = editor.Model;
            SetSelections(editor, new Selection(2, 1, 2, 1));
            DeleteAllLeft(editor);
            Assert.Equal("onetwo", model.GetLineContent(1));

            SetSelections(editor, new Selection(1, 1, 1, 1), new Selection(2, 1, 2, 1));
            DeleteAllLeft(editor);
            Assert.Equal("onetwothree", model.GetLinesContent()[0]);
            Assert.Single(model.GetLinesContent());

            SetSelections(editor, new Selection(1, 1, 1, 1));
            DeleteAllLeft(editor);
            Assert.Equal("onetwothree", model.GetLinesContent()[0]);
        });

    // Monaco: 'DeleteAllLeftAction' › 'should keep deleting lines in multi cursor mode'
    [Fact]
    public void DeleteAllLeft_ShouldKeepDeletingLinesInMultiCursorMode() =>
        TestCodeEditor.WithTestCodeEditor(
            ["hi my name is Carlos Matos", "BCC", "waso waso waso", "my wife doesnt believe in me", "nonononono", "bitconneeeect"],
            editor =>
            {
                TextModel model = editor.Model;
                SetSelections(editor, new Selection(3, 5, 3, 5), new Selection(2, 4, 2, 4), new Selection(5, 11, 5, 11));
                DeleteAllLeft(editor);

                List<Selection> selections = editor.ViewModel.GetSelections();
                Assert.Equal(string.Empty, model.GetLineContent(2));
                Assert.Equal(" waso waso", model.GetLineContent(3));
                Assert.Equal(string.Empty, model.GetLineContent(5));
                Assert.Equal(new Selection(3, 1, 3, 1), selections[0]);
                Assert.Equal(new Selection(2, 1, 2, 1), selections[1]);
                Assert.Equal(new Selection(5, 1, 5, 1), selections[2]);

                DeleteAllLeft(editor);
                selections = editor.ViewModel.GetSelections();
                Assert.Equal("hi my name is Carlos Matos waso waso", model.GetLineContent(1));
                Assert.Equal(2, selections.Count);
                Assert.Equal(new Selection(1, 27, 1, 27), selections[0]);
                Assert.Equal(new Selection(2, 29, 2, 29), selections[1]);
            });

    // Monaco: 'DeleteAllLeftAction' › 'should work in multi cursor mode'
    [Fact]
    public void DeleteAllLeft_ShouldWorkInMultiCursorMode() =>
        TestCodeEditor.WithTestCodeEditor(["hello", "world", "hello world", "hello", "bonjour", "hola", "world", "hello world"], editor =>
        {
            TextModel model = editor.Model;
            SetSelections(editor, new Selection(1, 2, 1, 2), new Selection(1, 4, 1, 4));
            DeleteAllLeft(editor);
            Assert.Equal("lo", model.GetLineContent(1));

            SetSelections(editor, new Selection(2, 2, 2, 2), new Selection(2, 4, 2, 5));
            DeleteAllLeft(editor);
            Assert.Equal("d", model.GetLineContent(2));

            SetSelections(editor, new Selection(3, 2, 3, 5), new Selection(3, 7, 3, 7));
            DeleteAllLeft(editor);
            Assert.Equal("world", model.GetLineContent(3));

            SetSelections(editor, new Selection(4, 3, 4, 3), new Selection(4, 5, 5, 4));
            DeleteAllLeft(editor);
            Assert.Equal("jour", model.GetLineContent(4));

            SetSelections(editor, new Selection(5, 3, 6, 3), new Selection(6, 5, 7, 5), new Selection(7, 7, 7, 7));
            DeleteAllLeft(editor);
            Assert.Equal("world", model.GetLineContent(5));
        });

    // Monaco: 'DeleteAllLeftAction' › 'issue #36234: should push undo stop'
    [Fact]
    public void DeleteAllLeft_Issue36234_ShouldPushUndoStop() =>
        TestCodeEditor.WithTestCodeEditor(["one", "two", "three"], editor =>
        {
            TextModel model = editor.Model;
            SetSelections(editor, new Selection(1, 1, 1, 1));
            editor.ViewModel.Type("Typing some text here on line ", "keyboard");
            Assert.Equal("Typing some text here on line one", model.GetLineContent(1));
            Assert.Equal(new Selection(1, 31, 1, 31), editor.ViewModel.GetSelection());

            DeleteAllLeft(editor);
            Assert.Equal("one", model.GetLineContent(1));
            Assert.Equal(new Selection(1, 1, 1, 1), editor.ViewModel.GetSelection());

            CoreEditingCommands.Undo.RunCoreEditingCommand(editor.ViewModel);
            Assert.Equal("Typing some text here on line one", model.GetLineContent(1));
            Assert.Equal(new Selection(1, 31, 1, 31), editor.ViewModel.GetSelection());
        });

    // Monaco: 'DeleteAllRightAction' › 'should be noop on empty'
    [Fact]
    public void DeleteAllRight_ShouldBeNoopOnEmpty() =>
        TestCodeEditor.WithTestCodeEditor([string.Empty], editor =>
        {
            DeleteAllRight(editor);
            Assert.Equal([string.Empty], editor.Model.GetLinesContent());
            Assert.Equal([new Selection(1, 1, 1, 1)], editor.ViewModel.GetSelections());

            SetSelections(editor, new Selection(1, 1, 1, 1));
            DeleteAllRight(editor);
            Assert.Equal([string.Empty], editor.Model.GetLinesContent());
            Assert.Equal([new Selection(1, 1, 1, 1)], editor.ViewModel.GetSelections());

            SetSelections(editor, new Selection(1, 1, 1, 1), new Selection(1, 1, 1, 1), new Selection(1, 1, 1, 1));
            DeleteAllRight(editor);
            Assert.Equal([string.Empty], editor.Model.GetLinesContent());
            Assert.Equal([new Selection(1, 1, 1, 1)], editor.ViewModel.GetSelections());
        });

    // Monaco: 'DeleteAllRightAction' › 'should delete selected range'
    [Fact]
    public void DeleteAllRight_ShouldDeleteSelectedRange() =>
        TestCodeEditor.WithTestCodeEditor(["hello", "world"], editor =>
        {
            SetSelections(editor, new Selection(1, 2, 1, 5));
            DeleteAllRight(editor);
            Assert.Equal(["ho", "world"], editor.Model.GetLinesContent());
            Assert.Equal([new Selection(1, 2, 1, 2)], editor.ViewModel.GetSelections());

            SetSelections(editor, new Selection(1, 1, 2, 4));
            DeleteAllRight(editor);
            Assert.Equal(["ld"], editor.Model.GetLinesContent());
            Assert.Equal([new Selection(1, 1, 1, 1)], editor.ViewModel.GetSelections());

            SetSelections(editor, new Selection(1, 1, 1, 3));
            DeleteAllRight(editor);
            Assert.Equal([string.Empty], editor.Model.GetLinesContent());
            Assert.Equal([new Selection(1, 1, 1, 1)], editor.ViewModel.GetSelections());
        });

    // Monaco: 'DeleteAllRightAction' › 'should delete to the right of the cursor'
    [Fact]
    public void DeleteAllRight_ShouldDeleteToTheRightOfTheCursor() =>
        TestCodeEditor.WithTestCodeEditor(["hello", "world"], editor =>
        {
            SetSelections(editor, new Selection(1, 3, 1, 3));
            DeleteAllRight(editor);
            Assert.Equal(["he", "world"], editor.Model.GetLinesContent());
            Assert.Equal([new Selection(1, 3, 1, 3)], editor.ViewModel.GetSelections());

            SetSelections(editor, new Selection(2, 1, 2, 1));
            DeleteAllRight(editor);
            Assert.Equal(["he", string.Empty], editor.Model.GetLinesContent());
            Assert.Equal([new Selection(2, 1, 2, 1)], editor.ViewModel.GetSelections());
        });

    // Monaco: 'DeleteAllRightAction' › 'should join two lines, if at the end of the line'
    [Fact]
    public void DeleteAllRight_ShouldJoinTwoLinesIfAtTheEndOfTheLine() =>
        TestCodeEditor.WithTestCodeEditor(["hello", "world"], editor =>
        {
            SetSelections(editor, new Selection(1, 6, 1, 6));
            DeleteAllRight(editor);
            Assert.Equal(["helloworld"], editor.Model.GetLinesContent());
            Assert.Equal([new Selection(1, 6, 1, 6)], editor.ViewModel.GetSelections());

            SetSelections(editor, new Selection(1, 6, 1, 6));
            DeleteAllRight(editor);
            Assert.Equal(["hello"], editor.Model.GetLinesContent());
            Assert.Equal([new Selection(1, 6, 1, 6)], editor.ViewModel.GetSelections());

            SetSelections(editor, new Selection(1, 6, 1, 6));
            DeleteAllRight(editor);
            Assert.Equal(["hello"], editor.Model.GetLinesContent());
            Assert.Equal([new Selection(1, 6, 1, 6)], editor.ViewModel.GetSelections());
        });

    // Monaco: 'DeleteAllRightAction' › 'should work with multiple cursors'
    [Fact]
    public void DeleteAllRight_ShouldWorkWithMultipleCursors() =>
        TestCodeEditor.WithTestCodeEditor(["hello", "there", "world"], editor =>
        {
            SetSelections(editor, new Selection(1, 3, 1, 3), new Selection(1, 6, 1, 6), new Selection(3, 4, 3, 4));
            DeleteAllRight(editor);
            Assert.Equal(["hethere", "wor"], editor.Model.GetLinesContent());
            Assert.Equal([new Selection(1, 3, 1, 3), new Selection(2, 4, 2, 4)], editor.ViewModel.GetSelections());

            DeleteAllRight(editor);
            Assert.Equal(["he", "wor"], editor.Model.GetLinesContent());
            Assert.Equal([new Selection(1, 3, 1, 3), new Selection(2, 4, 2, 4)], editor.ViewModel.GetSelections());

            DeleteAllRight(editor);
            Assert.Equal(["hewor"], editor.Model.GetLinesContent());
            Assert.Equal([new Selection(1, 3, 1, 3), new Selection(1, 6, 1, 6)], editor.ViewModel.GetSelections());

            DeleteAllRight(editor);
            Assert.Equal(["he"], editor.Model.GetLinesContent());
            Assert.Equal([new Selection(1, 3, 1, 3)], editor.ViewModel.GetSelections());

            DeleteAllRight(editor);
            Assert.Equal(["he"], editor.Model.GetLinesContent());
            Assert.Equal([new Selection(1, 3, 1, 3)], editor.ViewModel.GetSelections());
        });

    // Monaco: 'DeleteAllRightAction' › 'should work with undo/redo'
    [Fact]
    public void DeleteAllRight_ShouldWorkWithUndoRedo() =>
        TestCodeEditor.WithTestCodeEditor(["hello", "there", "world"], editor =>
        {
            SetSelections(editor, new Selection(1, 3, 1, 3), new Selection(1, 6, 1, 6), new Selection(3, 4, 3, 4));
            DeleteAllRight(editor);
            Assert.Equal(["hethere", "wor"], editor.Model.GetLinesContent());
            Assert.Equal([new Selection(1, 3, 1, 3), new Selection(2, 4, 2, 4)], editor.ViewModel.GetSelections());

            CoreEditingCommands.Undo.RunCoreEditingCommand(editor.ViewModel);
            Assert.Equal([new Selection(1, 3, 1, 3), new Selection(1, 6, 1, 6), new Selection(3, 4, 3, 4)], editor.ViewModel.GetSelections());

            CoreEditingCommands.Redo.RunCoreEditingCommand(editor.ViewModel);
            Assert.Equal([new Selection(1, 3, 1, 3), new Selection(2, 4, 2, 4)], editor.ViewModel.GetSelections());
        });
}

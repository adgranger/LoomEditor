using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Features.LinesOperations;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.LinesOperations;

// Monaco: suite 'JoinLinesAction'
public partial class LinesOperationsTests
{
    private static readonly string[] JoinText =
    [
        "hello", "world", "hello ", "world", "hello\t\t", "\tworld", "hello   ", "\tworld", string.Empty, string.Empty, "hello world",
    ];

    private static void JoinLines(TestCodeEditor editor) => LinesOperationsActions.JoinLines(editor.ViewModel);

    // Monaco: 'should join lines and insert space if necessary'
    [Fact]
    public void JoinLines_ShouldJoinLinesAndInsertSpaceIfNecessary() =>
        TestCodeEditor.WithTestCodeEditor(JoinText, editor =>
        {
            SetSelections(editor, new Selection(1, 2, 1, 2));
            JoinLines(editor);
            Assert.Equal("hello world", editor.Model.GetLineContent(1));
            AssertSelection(editor, new Selection(1, 6, 1, 6));

            SetSelections(editor, new Selection(2, 2, 2, 2));
            JoinLines(editor);
            Assert.Equal("hello world", editor.Model.GetLineContent(2));
            AssertSelection(editor, new Selection(2, 7, 2, 7));

            SetSelections(editor, new Selection(3, 2, 3, 2));
            JoinLines(editor);
            Assert.Equal("hello world", editor.Model.GetLineContent(3));
            AssertSelection(editor, new Selection(3, 7, 3, 7));

            SetSelections(editor, new Selection(4, 2, 5, 3));
            JoinLines(editor);
            Assert.Equal("hello world", editor.Model.GetLineContent(4));
            AssertSelection(editor, new Selection(4, 2, 4, 8));

            SetSelections(editor, new Selection(5, 1, 7, 3));
            JoinLines(editor);
            Assert.Equal("hello world", editor.Model.GetLineContent(5));
            AssertSelection(editor, new Selection(5, 1, 5, 3));
        });

    // Monaco: '#50471 Join lines at the end of document'
    [Fact]
    public void JoinLines_Issue50471_JoinLinesAtTheEndOfDocument() =>
        TestCodeEditor.WithTestCodeEditor(["hello", "world"], editor =>
        {
            SetSelections(editor, new Selection(2, 1, 2, 1));
            JoinLines(editor);
            Assert.Equal("hello", editor.Model.GetLineContent(1));
            Assert.Equal("world", editor.Model.GetLineContent(2));
            AssertSelection(editor, new Selection(2, 6, 2, 6));
        });

    // Monaco: 'should work in multi cursor mode'
    [Fact]
    public void JoinLines_ShouldWorkInMultiCursorMode() =>
        TestCodeEditor.WithTestCodeEditor(JoinText, editor =>
        {
            SetSelections(
                editor,
                /* primary cursor */
                new Selection(5, 2, 5, 2),
                new Selection(1, 2, 1, 2),
                new Selection(3, 2, 4, 2),
                new Selection(5, 4, 6, 3),
                new Selection(7, 5, 8, 4),
                new Selection(10, 1, 10, 1));
            JoinLines(editor);
            Assert.Equal("hello world\nhello world\nhello world\nhello world\n\nhello world", string.Join('\n', editor.Model.GetLinesContent()));
            AssertSelection(
                editor,
                /* primary cursor */
                new Selection(3, 4, 3, 8),
                new Selection(1, 6, 1, 6),
                new Selection(2, 2, 2, 8),
                new Selection(4, 5, 4, 9),
                new Selection(6, 1, 6, 1));
        });

    // Monaco: 'should push undo stop'
    [Fact]
    public void JoinLines_ShouldPushUndoStop() =>
        TestCodeEditor.WithTestCodeEditor(["hello", "world"], editor =>
        {
            SetSelections(editor, new Selection(1, 6, 1, 6));
            editor.ViewModel.Type(" my dear", "keyboard");
            Assert.Equal("hello my dear", editor.Model.GetLineContent(1));
            Assert.Equal(new Selection(1, 14, 1, 14), editor.ViewModel.GetSelection());

            JoinLines(editor);
            Assert.Equal("hello my dear world", editor.Model.GetLineContent(1));
            Assert.Equal(new Selection(1, 14, 1, 14), editor.ViewModel.GetSelection());

            CoreEditingCommands.Undo.RunCoreEditingCommand(editor.ViewModel);
            Assert.Equal("hello my dear", editor.Model.GetLineContent(1));
            Assert.Equal(new Selection(1, 14, 1, 14), editor.ViewModel.GetSelection());
        });

    [Fact]
    public void JoinLines_TrailingNoBreakSpace_IsReplacedByOneSpace() =>
        TestCodeEditor.WithTestCodeEditor(["a \t", "b"], editor =>
        {
            SetSelections(editor, new Selection(1, 1, 1, 1));
            JoinLines(editor);
            Assert.Equal("a b", editor.Model.GetLineContent(1));
        });
}

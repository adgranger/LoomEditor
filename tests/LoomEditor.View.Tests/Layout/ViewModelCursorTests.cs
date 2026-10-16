using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Events;
using LoomEditor.View.Tests.Cursors.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

// Monaco has no test for the cursor part of ViewModel on its own (cursor.test.ts goes through it): all tests are new.
public class ViewModelCursorTests
{
    private static Selection Caret(int line, int column) => Selection.Collapsed(new Position(line, column));

    [Fact]
    public void WithTestCodeEditor_NewEditor_PrimarySelectionIsAtTheStartAndPageSizeIsThree()
    {
        TestCodeEditor.WithTestCodeEditor("abc", editor =>
        {
            Assert.Equal(Caret(1, 1), editor.ViewModel.GetSelection());
            Assert.Equal(3, editor.ViewModel.CursorConfig.PageSize);
        });
    }

    [Fact]
    public void SetSelections_TwoSelections_AreReturnedAndOneOutgoingEventIsRaised()
    {
        TestCodeEditor.WithTestCodeEditor("abcdefgh", editor =>
        {
            var events = new List<OutgoingViewModelEvent>();
            editor.ViewModel.EventDispatcher.OnEvent += (_, e) => events.Add(e);

            editor.ViewModel.SetSelections("test", [Caret(1, 2), Caret(1, 5)]);

            Assert.Equal([Caret(1, 2), Caret(1, 5)], editor.ViewModel.GetSelections());
            CursorStateChangedEvent raised = Assert.IsType<CursorStateChangedEvent>(Assert.Single(events));
            Assert.Equal("test", raised.Source);
        });
    }

    [Fact]
    public void ModelEdit_ByAnotherParty_UpdatesTheLayoutThenMovesTheCursor()
    {
        TestCodeEditor.WithTestCodeEditor("abc", editor =>
        {
            editor.ViewModel.SetSelections("test", [Caret(1, 3)]);
            int layoutOffsetAtCursorUpdate = -1;
            editor.ViewModel.EventDispatcher.OnEvent += (_, _) =>
                layoutOffsetAtCursorUpdate = editor.ViewModel.ViewLayout.GetVerticalOffsetAfterLineNumber(2);

            editor.Model.PushEditOperations(null, [new TextEdit(new Range(1, 1, 1, 1), "XX\n")], null);

            Assert.Equal(Caret(2, 3), editor.ViewModel.GetSelection());
            Assert.Equal(2 * 19, layoutOffsetAtCursorUpdate);
        });
    }

    [Fact]
    public void CursorConfig_ModelOptionsOrViewportChange_IsRecreated()
    {
        TestCodeEditor.WithTestCodeEditor("abc", editor =>
        {
            editor.Model.UpdateOptions(new TextModelUpdateOptions(TabSize: 8));
            Assert.Equal(8, editor.ViewModel.CursorConfig.TabSize);

            editor.ViewModel.SetViewportSize(100, 400);
            Assert.Equal(19, editor.ViewModel.CursorConfig.PageSize);
        });
    }

    [Fact]
    public void ExecuteCommands_ReadOnly_RaisesReadOnlyEditAttemptAndDoesNothing()
    {
        var options = new TestCodeEditorOptions(CursorOptions: EditorCursorOptions.Default with { ReadOnly = true });
        TestCodeEditor.WithTestCodeEditor("abc", editor =>
        {
            var events = new List<OutgoingViewModelEvent>();
            editor.ViewModel.EventDispatcher.OnEvent += (_, e) => events.Add(e);

            editor.ViewModel.ExecuteCommands([new InsertTextCommand(new Range(1, 1, 1, 1), "X")]);

            Assert.Equal("abc", editor.Model.GetValue());
            Assert.IsType<ReadOnlyEditAttemptEvent>(Assert.Single(events));
        }, options);
    }
}

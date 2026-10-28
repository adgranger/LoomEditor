using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Cursors;
using LoomEditor.View.Features;
using LoomEditor.View.Features.LinesOperations;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.LinesOperations;

// Monaco: suite 'Editor Contrib - Line Operations' (the sort, delete duplicate lines and reverse lines suites)
public partial class LinesOperationsTests
{
    private static void AssertSelection(TestCodeEditor editor, params Selection[] expected) =>
        Assert.Equal(expected, editor.ViewModel.GetSelections());

    private static void SortAscending(TestCodeEditor editor) =>
        LinesOperationsActions.SortLines(editor.ViewModel, false, LinesOperationsCommands.SortLinesAscending);

    private static void SortDescending(TestCodeEditor editor) =>
        LinesOperationsActions.SortLines(editor.ViewModel, true, LinesOperationsCommands.SortLinesDescending);

    // Monaco: 'SortLinesAscendingAction' › 'should sort selected lines in ascending order'
    [Fact]
    public void SortLinesAscending_ShouldSortSelectedLinesInAscendingOrder() =>
        TestCodeEditor.WithTestCodeEditor(["omicron", "beta", "alpha"], editor =>
        {
            editor.ViewModel.SetSelections(null, [new Selection(1, 1, 3, 5)]);
            SortAscending(editor);
            Assert.Equal(["alpha", "beta", "omicron"], editor.Model.GetLinesContent());
            AssertSelection(editor, new Selection(1, 1, 3, 7));
        });

    // Monaco: 'SortLinesAscendingAction' › 'should sort lines in ascending order'
    [Fact]
    public void SortLinesAscending_ShouldSortLinesInAscendingOrder() =>
        TestCodeEditor.WithTestCodeEditor(["omicron", "beta", "alpha"], editor =>
        {
            SortAscending(editor);
            Assert.Equal(["alpha", "beta", "omicron"], editor.Model.GetLinesContent());
        });

    // Monaco: 'SortLinesAscendingAction' › 'should sort multiple selections in ascending order'
    [Fact]
    public void SortLinesAscending_ShouldSortMultipleSelectionsInAscendingOrder() =>
        TestCodeEditor.WithTestCodeEditor(["omicron", "beta", "alpha", string.Empty, "omicron", "beta", "alpha"], editor =>
        {
            editor.ViewModel.SetSelections(null, [new Selection(1, 1, 3, 5), new Selection(5, 1, 7, 5)]);
            SortAscending(editor);
            Assert.Equal(["alpha", "beta", "omicron", string.Empty, "alpha", "beta", "omicron"], editor.Model.GetLinesContent());
            AssertSelection(editor, new Selection(1, 1, 3, 7), new Selection(5, 1, 7, 7));
        });

    // Monaco: 'SortLinesAscendingAction' › 'applies to whole document when selection is single line'
    [Fact]
    public void SortLinesAscending_AppliesToWholeDocumentWhenSelectionIsSingleLine() =>
        TestCodeEditor.WithTestCodeEditor(["omicron", "beta", "alpha"], editor =>
        {
            editor.ViewModel.SetSelections(null, [new Selection(2, 1, 2, 4)]);
            SortAscending(editor);
            Assert.Equal(["alpha", "beta", "omicron"], editor.Model.GetLinesContent());
        });

    // Monaco: 'SortLinesDescendingAction' › 'should sort selected lines in descending order'
    [Fact]
    public void SortLinesDescending_ShouldSortSelectedLinesInDescendingOrder() =>
        TestCodeEditor.WithTestCodeEditor(["alpha", "beta", "omicron"], editor =>
        {
            editor.ViewModel.SetSelections(null, [new Selection(1, 1, 3, 7)]);
            SortDescending(editor);
            Assert.Equal(["omicron", "beta", "alpha"], editor.Model.GetLinesContent());
            AssertSelection(editor, new Selection(1, 1, 3, 5));
        });

    // Monaco: 'SortLinesDescendingAction' › 'should sort multiple selections in descending order'
    [Fact]
    public void SortLinesDescending_ShouldSortMultipleSelectionsInDescendingOrder() =>
        TestCodeEditor.WithTestCodeEditor(["alpha", "beta", "omicron", string.Empty, "alpha", "beta", "omicron"], editor =>
        {
            editor.ViewModel.SetSelections(null, [new Selection(1, 1, 3, 7), new Selection(5, 1, 7, 7)]);
            SortDescending(editor);
            Assert.Equal(["omicron", "beta", "alpha", string.Empty, "omicron", "beta", "alpha"], editor.Model.GetLinesContent());
            AssertSelection(editor, new Selection(1, 1, 3, 5), new Selection(5, 1, 7, 5));
        });

    // Monaco: 'DeleteDuplicateLinesAction' › 'should remove duplicate lines within selection'
    [Fact]
    public void RemoveDuplicateLines_ShouldRemoveDuplicateLinesWithinSelection() =>
        TestCodeEditor.WithTestCodeEditor(["alpha", "beta", "beta", "beta", "alpha", "omicron"], editor =>
        {
            editor.ViewModel.SetSelections(null, [new Selection(1, 3, 6, 4)]);
            LinesOperationsActions.RemoveDuplicateLines(editor.ViewModel);
            Assert.Equal(["alpha", "beta", "omicron"], editor.Model.GetLinesContent());
            AssertSelection(editor, new Selection(1, 1, 3, 8));
        });

    // Monaco: 'DeleteDuplicateLinesAction' › 'should remove duplicate lines'
    [Fact]
    public void RemoveDuplicateLines_ShouldRemoveDuplicateLines() =>
        TestCodeEditor.WithTestCodeEditor(["alpha", "beta", "beta", "beta", "alpha", "omicron"], editor =>
        {
            LinesOperationsActions.RemoveDuplicateLines(editor.ViewModel);
            Assert.Equal(["alpha", "beta", "omicron"], editor.Model.GetLinesContent());
            Assert.True(editor.ViewModel.GetSelection().IsEmpty);
        });

    // Monaco: 'DeleteDuplicateLinesAction' › 'should remove duplicate lines in multiple selections'
    [Fact]
    public void RemoveDuplicateLines_ShouldRemoveDuplicateLinesInMultipleSelections() =>
        TestCodeEditor.WithTestCodeEditor(["alpha", "beta", "beta", "omicron", string.Empty, "alpha", "alpha", "beta"], editor =>
        {
            editor.ViewModel.SetSelections(null, [new Selection(1, 2, 4, 3), new Selection(6, 2, 8, 3)]);
            LinesOperationsActions.RemoveDuplicateLines(editor.ViewModel);
            Assert.Equal(["alpha", "beta", "omicron", string.Empty, "alpha", "beta"], editor.Model.GetLinesContent());
            AssertSelection(editor, new Selection(1, 1, 3, 8), new Selection(5, 1, 6, 5));
        });

    // Monaco: 'DeleteDuplicateLinesAction' › 'applies to whole document when selection is single line'
    [Fact]
    public void RemoveDuplicateLines_AppliesToWholeDocumentWhenSelectionIsSingleLine() =>
        TestCodeEditor.WithTestCodeEditor(["alpha", "beta", "alpha", "omicron"], editor =>
        {
            editor.ViewModel.SetSelections(null, [new Selection(2, 1, 2, 2)]);
            LinesOperationsActions.RemoveDuplicateLines(editor.ViewModel);
            Assert.Equal(["alpha", "beta", "omicron"], editor.Model.GetLinesContent());
        });

    private sealed class FocusedEditor : IDisposable
    {
        private readonly EditorFeatureHost _host;

        public FocusedEditor(string text)
        {
            Context = new EditorContext();
            Context.SetText(text);
            _host = new EditorFeatureHost(Context, [() => new LinesOperationsFeature()]);
        }

        public EditorContext Context { get; }

        public void Dispose()
        {
            _host.Dispose();
            Context.Dispose();
        }
    }

    [Fact]
    public void Keybindings_MoveLinesDownAndTrimChord_ResolveToTheLinesOperationsCommands()
    {
        var focused = new KeybindingContext(TextInputFocus: true, EditorTextFocus: true);
        var dispatcher = new KeybindingDispatcher(KeybindingResolver.Default);

        Assert.True(dispatcher.Dispatch(new Keybinding(KeyModifiers.Alt, KeyCode.DownArrow), focused, out string? moveDown));
        Assert.Equal(LinesOperationsCommands.MoveLinesDown, moveDown);

        Assert.True(dispatcher.Dispatch(new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyK), focused, out string? first));
        Assert.Null(first);
        Assert.True(dispatcher.Dispatch(new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyX), focused, out string? trim));
        Assert.Equal(LinesOperationsCommands.TrimTrailingWhitespace, trim);
    }

    [Fact]
    public void Keybindings_NotWritable_ResolveToNothing()
    {
        var readOnly = new KeybindingContext(TextInputFocus: true, EditorTextFocus: true, EditorReadOnly: true);

        Assert.Null(KeybindingResolver.Default.Resolve(new Keybinding(KeyModifiers.Alt, KeyCode.DownArrow), readOnly));
    }

    [Fact]
    public void MoveLinesDown_ReadOnly_DoesNotRun()
    {
        using var editor = new FocusedEditor("a\nb");
        editor.Context.ViewModel.SetCursorOptions(new EditorCursorOptions { ReadOnly = true });

        bool ran = editor.Context.Commands.Execute(LinesOperationsCommands.MoveLinesDown);

        Assert.False(ran);
        Assert.Equal("a\nb", editor.Context.Model.GetValue());
    }

    [Fact]
    public void MoveLinesDown_TwoCursors_MovesBothBlocksInOneUndoStep()
    {
        using var editor = new FocusedEditor("a\nb\nc\nd\ne");
        editor.Context.SetSelections([Selection.Collapsed(new Position(1, 1)), Selection.Collapsed(new Position(3, 1))]);

        editor.Context.Commands.Execute(LinesOperationsCommands.MoveLinesDown);
        Assert.Equal("b\na\nd\nc\ne", editor.Context.Model.GetValue());

        editor.Context.Model.Undo();
        Assert.Equal("a\nb\nc\nd\ne", editor.Context.Model.GetValue());
    }
}

using LoomEditor.Core.Text;
using LoomEditor.View.Features.LinesOperations;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.LinesOperations;

// Monaco: suite 'ReverseLinesAction'
public partial class LinesOperationsTests
{
    private static void Reverse(TestCodeEditor editor) => LinesOperationsActions.ReverseLines(editor.ViewModel);

    // Monaco: 'reverses lines'
    [Fact]
    public void ReverseLines_ReversesLines() =>
        TestCodeEditor.WithTestCodeEditor(["alice", "bob", "charlie"], editor =>
        {
            Reverse(editor);
            Assert.Equal(["charlie", "bob", "alice"], editor.Model.GetLinesContent());
        });

    // Monaco: 'excludes empty last line'
    [Fact]
    public void ReverseLines_ExcludesEmptyLastLine() =>
        TestCodeEditor.WithTestCodeEditor(["alice", "bob", "charlie", string.Empty], editor =>
        {
            Reverse(editor);
            Assert.Equal(["charlie", "bob", "alice", string.Empty], editor.Model.GetLinesContent());
        });

    // Monaco: 'updates cursor'
    [Fact]
    public void ReverseLines_UpdatesCursor() =>
        TestCodeEditor.WithTestCodeEditor(["alice", "bob", "charlie"], editor =>
        {
            // cursor at third column of third line 'charlie'
            editor.ViewModel.SetSelections(null, [Selection.Collapsed(new Position(3, 3))]);
            Reverse(editor);
            // cursor at third column of *first* line 'charlie'
            Assert.Equal(new Position(1, 3), editor.ViewModel.GetSelection().Position);
        });

    // Monaco: 'preserves cursor on empty last line'
    [Fact]
    public void ReverseLines_PreservesCursorOnEmptyLastLine() =>
        TestCodeEditor.WithTestCodeEditor(["alice", "bob", "charlie", string.Empty], editor =>
        {
            editor.ViewModel.SetSelections(null, [Selection.Collapsed(new Position(4, 1))]);
            Reverse(editor);
            Assert.Equal(new Position(4, 1), editor.ViewModel.GetSelection().Position);
        });

    // Monaco: 'preserves selected text when selections do not span lines'
    [Fact]
    public void ReverseLines_PreservesSelectedTextWhenSelectionsDoNotSpanLines() =>
        TestCodeEditor.WithTestCodeEditor(["alice", "bob", "charlie", string.Empty], editor =>
        {
            editor.ViewModel.SetSelections(null, [new Selection(1, 1, 1, 3), new Selection(2, 1, 2, 4), new Selection(3, 1, 3, 5)]);
            string[] expectedSelectedText = ["al", "bob", "char"];
            Assert.Equal(expectedSelectedText, editor.ViewModel.GetSelections().Select(s => editor.Model.GetValueInRange(s.Range)));
            Reverse(editor);
            Assert.Equal(expectedSelectedText, editor.ViewModel.GetSelections().Select(s => editor.Model.GetValueInRange(s.Range)));
        });

    // Monaco: 'reverses lines within selection'
    [Fact]
    public void ReverseLines_ReversesLinesWithinSelection() =>
        TestCodeEditor.WithTestCodeEditor(["line1", "line2", "line3", "line4", "line5"], editor =>
        {
            // Select lines 2-4
            editor.ViewModel.SetSelections(null, [new Selection(2, 1, 4, 6)]);
            Reverse(editor);
            Assert.Equal(["line1", "line4", "line3", "line2", "line5"], editor.Model.GetLinesContent());
        });

    // Monaco: 'reverses lines within partial selection'
    [Fact]
    public void ReverseLines_ReversesLinesWithinPartialSelection() =>
        TestCodeEditor.WithTestCodeEditor(["line1", "line2", "line3", "line4", "line5"], editor =>
        {
            // Select partial lines 2-4 (from middle of line2 to middle of line4)
            editor.ViewModel.SetSelections(null, [new Selection(2, 3, 4, 3)]);
            Reverse(editor);
            Assert.Equal(["line1", "line4", "line3", "line2", "line5"], editor.Model.GetLinesContent());
        });

    // Monaco: 'reverses lines with multiple selections'
    [Fact]
    public void ReverseLines_ReversesLinesWithMultipleSelections() =>
        TestCodeEditor.WithTestCodeEditor(["line1", "line2", "line3", "line4", "line5", "line6"], editor =>
        {
            // Select lines 1-2 and lines 4-5
            editor.ViewModel.SetSelections(null, [new Selection(1, 1, 2, 6), new Selection(4, 1, 5, 6)]);
            Reverse(editor);
            Assert.Equal(["line2", "line1", "line3", "line5", "line4", "line6"], editor.Model.GetLinesContent());
        });

    // Monaco: 'updates selection positions after reversal'
    [Fact]
    public void ReverseLines_UpdatesSelectionPositionsAfterReversal() =>
        TestCodeEditor.WithTestCodeEditor(["line1", "line2", "line3", "line4"], editor =>
        {
            // Select lines 1-3
            editor.ViewModel.SetSelections(null, [new Selection(1, 2, 3, 3)]);
            Reverse(editor);
            // Originally line 1 col 2 -> line 3 col 3, so after reversal it is line 3 col 2 -> line 1 col 3
            Range range = editor.ViewModel.GetSelection().Range;
            Assert.Equal(1, range.StartLineNumber);
            Assert.Equal(3, range.StartColumn);
            Assert.Equal(3, range.EndLineNumber);
            Assert.Equal(2, range.EndColumn);
        });

    // Monaco: 'applies to whole document when selection is single line' (first of the two with this name)
    [Fact]
    public void ReverseLines_AppliesToWholeDocumentWhenSelectionIsSingleLine_1() =>
        TestCodeEditor.WithTestCodeEditor(["line1", "line2", "line3"], editor =>
        {
            // Select only line 2
            editor.ViewModel.SetSelections(null, [new Selection(2, 1, 2, 6)]);
            Reverse(editor);
            Assert.Equal(["line3", "line2", "line1"], editor.Model.GetLinesContent());
        });

    // Monaco: 'excludes end line when selection ends at column 1'
    [Fact]
    public void ReverseLines_ExcludesEndLineWhenSelectionEndsAtColumn1() =>
        TestCodeEditor.WithTestCodeEditor(["line1", "line2", "line3", "line4", "line5"], editor =>
        {
            // Select from line 2 to line 4 column 1 (should exclude line 4)
            editor.ViewModel.SetSelections(null, [new Selection(2, 1, 4, 1)]);
            Reverse(editor);
            Assert.Equal(["line1", "line3", "line2", "line4", "line5"], editor.Model.GetLinesContent());
        });

    // Monaco: 'applies to whole document when selection is single line' (second of the two with this name)
    [Fact]
    public void ReverseLines_AppliesToWholeDocumentWhenSelectionIsSingleLine_2() =>
        TestCodeEditor.WithTestCodeEditor(["omicron", "beta", "alpha"], editor =>
        {
            editor.ViewModel.SetSelections(null, [new Selection(2, 1, 2, 4)]);
            Reverse(editor);
            Assert.Equal(["alpha", "beta", "omicron"], editor.Model.GetLinesContent());
        });
}

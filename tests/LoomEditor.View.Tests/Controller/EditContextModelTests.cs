using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Controller;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Controller;

// No Monaco test covers this logic: nativeEditContext.ts is only tested through the DOM.
public class EditContextModelTests
{
    private static EditContextModel WithSelectionAt(string text, Selection selection)
    {
        // Gives the IME the text of the lines of `selection` and makes its selection the previous one.
        var model = new EditContextModel();
        TextModel textModel = new(text);
        model.Update(textModel, selection);
        return model;
    }

    [Fact]
    public void GetNewEditContextState_SelectionOnTwoLines_GivesTheFullLinesAndTheOffsets()
    {
        TextModel model = new("abc\ndef");

        EditContextState? state = EditContextState.GetNewEditContextState(model, new Selection(1, 2, 2, 3));

        Assert.NotNull(state);
        Assert.Equal("abc\ndef", state.Text);
        Assert.Equal(1, state.SelectionStartOffset);
        Assert.Equal(6, state.SelectionEndOffset);
    }

    [Fact]
    public void GetNewEditContextState_InvalidRange_IsNull()
    {
        TextModel model = new("abc");

        Assert.Null(EditContextState.GetNewEditContextState(model, new Selection(1, 1, 5, 1)));
    }

    [Fact]
    public void OnTextUpdate_InsertAtACollapsedSelection_IsAPlainType()
    {
        EditContextModel model = WithSelectionAt("abc", new Selection(1, 4, 1, 4));

        TypeData? data = model.OnTextUpdate(new TextUpdate(3, 3, "x", 4, 4));

        Assert.Equal(new TypeData("x", 0, 0, 0), data);
    }

    [Fact]
    public void OnTextUpdate_ImeReplacesThePreviousCharacters_CountsThem()
    {
        EditContextModel model = WithSelectionAt("12345", new Selection(1, 6, 1, 6));

        TypeData? data = model.OnTextUpdate(new TextUpdate(3, 5, "せん", 5, 5));

        Assert.Equal(new TypeData("せん", 2, 0, 0), data);
    }

    [Fact]
    public void OnTextUpdate_ImeMovesItsCaretInsideTheComposition_GivesAPositionDelta()
    {
        EditContextModel model = WithSelectionAt("12345", new Selection(1, 6, 1, 6));

        TypeData? data = model.OnTextUpdate(new TextUpdate(3, 5, "せん", 4, 4));

        Assert.Equal(new TypeData("せん", 2, 0, -1), data);
    }

    [Fact]
    public void OnTextUpdate_HighSurrogateAlone_WaitsForTheLowSurrogate()
    {
        EditContextModel model = WithSelectionAt("ab", new Selection(1, 3, 1, 3));

        TypeData? first = model.OnTextUpdate(new TextUpdate(2, 2, "\uD83D", 3, 3));
        TypeData? second = model.OnTextUpdate(new TextUpdate(3, 3, "\uDE05", 4, 4));

        Assert.Null(first);

        // The update range is shifted by -1: the pair replaces [2,2) and the caret (offset 4) is at its end.
        Assert.Equal(new TypeData("😅", 0, 0, 0), second);
    }

    [Fact]
    public void Update_TextChangedOnlyWhenTheLinesChanged()
    {
        var editContext = new EditContextModel();
        TextModel model = new("abc\ndef");

        (bool textChanged, bool selectionChanged) first = editContext.Update(model, new Selection(1, 2, 1, 2));
        (bool textChanged, bool selectionChanged) sameLine = editContext.Update(model, new Selection(1, 3, 1, 3));
        (bool textChanged, bool selectionChanged) otherLine = editContext.Update(model, new Selection(2, 2, 2, 2));

        Assert.Equal((true, true), first);
        Assert.Equal((false, true), sameLine);
        Assert.Equal((true, true), otherLine);
        Assert.Equal("def", editContext.Text);
        Assert.Equal((1, 1), editContext.Selection);
    }

    [Fact]
    public void OnSelectionUpdate_EmptySelectionMoves_GivesAPositionDelta()
    {
        EditContextModel model = WithSelectionAt("12345", new Selection(1, 4, 1, 4));

        Assert.Equal(new TypeData(string.Empty, 0, 0, -2), model.OnSelectionUpdate(1, 1));
        Assert.Equal((1, 1), model.Selection);
    }

    [Fact]
    public void OnSelectionUpdate_NonEmptyOrUnchanged_IsNullButStoresTheSelection()
    {
        EditContextModel model = WithSelectionAt("12345", new Selection(1, 4, 1, 4));

        Assert.Null(model.OnSelectionUpdate(3, 3));
        Assert.Null(model.OnSelectionUpdate(1, 3));
        Assert.Equal((1, 3), model.Selection);
    }

    [Fact]
    public void Apply_PlainTypeData_TypesTheText()
    {
        TestCodeEditor.WithTestCodeEditor(
            [string.Empty],
            editor =>
            {
                EditContextModel.Apply(editor.ViewModel, new TypeData("x", 0, 0, 0));

                Assert.Equal("x", editor.Model.GetLineContent(1));
            });
    }

    [Fact]
    public void Apply_ReplaceCounts_UsesCompositionType()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["ab"],
            editor =>
            {
                MoveTo(editor.ViewModel, 1, 3);

                EditContextModel.Apply(editor.ViewModel, new TypeData("せ", 1, 0, 0));

                Assert.Equal("aせ", editor.Model.GetLineContent(1));
            });
    }
}

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Tests.Model.TestHelpers;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model;

/// <summary>
/// Tests of <c>trimAutoWhitespace</c> in <c>PushEditOperations</c>. New tests: Monaco tests this through the cursor
/// (<c>removeAutoWhitespace</c> in <c>cursor.test.ts</c>, ported with typing).
/// </summary>
public class TextModelTrimAutoWhitespaceTests
{
    // What Enter does on "  x": insert "\n  " at the end of line 1, marked as an auto whitespace edit.
    // Line 2 is then "  " and is recorded as a trim candidate.
    private static TextModel CreateAfterEnter(string text = "  x")
    {
        var model = TestTextModels.Create(text);
        model.PushEditOperations(
            null,
            [new IdentifiedEditOperation(new Range(1, 4, 1, 4), "\n  ", IsAutoWhitespaceEdit: true)],
            null);
        model.PushStackElement();
        Assert.Equal("  ", model.GetLineContent(2));
        return model;
    }

    private static Selection Caret(int line, int column) => new(new Position(line, column), new Position(line, column));

    [Fact]
    public void PushEditOperations_NewLineAfterTheTrimLine_TrimsItAndUndoRestoresIt()
    {
        var model = CreateAfterEnter();

        model.PushEditOperations(
            [Caret(2, 3)],
            [new IdentifiedEditOperation(new Range(2, 3, 2, 3), "\n")],
            null);

        Assert.Equal("", model.GetLineContent(2));
        Assert.Equal("", model.GetLineContent(3));
        Assert.Equal(3, model.LineCount);

        model.Undo();

        Assert.Equal("  x\n  ", model.GetValue());
    }

    [Fact]
    public void PushEditOperations_EditTouchingTheTrimLine_DoesNotTrimIt()
    {
        var model = CreateAfterEnter();

        model.PushEditOperations(
            [Caret(2, 3)],
            [new IdentifiedEditOperation(new Range(2, 3, 2, 3), "z")],
            null);

        Assert.Equal("  z", model.GetLineContent(2));
    }

    [Fact]
    public void PushEditOperations_EditFarFromTheCursor_DoesNotTrim()
    {
        var model = CreateAfterEnter("  x" + string.Concat(Enumerable.Repeat("\nl", 18)));
        Assert.Equal(20, model.LineCount);

        model.PushEditOperations(
            [Caret(2, 3)],
            [new IdentifiedEditOperation(new Range(15, 1, 15, 1), "q")],
            null);

        Assert.Equal("  ", model.GetLineContent(2));
        Assert.StartsWith("q", model.GetLineContent(15), StringComparison.Ordinal);
    }

    [Fact]
    public void PushEditOperations_TrimAutoWhitespaceOff_NeverTrims()
    {
        var model = CreateAfterEnter();
        model.UpdateOptions(new TextModelUpdateOptions(TrimAutoWhitespace: false));

        model.PushEditOperations(
            [Caret(2, 3)],
            [new IdentifiedEditOperation(new Range(2, 3, 2, 3), "\n")],
            null);

        Assert.Equal("  ", model.GetLineContent(2));
        Assert.Equal("", model.GetLineContent(3));
    }

    [Fact]
    public void ApplyEdits_PlainInsert_LeavesNothingToTrim()
    {
        var model = TestTextModels.Create("  x");

        model.ApplyEdits([TextEdit.Insert(new Position(1, 4), "\n  ")]);

        // The edit was not marked as an auto whitespace edit by ApplyEdits: nothing to trim afterwards.
        model.PushEditOperations(
            [Caret(2, 3)],
            [new IdentifiedEditOperation(new Range(2, 3, 2, 3), "\n")],
            null);
        Assert.Equal("  ", model.GetLineContent(2));
    }

    [Fact]
    public void SetValue_ForgetsTheTrimCandidates()
    {
        var model = CreateAfterEnter();

        model.SetValue("  \n  ");
        model.PushEditOperations(
            [Caret(2, 3)],
            [new IdentifiedEditOperation(new Range(2, 3, 2, 3), "\n")],
            null);

        Assert.Equal("  ", model.GetLineContent(2));
    }
}

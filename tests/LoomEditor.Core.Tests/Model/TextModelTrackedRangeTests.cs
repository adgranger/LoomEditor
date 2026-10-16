using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Model.TestHelpers;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model;

/// <summary>Tests of tracked ranges (<see cref="TextModel.SetTrackedRange"/>, <see cref="TextModel.GetTrackedRange"/>). New tests.</summary>
public class TextModelTrackedRangeTests
{
    private static string Track(TextModel model, Range range, TrackedRangeStickiness stickiness) =>
        model.SetTrackedRange(null, range, stickiness)!;

    private static void Insert(TextModel model, int line, int column, string text) =>
        model.ApplyEdits([TextEdit.Insert(new Position(line, column), text)]);

    [Fact]
    public void SetTrackedRange_AlwaysGrows_InsertBeforeMovesAndInsertAtEndGrows()
    {
        var model = TestTextModels.Create("0123456789");
        string id = Track(model, new Range(1, 3, 1, 5), TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges);

        Insert(model, 1, 1, "xx");
        Assert.Equal(new Range(1, 5, 1, 7), model.GetTrackedRange(id));

        Insert(model, 1, 7, "y");
        Assert.Equal(new Range(1, 5, 1, 8), model.GetTrackedRange(id));
    }

    [Fact]
    public void SetTrackedRange_NeverGrows_InsertAtEndDoesNotGrow()
    {
        var model = TestTextModels.Create("0123456789");
        string id = Track(model, new Range(1, 3, 1, 5), TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges);

        Insert(model, 1, 1, "xx");
        Assert.Equal(new Range(1, 5, 1, 7), model.GetTrackedRange(id));

        Insert(model, 1, 7, "y");
        Assert.Equal(new Range(1, 5, 1, 7), model.GetTrackedRange(id));
    }

    [Fact]
    public void ApplyEdits_DeleteLinesBeforeRange_MovesRangeUp()
    {
        var model = TestTextModels.Create("l1\nl2\nl3\nl4\nl5\nl6");
        string id = Track(model, new Range(5, 1, 5, 2), TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges);

        model.ApplyEdits([TextEdit.Delete(new Range(2, 1, 4, 1))]);

        Assert.Equal(new Range(3, 1, 3, 2), model.GetTrackedRange(id));
    }

    [Fact]
    public void ApplyEdits_DeleteTextContainingRange_CollapsesIt()
    {
        var model = TestTextModels.Create("0123456789");
        string id = Track(model, new Range(1, 3, 1, 5), TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges);

        model.ApplyEdits([TextEdit.Delete(new Range(1, 2, 1, 8))]);

        Assert.Equal(new Range(1, 2, 1, 2), model.GetTrackedRange(id));
    }

    [Fact]
    public void SetTrackedRange_NullRange_DeletesTheRange()
    {
        var model = TestTextModels.Create("0123456789");
        string id = Track(model, new Range(1, 3, 1, 5), TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges);

        string? result = model.SetTrackedRange(id, null, TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges);

        Assert.Null(result);
        Assert.Null(model.GetTrackedRange(id));
    }

    [Fact]
    public void SetTrackedRange_UnknownIdAndNullRange_ReturnsNull()
    {
        var model = TestTextModels.Create("0123456789");

        Assert.Null(model.SetTrackedRange(null, null, TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges));
        Assert.Null(model.SetTrackedRange("unknown", null, TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges));
        Assert.Null(model.GetTrackedRange("unknown"));
    }

    [Fact]
    public void SetTrackedRange_ExistingId_MovesTheRangeAndKeepsTheId()
    {
        var model = TestTextModels.Create("0123456789");
        string id = Track(model, new Range(1, 3, 1, 5), TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges);

        string? sameId = model.SetTrackedRange(id, new Range(1, 6, 1, 9), TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges);

        Assert.Equal(id, sameId);
        Assert.Equal(new Range(1, 6, 1, 9), model.GetTrackedRange(id));
    }

    [Fact]
    public void SetTrackedRange_TwoRanges_HaveDifferentIds()
    {
        var model = TestTextModels.Create("0123456789");

        string first = Track(model, new Range(1, 1, 1, 2), TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges);
        string second = Track(model, new Range(1, 3, 1, 4), TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void SetValue_ForgetsEveryTrackedRange()
    {
        var model = TestTextModels.Create("0123456789");
        string id = Track(model, new Range(1, 3, 1, 5), TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges);

        model.SetValue("abc");

        Assert.Null(model.GetTrackedRange(id));
    }

    [Fact]
    public void Undo_AfterPushEditOperations_PutsTheRangeBack()
    {
        var model = TestTextModels.Create("0123456789");
        string id = Track(model, new Range(1, 3, 1, 5), TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges);

        model.PushEditOperations(null, [TextEdit.Insert(new Position(1, 1), "xx")], null);
        Assert.Equal(new Range(1, 5, 1, 7), model.GetTrackedRange(id));

        model.Undo();
        Assert.Equal(new Range(1, 3, 1, 5), model.GetTrackedRange(id));

        model.Redo();
        Assert.Equal(new Range(1, 5, 1, 7), model.GetTrackedRange(id));
    }

    [Fact]
    public void ValidateRangeRelaxed_InsideSurrogatePair_IsKeptAsIs()
    {
        // columns: a = 1, high surrogate = 2, low surrogate = 3, b = 4
        var model = TestTextModels.Create("a\U0001F600b");
        var inside = new Range(1, 3, 1, 3);

        Assert.Equal(inside, model.ValidateRangeRelaxed(inside));
        Assert.NotEqual(inside, model.ValidateRange(inside));

        string id = Track(model, inside, TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges);
        Assert.Equal(inside, model.GetTrackedRange(id));
    }

    [Fact]
    public void ValidateRangeRelaxed_OutsideTheText_IsClamped()
    {
        var model = TestTextModels.Create("ab\ncd");

        Assert.Equal(new Range(1, 3, 2, 3), model.ValidateRangeRelaxed(new Range(1, 9, 7, 9)));
    }

    [Fact]
    public void SetEol_TrackedRange_KeepsItsLinesAndColumns()
    {
        var model = TestTextModels.Create("first line\nsecond line");
        string id = Track(model, new Range(2, 2, 2, 5), TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges);

        model.SetEol(EndOfLineSequence.CRLF);
        Assert.Equal(new Range(2, 2, 2, 5), model.GetTrackedRange(id));

        model.SetEol(EndOfLineSequence.LF);
        Assert.Equal(new Range(2, 2, 2, 5), model.GetTrackedRange(id));
    }
}

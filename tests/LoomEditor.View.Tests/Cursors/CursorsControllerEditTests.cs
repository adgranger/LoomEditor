using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Events;
using LoomEditor.View.Tests.Cursors.TestHelpers;

namespace LoomEditor.View.Tests.Cursors;

// The Monaco tests come with the operations (P3-25 and later): all tests are new.
public class CursorsControllerEditTests
{
    private static Selection Caret(int line, int column) => Selection.Collapsed(new Position(line, column));

    private static InsertTextCommand Insert(int line, int column, string text) =>
        new(new Range(line, column, line, column), text);

    [Fact]
    public void ExecuteCommands_OneCommandPerCursor_InsertsInOneUndoStepAndMovesTheCursors()
    {
        var f = new CursorsControllerFixture("abc\ndef\nghi");
        f.Controller.SetSelections(f.Collector, "test", [Caret(1, 1), Caret(2, 1), Caret(3, 1)], CursorChangeReason.Explicit);
        f.ResetEvents();

        f.Controller.ExecuteCommands(f.Collector, [Insert(1, 1, "X"), Insert(2, 1, "X"), Insert(3, 1, "X")], "test");

        Assert.Equal("Xabc\nXdef\nXghi", f.Model.GetValue());
        Assert.Equal([Caret(1, 2), Caret(2, 2), Caret(3, 2)], f.Controller.GetSelections());
        Assert.Equal(CursorChangeReason.NotSet, Assert.Single(f.Outgoing).Reason);
        Assert.Single(f.Collector.ViewEvents.OfType<ViewRevealRangeRequestEvent>());

        f.Model.Undo();

        Assert.Equal("abc\ndef\nghi", f.Model.GetValue());
    }

    [Fact]
    public void ExecuteCommand_WithSecondaryCursors_KillsThemFirst()
    {
        var f = new CursorsControllerFixture("abc\ndef");
        f.Controller.SetSelections(f.Collector, "test", [Caret(1, 1), Caret(2, 1)], CursorChangeReason.Explicit);

        f.Controller.ExecuteCommand(f.Collector, Insert(1, 1, "X"), "test");

        Assert.Equal("Xabc\ndef", f.Model.GetValue());
        Assert.Equal([Caret(1, 2)], f.Controller.GetSelections());
    }

    [Fact]
    public void ExecuteEditOperation_PushStackElementBefore_CreatesASeparateUndoStep()
    {
        var f = new CursorsControllerFixture("abc");
        f.Controller.ExecuteCommands(f.Collector, [Insert(1, 1, "X")], "test");

        f.Controller.ExecuteEdit(
            () => f.Controller.ExecuteEditOperation(new EditOperationResult(EditOperationType.Other, [Insert(1, 2, "Y")], true, false)),
            f.Collector,
            "test");
        f.Model.Undo();

        Assert.Equal("Xabc", f.Model.GetValue());
    }

    [Fact]
    public void ExecuteEditOperation_WithFollowUp_RunsTheFollowUpWithTheNewSelections()
    {
        var f = new CursorsControllerFixture("abc");
        IReadOnlyList<Selection>? received = null;

        f.Controller.ExecuteEdit(
            () => f.Controller.ExecuteEditOperation(new EditOperationResult(
                EditOperationType.Other,
                [Insert(1, 1, "X")],
                false,
                false,
                selections =>
                {
                    received = selections;
                    return new EditOperationResult(EditOperationType.Other, [Insert(1, 2, "Z")], false, false);
                })),
            f.Collector,
            "test");

        Assert.Equal([Caret(1, 2)], received);
        Assert.Equal("XZabc", f.Model.GetValue());
        Assert.Equal([Caret(1, 3)], f.Controller.GetSelections());
    }

    [Fact]
    public void ExecuteEdits_ComputerReturnsSelections_CursorIsNotRecoveredFromMarkers()
    {
        var f = new CursorsControllerFixture("abcdef");
        f.Controller.SetSelections(f.Collector, "test", [Caret(1, 6)], CursorChangeReason.Explicit);
        f.ResetEvents();

        f.Controller.ExecuteEdits(
            f.Collector,
            "test",
            [new IdentifiedEditOperation(new Range(1, 1, 1, 1), "XX")],
            _ => [Caret(1, 5)]);

        Assert.Equal("XXabcdef", f.Model.GetValue());
        Assert.Equal([Caret(1, 5)], f.Controller.GetSelections());
        Assert.Equal(CursorChangeReason.NotSet, Assert.Single(f.Outgoing).Reason);
    }

    [Fact]
    public void ExecuteCommands_ReadOnly_ChangesNothingAndEmitsNothing()
    {
        var f = new CursorsControllerFixture("abc", EditorCursorOptions.Default with { ReadOnly = true });

        f.Controller.ExecuteCommands(f.Collector, [Insert(1, 1, "X")], "test");

        Assert.Equal("abc", f.Model.GetValue());
        Assert.Empty(f.Collector.ViewEvents);
        Assert.Empty(f.Collector.OutgoingEvents);
    }

    [Fact]
    public void ExecuteCommands_CommandThrows_PropagatesAndTheCursorsTrackAgain()
    {
        var f = new CursorsControllerFixture("abcdef");
        f.Controller.SetSelections(f.Collector, "test", [Caret(1, 3)], CursorChangeReason.Explicit);

        Assert.Throws<InvalidOperationException>(() => f.Controller.ExecuteCommands(f.Collector, [new ThrowingCursorStateCommand()], "test"));

        f.Model.PushEditOperations(null, [new TextEdit(new Range(1, 1, 1, 1), "XX")], null);
        Assert.Equal([Caret(1, 5)], f.Controller.GetSelections());
    }
}

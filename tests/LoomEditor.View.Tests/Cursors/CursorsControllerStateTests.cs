using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Cursors.TestHelpers;

namespace LoomEditor.View.Tests.Cursors;

// The Monaco tests that cover this file (cursor.test.ts) are ported through the view model in P3-27: all tests are new.
public class CursorsControllerStateTests
{
    private static Selection Sel(int line, int startColumn, int endColumn) =>
        new(new Position(line, startColumn), new Position(line, endColumn));

    private static Selection Caret(int line, int column) => Selection.Collapsed(new Position(line, column));


    [Fact]
    public void SetStates_TwoStates_SetsSelectionsAndEmitsOneEventOfEachKind()
    {
        var f = new CursorsControllerFixture("abcdefgh");

        f.Controller.SetSelections(f.Collector, "test", [Sel(1, 1, 3), Caret(1, 6)], CursorChangeReason.Explicit);

        Assert.Equal([Sel(1, 1, 3), Caret(1, 6)], f.Controller.GetSelections());
        CursorStateChangedEvent outgoing = Assert.Single(f.Outgoing);
        Assert.Equal("test", outgoing.Source);
        Assert.Equal(CursorChangeReason.Explicit, outgoing.Reason);
        Assert.Equal([Caret(1, 1)], outgoing.OldSelections);
        Assert.Single(f.ViewState);
    }

    [Fact]
    public void SetStates_SameStatesAgain_EmitsNothing()
    {
        var f = new CursorsControllerFixture("abcdefgh");
        f.Controller.SetSelections(f.Collector, "test", [Sel(1, 1, 3), Caret(1, 6)], CursorChangeReason.Explicit);
        f.ResetEvents();

        bool changed = f.Controller.SetStates(
            f.Collector, "test", CursorChangeReason.Explicit, CursorState.FromModelSelections([Sel(1, 1, 3), Caret(1, 6)]));

        Assert.False(changed);
        Assert.Empty(f.Collector.ViewEvents);
        Assert.Empty(f.Collector.OutgoingEvents);
    }

    [Fact]
    public void SetStates_EmptySource_ReportsKeyboard()
    {
        var f = new CursorsControllerFixture("abcdefgh");

        f.Controller.SetSelections(f.Collector, "", [Caret(1, 4)], CursorChangeReason.Explicit);

        Assert.Equal("keyboard", Assert.Single(f.Outgoing).Source);
    }

    [Fact]
    public void SetStates_MoreStatesThanTheLimit_KeepsTheLimitAndReportsIt()
    {
        var options = EditorCursorOptions.Default with { MultiCursorLimit = 3 };
        var f = new CursorsControllerFixture("abcdefgh", options);

        f.Controller.SetSelections(f.Collector, "test", [Caret(1, 1), Caret(1, 3), Caret(1, 5), Caret(1, 7)], CursorChangeReason.Explicit);

        Assert.Equal([Caret(1, 1), Caret(1, 3), Caret(1, 5)], f.Controller.GetSelections());
        Assert.True(Assert.Single(f.Outgoing).ReachedMaxCursorCount);
    }

    [Fact]
    public void OnModelContentChanged_SetValue_ResetsToOneCursorAtTheStart()
    {
        var f = new CursorsControllerFixture("abcdefgh");
        f.Controller.SetSelections(f.Collector, "test", [Sel(1, 2, 5), Caret(1, 7)], CursorChangeReason.Explicit);
        f.ResetEvents();

        f.Model.SetValue("new text");

        Assert.Equal([Caret(1, 1)], f.Controller.GetSelections());
        CursorStateChangedEvent outgoing = Assert.Single(f.Outgoing);
        Assert.Equal(CursorChangeReason.ContentFlush, outgoing.Reason);
        Assert.Null(outgoing.OldSelections);
    }

    [Fact]
    public void OnModelContentChanged_EditByAnotherParty_MovesTheCursorWithTheText()
    {
        var f = new CursorsControllerFixture("abcdef");
        f.Controller.SetSelections(f.Collector, "test", [Caret(1, 3)], CursorChangeReason.Explicit);
        f.ResetEvents();

        f.Model.PushEditOperations(null, [new TextEdit(new Range(1, 1, 1, 1), "XX")], null);

        Assert.Equal([Caret(1, 5)], f.Controller.GetSelections());
        Assert.Equal(CursorChangeReason.RecoverFromMarkers, Assert.Single(f.Outgoing).Reason);
    }

    [Fact]
    public void OnModelContentChanged_UndoWithFocus_RestoresTheSelectionsAndRequestsAReveal()
    {
        var f = new CursorsControllerFixture("abcdef");
        f.Controller.SetHasFocus(true);
        f.Controller.SetSelections(f.Collector, "test", [Caret(1, 2)], CursorChangeReason.Explicit);
        f.Model.PushEditOperations([Caret(1, 2)], [new TextEdit(new Range(1, 1, 1, 1), "XX")], _ => [Caret(1, 4)]);
        f.ResetEvents();

        f.Model.Undo();

        Assert.Equal([Caret(1, 2)], f.Controller.GetSelections());
        Assert.Equal(CursorChangeReason.Undo, Assert.Single(f.Outgoing).Reason);
        ViewRevealRangeRequestEvent reveal = Assert.Single(f.Collector.ViewEvents.OfType<ViewRevealRangeRequestEvent>());
        Assert.Equal(ScrollType.Smooth, reveal.ScrollType);
    }

    [Fact]
    public void OnModelContentChanged_UndoWithoutFocus_RecoversFromMarkers()
    {
        var f = new CursorsControllerFixture("abcdef");
        f.Controller.SetSelections(f.Collector, "test", [Caret(1, 2)], CursorChangeReason.Explicit);
        f.Model.PushEditOperations([Caret(1, 2)], [new TextEdit(new Range(1, 1, 1, 1), "XX")], _ => [Caret(1, 4)]);
        f.ResetEvents();

        f.Model.Undo();

        Assert.Equal(CursorChangeReason.RecoverFromMarkers, Assert.Single(f.Outgoing).Reason);
        Assert.Empty(f.Collector.ViewEvents.OfType<ViewRevealRangeRequestEvent>());
    }

    [Fact]
    public void RestoreState_AfterSaveState_GivesBackTheSameSelections()
    {
        var f = new CursorsControllerFixture("abcdefgh\nijklmnop");
        f.Controller.SetSelections(f.Collector, "test", [Sel(1, 2, 5), new Selection(new Position(2, 6), new Position(2, 3))], CursorChangeReason.Explicit);
        List<SavedCursorState> saved = f.Controller.SaveState();
        f.Controller.SetSelections(f.Collector, "test", [Caret(1, 1)], CursorChangeReason.Explicit);

        f.Controller.RestoreState(f.Collector, saved);

        Assert.Equal([Sel(1, 2, 5), new Selection(new Position(2, 6), new Position(2, 3))], f.Controller.GetSelections());
    }

    [Fact]
    public void RestoreState_MissingOrZeroValues_UseTheDefaults()
    {
        var f = new CursorsControllerFixture("abcdefgh");

        f.Controller.RestoreState(f.Collector, [new SavedCursorState(false, null, new SavedCursorPosition(1, 0))]);

        Assert.Equal([Caret(1, 1)], f.Controller.GetSelections());
    }
}

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;

namespace LoomEditor.View.Tests.Cursors;

// Monaco has no test for SingleCursorState or CursorState: all tests are new.
public class SingleCursorStateTests
{
    private static SingleCursorState State(Range start, SelectionStartKind kind, Position position, int leftover = 0, int startLeftover = 0) =>
        new(start, kind, startLeftover, position, leftover);

    [Fact]
    public void Selection_PositionBeforeEmptyStart_IsStartToPosition()
    {
        var state = State(new Range(1, 5, 1, 5), SelectionStartKind.Simple, new Position(1, 2));

        Assert.Equal(new Selection(new Position(1, 5), new Position(1, 2)), state.Selection);
    }

    [Fact]
    public void Selection_PositionBeforeWordStart_KeepsTheEndOfTheStartRange()
    {
        var state = State(new Range(1, 3, 1, 7), SelectionStartKind.Word, new Position(1, 1));

        Assert.Equal(new Selection(new Position(1, 7), new Position(1, 1)), state.Selection);
    }

    [Fact]
    public void Selection_PositionAfterWordStart_KeepsTheStartOfTheStartRange()
    {
        var state = State(new Range(1, 3, 1, 7), SelectionStartKind.Word, new Position(1, 9));

        Assert.Equal(new Selection(new Position(1, 3), new Position(1, 9)), state.Selection);
    }

    [Fact]
    public void Move_InSelectionMode_KeepsSelectionStartAndKind()
    {
        var state = State(new Range(1, 3, 1, 7), SelectionStartKind.Word, new Position(1, 9), leftover: 2, startLeftover: 1);

        SingleCursorState moved = state.Move(inSelectionMode: true, 2, 4, 6);

        Assert.Equal(new Range(1, 3, 1, 7), moved.SelectionStart);
        Assert.Equal(SelectionStartKind.Word, moved.SelectionStartKind);
        Assert.Equal(1, moved.SelectionStartLeftoverVisibleColumns);
        Assert.Equal(new Position(2, 4), moved.Position);
        Assert.Equal(6, moved.LeftoverVisibleColumns);
    }

    [Fact]
    public void Move_NotInSelectionMode_CollapsesWithKindSimple()
    {
        var state = State(new Range(1, 3, 1, 7), SelectionStartKind.Word, new Position(1, 9));

        SingleCursorState moved = state.Move(inSelectionMode: false, 2, 4, 6);

        Assert.Equal(new Range(2, 4, 2, 4), moved.SelectionStart);
        Assert.Equal(SelectionStartKind.Simple, moved.SelectionStartKind);
        Assert.Equal(6, moved.SelectionStartLeftoverVisibleColumns);
        Assert.Equal(new Position(2, 4), moved.Position);
        Assert.False(moved.HasSelection());
    }

    [Fact]
    public void Equals_DifferentLeftoverVisibleColumns_AreNotEqual()
    {
        var a = State(new Range(1, 3, 1, 3), SelectionStartKind.Simple, new Position(1, 3), leftover: 0);
        var b = State(new Range(1, 3, 1, 3), SelectionStartKind.Simple, new Position(1, 3), leftover: 1);

        Assert.False(a.Equals(b));
        Assert.True(a.Equals(State(new Range(1, 3, 1, 3), SelectionStartKind.Simple, new Position(1, 3))));
    }

    [Fact]
    public void HasSelection_NonEmptySelectionStartRange_IsTrue()
    {
        // With a non-empty start range the selection is never empty either: a position at the start of the range
        // gives the selection (end of range → position). HasSelection still reads both, like Monaco.
        var state = State(new Range(1, 3, 1, 7), SelectionStartKind.Word, new Position(1, 3));

        Assert.False(state.Selection.IsEmpty);
        Assert.True(state.HasSelection());
        Assert.False(State(new Range(1, 3, 1, 3), SelectionStartKind.Simple, new Position(1, 3)).HasSelection());
    }

    [Fact]
    public void CursorStateFromModelSelections_KeepsOrderAndDirection()
    {
        var forward = new Selection(new Position(1, 1), new Position(1, 4));
        var backward = new Selection(new Position(3, 5), new Position(2, 2));

        PartialModelCursorState[] states = CursorState.FromModelSelections([forward, backward]);

        Assert.Equal(2, states.Length);
        Assert.Null(states[0].ViewState);
        Assert.Equal(forward, states[0].ModelState.Selection);
        Assert.Equal(backward, states[1].ModelState.Selection);
        Assert.Equal(new Range(3, 5, 3, 5), states[1].ModelState.SelectionStart);
        Assert.Equal(new Position(2, 2), states[1].ModelState.Position);
        Assert.Equal(SelectionStartKind.Simple, states[1].ModelState.SelectionStartKind);
    }

    [Fact]
    public void CursorStateEquals_ComparesBothStates()
    {
        var model = State(new Range(1, 1, 1, 1), SelectionStartKind.Simple, new Position(1, 1));
        var view = State(new Range(1, 1, 1, 1), SelectionStartKind.Simple, new Position(1, 1));
        var otherView = State(new Range(1, 1, 1, 1), SelectionStartKind.Simple, new Position(1, 1), leftover: 3);

        Assert.True(new CursorState(model, view).Equals(new CursorState(model, view)));
        Assert.False(new CursorState(model, view).Equals(new CursorState(model, otherView)));
    }

    [Theory]
    [InlineData('\'', true)]
    [InlineData('"', true)]
    [InlineData('`', true)]
    [InlineData('a', false)]
    public void IsQuote_ReturnsTrueForTheThreeQuotes(char ch, bool expected)
    {
        Assert.Equal(expected, CursorCommon.IsQuote(ch));
    }
}

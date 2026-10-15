using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Tests.Cursors.TestHelpers;

namespace LoomEditor.View.Tests.Cursors;

// No Monaco test targets Cursor directly (it is covered by cursor.test.ts): all tests are new.
public class CursorTests
{
    private static SingleCursorState State(Selection selection) =>
        CursorState.FromModelSelection(selection).ModelState;

    [Fact]
    public void Constructor_NewCursor_IsAtTheStartInBothStates()
    {
        var context = CursorTestContext.Create("abcdef");

        var cursor = new Cursor(context);

        Assert.Equal(new Position(1, 1), cursor.ModelState.Position);
        Assert.Equal(new Position(1, 1), cursor.ViewState.Position);
        Assert.False(cursor.ModelState.HasSelection());
    }

    [Fact]
    public void ReadSelectionFromMarkers_AfterAnInsertBeforeTheSelection_FollowsTheText()
    {
        var context = CursorTestContext.Create("abcdef");
        var cursor = new Cursor(context);
        var selection = new Selection(new Position(1, 4), new Position(1, 2));
        cursor.SetState(context, State(selection), null);

        context.Model.PushEditOperations(null, [TextEdit.Insert(new Position(1, 1), "xx")], null);

        Selection read = cursor.ReadSelectionFromMarkers(context);
        Assert.Equal(new Selection(new Position(1, 6), new Position(1, 4)), read);
        Assert.Equal(SelectionDirection.RightToLeft, read.Direction);
    }

    [Fact]
    public void ReadSelectionFromMarkers_EmptyCursorWhoseSurroundingTextIsReplaced_StaysEmpty()
    {
        var context = CursorTestContext.Create("abcdef");
        var cursor = new Cursor(context);
        cursor.SetState(context, State(Selection.Collapsed(new Position(1, 3))), null);

        // Someone replaces "bc" (1,2)-(1,4) by "zz" around the cursor.
        context.Model.PushEditOperations(null, [TextEdit.Replace(new Range(1, 2, 1, 4), "zz")], null);

        Selection read = cursor.ReadSelectionFromMarkers(context);
        Assert.True(read.IsEmpty);
    }

    [Fact]
    public void SetState_ModelStateOutsideTheModel_IsClampedToAValidPosition()
    {
        var context = CursorTestContext.Create("abc\nde");
        var cursor = new Cursor(context);
        var outside = new SingleCursorState(new Range(9, 9, 9, 9), SelectionStartKind.Simple, 5, new Position(9, 9), 5);

        cursor.SetState(context, outside, null);

        Assert.Equal(new Position(2, 3), cursor.ModelState.Position);
        Assert.Equal(new Range(2, 3, 2, 3), cursor.ModelState.SelectionStart);
        Assert.Equal(0, cursor.ModelState.LeftoverVisibleColumns);
        Assert.Equal(new Position(2, 3), cursor.ViewState.Position);
    }

    [Fact]
    public void SetState_ViewStateOnly_ComputesTheModelState()
    {
        var context = CursorTestContext.Create("abc\nde");
        var cursor = new Cursor(context);
        var viewState = State(Selection.Collapsed(new Position(2, 2)));

        cursor.SetState(context, null, viewState);

        Assert.Equal(new Position(2, 2), cursor.ModelState.Position);
    }

    [Fact]
    public void StopTrackingSelection_ReleasesTheTrackedRange()
    {
        var context = CursorTestContext.Create("abc");
        var cursor = new Cursor(context);
        Assert.Equal(1, context.Model.TrackedRangeCount);

        cursor.StopTrackingSelection(context);
        Assert.Equal(0, context.Model.TrackedRangeCount);

        cursor.StartTrackingSelection(context);
        Assert.Equal(1, context.Model.TrackedRangeCount);

        cursor.Dispose(context);
        Assert.Equal(0, context.Model.TrackedRangeCount);
    }
}

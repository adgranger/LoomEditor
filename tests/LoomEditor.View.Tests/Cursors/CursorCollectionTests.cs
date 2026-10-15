using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Tests.Cursors.TestHelpers;

namespace LoomEditor.View.Tests.Cursors;

// No Monaco test targets CursorCollection directly (it is covered by cursor.test.ts): all tests are new.
public class CursorCollectionTests
{
    private static Selection Sel(int line, int startColumn, int endColumn) =>
        new(new Position(line, startColumn), new Position(line, endColumn));

    private static Selection Caret(int line, int column) => Selection.Collapsed(new Position(line, column));

    [Fact]
    public void SetSelections_ThreeSelections_CreatesThreeCursorsPrimaryFirst()
    {
        var collection = new CursorCollection(CursorTestContext.Create("abcdefgh"));

        collection.SetSelections([Sel(1, 1, 2), Sel(1, 3, 4), Sel(1, 5, 6)]);

        Assert.Equal([Sel(1, 1, 2), Sel(1, 3, 4), Sel(1, 5, 6)], collection.GetSelections());
        Assert.Equal(Sel(1, 1, 2), collection.GetPrimaryCursor().ModelState.Selection);

        collection.KillSecondaryCursors();

        Assert.Equal([Sel(1, 1, 2)], collection.GetSelections());
    }

    [Fact]
    public void GetLastAddedCursorIndex_AfterAddingTwoSecondaryCursors_IsTwo()
    {
        var collection = new CursorCollection(CursorTestContext.Create("abcdefgh"));

        Assert.Equal(0, collection.GetLastAddedCursorIndex());

        collection.SetSelections([Caret(1, 1), Caret(1, 3), Caret(1, 5)]);

        Assert.Equal(2, collection.GetLastAddedCursorIndex());
    }

    [Fact]
    public void Normalize_OverlappingSelections_AreMerged()
    {
        var collection = new CursorCollection(CursorTestContext.Create("abcdefghij"));
        collection.SetSelections([Sel(1, 1, 5), Sel(1, 3, 8)]);

        collection.Normalize();

        Assert.Equal([Sel(1, 1, 8)], collection.GetSelections());
    }

    [Fact]
    public void Normalize_MultiCursorMergeOverlappingOff_DoesNotMerge()
    {
        var options = EditorCursorOptions.Default with { MultiCursorMergeOverlapping = false };
        var collection = new CursorCollection(CursorTestContext.Create("abcdefghij", options));
        collection.SetSelections([Sel(1, 1, 5), Sel(1, 3, 8)]);

        collection.Normalize();

        Assert.Equal(2, collection.GetSelections().Count);
    }

    [Fact]
    public void Normalize_TwoEmptyCursorsAtTheSamePosition_BecomeOne()
    {
        var collection = new CursorCollection(CursorTestContext.Create("abcdef"));
        collection.SetSelections([Caret(1, 3), Caret(1, 3)]);

        collection.Normalize();

        Assert.Equal([Caret(1, 3)], collection.GetSelections());
    }

    [Fact]
    public void Normalize_TouchingNonEmptySelections_AreKept()
    {
        var collection = new CursorCollection(CursorTestContext.Create("abcdefghij"));
        collection.SetSelections([Sel(1, 1, 4), Sel(1, 4, 8)]);

        collection.Normalize();

        Assert.Equal(2, collection.GetSelections().Count);
    }

    [Fact]
    public void Normalize_EmptyCursorTouchingASelection_IsMerged()
    {
        var collection = new CursorCollection(CursorTestContext.Create("abcdefghij"));
        collection.SetSelections([Sel(1, 1, 4), Caret(1, 4)]);

        collection.Normalize();

        Assert.Equal([Sel(1, 1, 4)], collection.GetSelections());
    }

    [Fact]
    public void GetTopMostAndBottomMostViewPosition_ReturnTheExtremePositions()
    {
        var collection = new CursorCollection(CursorTestContext.Create("abc\ndef\nghi"));
        collection.SetSelections([Caret(2, 2), Caret(1, 3), Caret(3, 1)]);

        Assert.Equal(new Position(1, 3), collection.GetTopMostViewPosition());
        Assert.Equal(new Position(3, 1), collection.GetBottomMostViewPosition());
    }

    [Fact]
    public void Dispose_ReleasesAllTrackedRanges()
    {
        var context = CursorTestContext.Create("abcdef");
        var collection = new CursorCollection(context);
        collection.SetSelections([Caret(1, 1), Caret(1, 3)]);
        Assert.Equal(2, context.Model.TrackedRangeCount);

        collection.Dispose();

        Assert.Equal(0, context.Model.TrackedRangeCount);
    }
}

using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

// Ported from microsoft/vscode: src/vs/editor/test/common/viewLayout/lineHeights.test.ts
// Suite: 'Editor ViewLayout - LineHeightsManager (auto-commit on read)'
public class LineHeightsManagerAutoCommitTests
{
    private static LineHeightsManager Create() => new(10, []);

    // Monaco: 'read after single insert without commit'
    [Fact]
    public void Read_AfterSingleInsert_SeesIt()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 3, 3, 20);

        Assert.Equal(10, manager.HeightForLineNumber(1));
        Assert.Equal(20, manager.HeightForLineNumber(3));
        Assert.Equal(10, manager.HeightForLineNumber(4));
        Assert.Equal(40, manager.GetAccumulatedLineHeightsIncludingLineNumber(3));
        Assert.Equal(50, manager.GetAccumulatedLineHeightsIncludingLineNumber(4));
    }

    // Monaco: 'read after multiple inserts without commit'
    [Fact]
    public void Read_AfterMultipleInserts_SeesThem()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 2, 2, 15);
        manager.InsertOrChangeCustomLineHeight("dec2", 4, 4, 25);

        Assert.Equal(15, manager.HeightForLineNumber(2));
        Assert.Equal(10, manager.HeightForLineNumber(3));
        Assert.Equal(25, manager.HeightForLineNumber(4));
        Assert.Equal(60, manager.GetAccumulatedLineHeightsIncludingLineNumber(4));
        Assert.Equal(70, manager.GetAccumulatedLineHeightsIncludingLineNumber(5));
    }

    // Monaco: 'read after remove without commit'
    [Fact]
    public void Read_AfterRemove_SeesDefaultHeight()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 3, 3, 20);
        Assert.Equal(20, manager.HeightForLineNumber(3));

        manager.RemoveCustomLineHeight("dec1");

        Assert.Equal(10, manager.HeightForLineNumber(3));
        Assert.Equal(30, manager.GetAccumulatedLineHeightsIncludingLineNumber(3));
    }

    // Monaco: 'insert then remove same decoration without commit'
    [Fact]
    public void Read_AfterInsertThenRemoveOfSameDecoration_SeesDefaultHeight()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 3, 3, 20);
        manager.RemoveCustomLineHeight("dec1");

        Assert.Equal(10, manager.HeightForLineNumber(3));
        Assert.Equal(30, manager.GetAccumulatedLineHeightsIncludingLineNumber(3));
    }

    // Monaco: 'insert same decoration ID twice without commit replaces first'
    [Fact]
    public void Read_AfterInsertingSameDecorationTwice_SecondReplacesFirst()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 3, 3, 20);
        manager.InsertOrChangeCustomLineHeight("dec1", 5, 5, 30);

        Assert.Equal(10, manager.HeightForLineNumber(3));
        Assert.Equal(30, manager.HeightForLineNumber(5));
        Assert.Equal(70, manager.GetAccumulatedLineHeightsIncludingLineNumber(5));
    }

    // Monaco: 'interleaved callers: remove must cancel queued inserts before first flush'
    [Fact]
    public void Commit_RemoveAfterQueuedInsertThenLinesInserted_KeepsDecorationRemoved()
    {
        var manager = Create();

        manager.InsertOrChangeCustomLineHeight("decA", 3, 3, 20);
        manager.InsertOrChangeCustomLineHeight("decB", 4, 4, 30);
        manager.RemoveCustomLineHeight("decA");
        manager.OnLinesInserted(1, 1);

        Assert.Equal(10, manager.HeightForLineNumber(4));
        Assert.Equal(30, manager.HeightForLineNumber(5));
        Assert.Equal(70, manager.GetAccumulatedLineHeightsIncludingLineNumber(5));
    }

    // Monaco: 'interleaved callers: remove must cancel queued inserts before delete flush'
    [Fact]
    public void Commit_RemoveAfterQueuedInsertThenLinesDeleted_KeepsDecorationRemoved()
    {
        var manager = Create();

        manager.InsertOrChangeCustomLineHeight("decA", 3, 3, 20);
        manager.InsertOrChangeCustomLineHeight("decB", 5, 5, 30);
        manager.RemoveCustomLineHeight("decA");
        manager.OnLinesDeleted(1, 1);

        Assert.Equal(10, manager.HeightForLineNumber(2));
        Assert.Equal(10, manager.HeightForLineNumber(3));
        Assert.Equal(30, manager.HeightForLineNumber(4));
        Assert.Equal(60, manager.GetAccumulatedLineHeightsIncludingLineNumber(4));
    }

    // Monaco: 'interleaved: insert, insert, onLinesInserted, onLinesDeleted, remove, remove, insert, insert, read'
    [Fact]
    public void Commit_InterleavedInsertsRemovesAndLineChanges_IsCorrect()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 2, 2, 20);
        manager.InsertOrChangeCustomLineHeight("dec2", 5, 5, 30);
        manager.OnLinesInserted(3, 4);
        manager.OnLinesDeleted(1, 1);
        manager.RemoveCustomLineHeight("dec1");
        manager.RemoveCustomLineHeight("dec2");
        manager.InsertOrChangeCustomLineHeight("dec3", 3, 3, 40);
        manager.InsertOrChangeCustomLineHeight("dec4", 5, 5, 50);

        Assert.Equal(10, manager.HeightForLineNumber(1));
        Assert.Equal(40, manager.HeightForLineNumber(3));
        Assert.Equal(10, manager.HeightForLineNumber(4));
        Assert.Equal(50, manager.HeightForLineNumber(5));
        Assert.Equal(10, manager.HeightForLineNumber(6));
    }

    // Monaco: 'interleaved: insert, onLinesInserted, remove, read'
    [Fact]
    public void Commit_InsertThenLinesInsertedThenRemove_SeesDefaultHeight()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 3, 3, 20);
        manager.OnLinesInserted(1, 1);
        manager.RemoveCustomLineHeight("dec1");

        Assert.Equal(10, manager.HeightForLineNumber(3));
        Assert.Equal(10, manager.HeightForLineNumber(4));
    }

    // Monaco: 'interleaved: onLinesDeleted, insert, read'
    [Fact]
    public void Commit_LinesDeletedThenInsert_IsCorrect()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 5, 5, 20);
        manager.OnLinesDeleted(1, 2);
        manager.InsertOrChangeCustomLineHeight("dec2", 1, 1, 30);

        Assert.Equal(30, manager.HeightForLineNumber(1));
        Assert.Equal(10, manager.HeightForLineNumber(2));
        Assert.Equal(20, manager.HeightForLineNumber(3));
    }

    // Monaco: 'interleaved: insert, onLinesDeleted, insert, read'
    [Fact]
    public void Commit_InsertThenLinesDeletedThenInsert_IsCorrect()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 3, 3, 20);
        manager.OnLinesDeleted(1, 1);
        manager.InsertOrChangeCustomLineHeight("dec2", 5, 5, 30);

        Assert.Equal(10, manager.HeightForLineNumber(1));
        Assert.Equal(20, manager.HeightForLineNumber(2));
        Assert.Equal(30, manager.HeightForLineNumber(5));
        Assert.Equal(80, manager.GetAccumulatedLineHeightsIncludingLineNumber(5));
    }

    // Monaco: 'onLinesInserted then onLinesDeleted without reads between'
    [Fact]
    public void Commit_LinesInsertedThenLinesDeleted_IsCorrect()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 3, 3, 20);
        manager.OnLinesInserted(1, 2);
        manager.OnLinesDeleted(1, 1);

        Assert.Equal(20, manager.HeightForLineNumber(4));
        Assert.Equal(10, manager.HeightForLineNumber(3));
        Assert.Equal(50, manager.GetAccumulatedLineHeightsIncludingLineNumber(4));
    }

    // Monaco: 'multiple onLinesInserted without reads between'
    [Fact]
    public void Commit_MultipleLinesInserted_ShiftsCumulatively()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 3, 3, 20);
        manager.OnLinesInserted(1, 1);
        manager.OnLinesInserted(1, 1);

        Assert.Equal(20, manager.HeightForLineNumber(5));
        Assert.Equal(10, manager.HeightForLineNumber(3));
        Assert.Equal(10, manager.HeightForLineNumber(4));
    }

    // Monaco: 'multiple onLinesDeleted without reads between'
    [Fact]
    public void Commit_MultipleLinesDeleted_ShiftsCumulatively()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 10, 10, 20);
        manager.OnLinesDeleted(1, 2);
        manager.OnLinesDeleted(1, 2);

        Assert.Equal(20, manager.HeightForLineNumber(6));
        Assert.Equal(10, manager.HeightForLineNumber(7));
    }

    // Monaco: 'pending insert then onLinesDeleted affecting that line'
    [Fact]
    public void Commit_PendingInsertThenLinesDeletedOnThatLine_CollapsesToFromLine()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 3, 3, 20);
        manager.OnLinesDeleted(3, 3);

        Assert.Equal(20, manager.HeightForLineNumber(3));
    }

    // Monaco: 'pending insert then onLinesInserted shifting that line'
    [Fact]
    public void Commit_PendingInsertThenLinesInsertedBefore_ShiftsIt()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 3, 3, 20);
        manager.OnLinesInserted(1, 2);

        Assert.Equal(10, manager.HeightForLineNumber(3));
        Assert.Equal(20, manager.HeightForLineNumber(5));
    }

    // Monaco: 'accumulated heights correct after interleaved ops without commit'
    [Fact]
    public void GetAccumulatedLineHeights_AfterPendingInserts_IsCorrect()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 2, 2, 15);
        manager.InsertOrChangeCustomLineHeight("dec2", 4, 4, 25);

        Assert.Equal(10, manager.GetAccumulatedLineHeightsIncludingLineNumber(1));
        Assert.Equal(25, manager.GetAccumulatedLineHeightsIncludingLineNumber(2));
        Assert.Equal(35, manager.GetAccumulatedLineHeightsIncludingLineNumber(3));
        Assert.Equal(60, manager.GetAccumulatedLineHeightsIncludingLineNumber(4));
        Assert.Equal(70, manager.GetAccumulatedLineHeightsIncludingLineNumber(5));
    }

    // Monaco: 'constructor with initial data works without explicit commit'
    [Fact]
    public void Constructor_WithInitialData_ReadsWithoutExplicitCommit()
    {
        CustomLineHeightData[] data =
        [
            new("dec1", 2, 4, 20),
            new("dec2", 6, 6, 30),
        ];
        var manager = new LineHeightsManager(10, data);

        Assert.Equal(10, manager.HeightForLineNumber(1));
        Assert.Equal(20, manager.HeightForLineNumber(2));
        Assert.Equal(20, manager.HeightForLineNumber(3));
        Assert.Equal(20, manager.HeightForLineNumber(4));
        Assert.Equal(10, manager.HeightForLineNumber(5));
        Assert.Equal(30, manager.HeightForLineNumber(6));
        Assert.Equal(110, manager.GetAccumulatedLineHeightsIncludingLineNumber(6));
    }

    // Monaco: 'deleting line 2 with lineHeightsRemoved re-adding at line 1 moves special line to line 1'
    [Fact]
    public void Commit_LinesDeletedThenReinsertAtLineOne_MovesSpecialLine()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 2, 2, 20);
        Assert.Equal(20, manager.HeightForLineNumber(2));
        manager.OnLinesDeleted(2, 2);
        manager.InsertOrChangeCustomLineHeight("dec1", 1, 1, 20);

        Assert.Equal(20, manager.HeightForLineNumber(1));
    }
}

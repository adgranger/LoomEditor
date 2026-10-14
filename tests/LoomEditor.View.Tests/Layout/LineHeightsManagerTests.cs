using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

// Ported from microsoft/vscode: src/vs/editor/test/common/viewLayout/lineHeights.test.ts
// Suite: 'Editor ViewLayout - LineHeightsManager'
public class LineHeightsManagerTests
{
    private static LineHeightsManager Create(int defaultLineHeight = 10) => new(defaultLineHeight, []);

    // Monaco: 'default line height is used when no custom heights exist'
    [Fact]
    public void DefaultLineHeight_NoCustomHeights_IsUsed()
    {
        var manager = Create();

        Assert.Equal(10, manager.HeightForLineNumber(1));
        Assert.Equal(10, manager.HeightForLineNumber(5));
        Assert.Equal(10, manager.HeightForLineNumber(100));

        Assert.Equal(10, manager.GetAccumulatedLineHeightsIncludingLineNumber(1));
        Assert.Equal(50, manager.GetAccumulatedLineHeightsIncludingLineNumber(5));
        Assert.Equal(100, manager.GetAccumulatedLineHeightsIncludingLineNumber(10));
    }

    // Monaco: 'can change default line height'
    [Fact]
    public void DefaultLineHeight_Changed_IsUsed()
    {
        var manager = Create();
        manager.DefaultLineHeight = 20;

        Assert.Equal(20, manager.HeightForLineNumber(1));
        Assert.Equal(20, manager.HeightForLineNumber(5));

        Assert.Equal(20, manager.GetAccumulatedLineHeightsIncludingLineNumber(1));
        Assert.Equal(100, manager.GetAccumulatedLineHeightsIncludingLineNumber(5));
    }

    // Monaco: 'can add single custom line height'
    [Fact]
    public void Insert_SingleCustomLineHeight_IsApplied()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 3, 3, 20);

        Assert.Equal(10, manager.HeightForLineNumber(1));
        Assert.Equal(10, manager.HeightForLineNumber(2));
        Assert.Equal(20, manager.HeightForLineNumber(3));
        Assert.Equal(10, manager.HeightForLineNumber(4));

        Assert.Equal(10, manager.GetAccumulatedLineHeightsIncludingLineNumber(1));
        Assert.Equal(20, manager.GetAccumulatedLineHeightsIncludingLineNumber(2));
        Assert.Equal(40, manager.GetAccumulatedLineHeightsIncludingLineNumber(3));
        Assert.Equal(50, manager.GetAccumulatedLineHeightsIncludingLineNumber(4));
    }

    // Monaco: 'can add multiple custom line heights'
    [Fact]
    public void Insert_MultipleCustomLineHeights_AreApplied()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 2, 2, 15);
        manager.InsertOrChangeCustomLineHeight("dec2", 4, 4, 25);

        Assert.Equal(10, manager.HeightForLineNumber(1));
        Assert.Equal(15, manager.HeightForLineNumber(2));
        Assert.Equal(10, manager.HeightForLineNumber(3));
        Assert.Equal(25, manager.HeightForLineNumber(4));
        Assert.Equal(10, manager.HeightForLineNumber(5));

        Assert.Equal(10, manager.GetAccumulatedLineHeightsIncludingLineNumber(1));
        Assert.Equal(25, manager.GetAccumulatedLineHeightsIncludingLineNumber(2));
        Assert.Equal(35, manager.GetAccumulatedLineHeightsIncludingLineNumber(3));
        Assert.Equal(60, manager.GetAccumulatedLineHeightsIncludingLineNumber(4));
        Assert.Equal(70, manager.GetAccumulatedLineHeightsIncludingLineNumber(5));
    }

    // Monaco: 'can add range of custom line heights'
    [Fact]
    public void Insert_RangeOfCustomLineHeights_IsApplied()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 2, 4, 15);

        Assert.Equal(10, manager.HeightForLineNumber(1));
        Assert.Equal(15, manager.HeightForLineNumber(2));
        Assert.Equal(15, manager.HeightForLineNumber(3));
        Assert.Equal(15, manager.HeightForLineNumber(4));
        Assert.Equal(10, manager.HeightForLineNumber(5));

        Assert.Equal(10, manager.GetAccumulatedLineHeightsIncludingLineNumber(1));
        Assert.Equal(25, manager.GetAccumulatedLineHeightsIncludingLineNumber(2));
        Assert.Equal(40, manager.GetAccumulatedLineHeightsIncludingLineNumber(3));
        Assert.Equal(55, manager.GetAccumulatedLineHeightsIncludingLineNumber(4));
        Assert.Equal(65, manager.GetAccumulatedLineHeightsIncludingLineNumber(5));
    }

    // Monaco: 'can change existing custom line height'
    [Fact]
    public void Change_ExistingCustomLineHeight_IsReplaced()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 3, 3, 20);
        Assert.Equal(20, manager.HeightForLineNumber(3));

        manager.InsertOrChangeCustomLineHeight("dec1", 3, 3, 30);
        Assert.Equal(30, manager.HeightForLineNumber(3));

        Assert.Equal(50, manager.GetAccumulatedLineHeightsIncludingLineNumber(3));
        Assert.Equal(60, manager.GetAccumulatedLineHeightsIncludingLineNumber(4));
    }

    // Monaco: 'can remove custom line height'
    [Fact]
    public void Remove_CustomLineHeight_RestoresDefault()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 3, 3, 20);
        Assert.Equal(20, manager.HeightForLineNumber(3));

        manager.RemoveCustomLineHeight("dec1");
        Assert.Equal(10, manager.HeightForLineNumber(3));

        Assert.Equal(30, manager.GetAccumulatedLineHeightsIncludingLineNumber(3));
        Assert.Equal(40, manager.GetAccumulatedLineHeightsIncludingLineNumber(4));
    }

    // Monaco: 'handles overlapping custom line heights (last one wins)'
    [Fact]
    public void Insert_OverlappingCustomLineHeights_LastOneWins()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 3, 5, 20);
        manager.InsertOrChangeCustomLineHeight("dec2", 4, 6, 30);

        Assert.Equal(10, manager.HeightForLineNumber(2));
        Assert.Equal(20, manager.HeightForLineNumber(3));
        Assert.Equal(30, manager.HeightForLineNumber(4));
        Assert.Equal(30, manager.HeightForLineNumber(5));
        Assert.Equal(30, manager.HeightForLineNumber(6));
        Assert.Equal(10, manager.HeightForLineNumber(7));
    }

    // Monaco: 'handles deleting lines before custom line heights'
    [Fact]
    public void OnLinesDeleted_BeforeCustomLineHeights_ShiftsThem()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 10, 12, 20);

        manager.OnLinesDeleted(5, 7);

        Assert.Equal(20, manager.HeightForLineNumber(7));
        Assert.Equal(20, manager.HeightForLineNumber(8));
        Assert.Equal(20, manager.HeightForLineNumber(9));
        Assert.Equal(10, manager.HeightForLineNumber(10));
    }

    // Monaco: 'handles deleting lines overlapping with custom line heights'
    [Fact]
    public void OnLinesDeleted_OverlappingCustomLineHeights_TruncatesThem()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 5, 10, 20);

        manager.OnLinesDeleted(7, 12);

        Assert.Equal(20, manager.HeightForLineNumber(5));
        Assert.Equal(20, manager.HeightForLineNumber(6));
        Assert.Equal(10, manager.HeightForLineNumber(7));
    }

    // Monaco: 'handles deleting lines containing custom line heights completely'
    [Fact]
    public void OnLinesDeleted_ContainingCustomLineHeights_CollapsesToOneLine()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 5, 7, 20);

        manager.OnLinesDeleted(4, 8);

        Assert.Equal(10, manager.HeightForLineNumber(3));
        Assert.Equal(20, manager.HeightForLineNumber(4));
        Assert.Equal(10, manager.HeightForLineNumber(5));
    }

    // Monaco: 'handles deleting lines at the very beginning'
    [Fact]
    public void OnLinesDeleted_AfterFirstLineCustomHeight_KeepsIt()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("decA", 1, 1, 40);

        manager.OnLinesDeleted(2, 4);

        Assert.Equal(40, manager.HeightForLineNumber(1));
    }

    // Monaco: 'handles inserting lines before custom line heights'
    [Fact]
    public void OnLinesInserted_BeforeCustomLineHeights_ShiftsThem()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 5, 7, 20);

        manager.OnLinesInserted(3, 4);

        Assert.Equal(10, manager.HeightForLineNumber(5));
        Assert.Equal(10, manager.HeightForLineNumber(6));
        Assert.Equal(20, manager.HeightForLineNumber(7));
        Assert.Equal(20, manager.HeightForLineNumber(8));
        Assert.Equal(20, manager.HeightForLineNumber(9));
    }

    // Monaco: 'handles inserting lines inside custom line heights range'
    [Fact]
    public void OnLinesInserted_InsideCustomLineHeightsRange_ExtendsIt()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 5, 7, 20);

        manager.OnLinesInserted(6, 7);

        Assert.Equal(20, manager.HeightForLineNumber(5));
        Assert.Equal(20, manager.HeightForLineNumber(6));
        Assert.Equal(20, manager.HeightForLineNumber(7));
        Assert.Equal(20, manager.HeightForLineNumber(8));
        Assert.Equal(20, manager.HeightForLineNumber(9));
    }

    // Monaco: 'changing decoration id maintains custom line height'
    [Fact]
    public void Insert_AfterRemovingOtherDecorationId_KeepsCustomLineHeight()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 5, 7, 20);

        manager.RemoveCustomLineHeight("dec1");
        manager.InsertOrChangeCustomLineHeight("dec2", 5, 7, 20);

        Assert.Equal(20, manager.HeightForLineNumber(5));
        Assert.Equal(20, manager.HeightForLineNumber(6));
        Assert.Equal(20, manager.HeightForLineNumber(7));
    }

    // Monaco: 'accumulates heights correctly with complex setup'
    [Fact]
    public void GetAccumulatedLineHeights_ComplexSetup_IsCorrect()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("dec1", 3, 3, 15);
        manager.InsertOrChangeCustomLineHeight("dec2", 5, 7, 20);
        manager.InsertOrChangeCustomLineHeight("dec3", 10, 10, 30);

        Assert.Equal(10, manager.GetAccumulatedLineHeightsIncludingLineNumber(1));
        Assert.Equal(20, manager.GetAccumulatedLineHeightsIncludingLineNumber(2));
        Assert.Equal(35, manager.GetAccumulatedLineHeightsIncludingLineNumber(3));
        Assert.Equal(45, manager.GetAccumulatedLineHeightsIncludingLineNumber(4));
        Assert.Equal(65, manager.GetAccumulatedLineHeightsIncludingLineNumber(5));
        Assert.Equal(105, manager.GetAccumulatedLineHeightsIncludingLineNumber(7));
        Assert.Equal(125, manager.GetAccumulatedLineHeightsIncludingLineNumber(9));
        Assert.Equal(155, manager.GetAccumulatedLineHeightsIncludingLineNumber(10));
    }

    // Monaco: 'partial deletion with multiple lines for the same decoration ID'
    [Fact]
    public void OnLinesDeleted_PartialDeletionWithSameDecorationId_KeepsRemainingLine()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("decSame", 5, 5, 20);
        manager.InsertOrChangeCustomLineHeight("decSame", 6, 6, 25);

        manager.OnLinesDeleted(6, 6);

        Assert.Equal(10, manager.HeightForLineNumber(5));
        Assert.Equal(25, manager.HeightForLineNumber(6));
    }

    // Monaco: 'overlapping decorations use maximum line height'
    [Fact]
    public void HeightForLineNumber_OverlappingDecorations_UsesMaximum()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("decA", 3, 5, 40);
        manager.InsertOrChangeCustomLineHeight("decB", 4, 6, 30);

        Assert.Equal(40, manager.HeightForLineNumber(3));
        Assert.Equal(40, manager.HeightForLineNumber(4));
        Assert.Equal(40, manager.HeightForLineNumber(5));
        Assert.Equal(30, manager.HeightForLineNumber(6));
    }

    // Monaco: 'onLinesInserted with same decoration ID extending to inserted line'
    [Fact]
    public void OnLinesInserted_SameDecorationIdExtendingToInsertedLine_MovesCustomHeight()
    {
        var manager = Create();
        manager.InsertOrChangeCustomLineHeight("decA", 1, 1, 30);

        Assert.Equal(30, manager.HeightForLineNumber(1));
        Assert.Equal(10, manager.HeightForLineNumber(2));

        manager.OnLinesInserted(2, 2);
        manager.InsertOrChangeCustomLineHeight("decA", 2, 2, 30);

        // InsertOrChangeCustomLineHeight removes the old decoration first: line 1 loses the custom height.
        Assert.Equal(10, manager.HeightForLineNumber(1));
        Assert.Equal(30, manager.HeightForLineNumber(2));
        Assert.Equal(10, manager.HeightForLineNumber(3));
    }
}

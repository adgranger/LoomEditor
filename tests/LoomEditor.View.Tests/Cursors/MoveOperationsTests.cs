using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Tests.Cursors.TestHelpers;

namespace LoomEditor.View.Tests.Cursors;

// The Monaco tests of these functions go through the core commands (cursor.test.ts, P3-27): these are direct tests.
public class MoveOperationsTests
{
    private const int MaxSafeSmallInteger = 1 << 30;

    private static SingleCursorState At(int lineNumber, int column, int leftoverVisibleColumns = 0) =>
        new(
            new Range(lineNumber, column, lineNumber, column),
            SelectionStartKind.Simple,
            leftoverVisibleColumns,
            new Position(lineNumber, column),
            leftoverVisibleColumns);

    private static CursorContext Context(string text, EditorCursorOptions? options = null) => CursorTestContext.Create(text, options);

    [Fact]
    public void MoveLeft_AfterEmoji_MovesOneGrapheme()
    {
        CursorContext context = Context("a😀b");

        SingleCursorState result = MoveOperations.MoveLeft(context.CursorConfig, context.ViewModel, At(1, 3), false, 1);
        SingleCursorState fromEnd = MoveOperations.MoveLeft(context.CursorConfig, context.ViewModel, At(1, 4), false, 1);

        Assert.Equal(new Position(1, 2), result.Position);
        Assert.Equal(new Position(1, 2), fromEnd.Position);
    }

    [Fact]
    public void MoveRight_BeforeEmoji_MovesOneGrapheme()
    {
        CursorContext context = Context("a😀b");

        SingleCursorState result = MoveOperations.MoveRight(context.CursorConfig, context.ViewModel, At(1, 2), false, 1);

        Assert.Equal(new Position(1, 4), result.Position);
    }

    [Fact]
    public void MoveLeftAndRight_AtLineEdges_CrossLines()
    {
        CursorContext context = Context("abc\ndef");

        SingleCursorState left = MoveOperations.MoveLeft(context.CursorConfig, context.ViewModel, At(2, 1), false, 1);
        SingleCursorState right = MoveOperations.MoveRight(context.CursorConfig, context.ViewModel, At(1, 4), false, 1);

        Assert.Equal(new Position(1, 4), left.Position);
        Assert.Equal(new Position(2, 1), right.Position);
    }

    [Fact]
    public void MoveLeftAndRight_WithSelection_CollapseToItsEdges()
    {
        CursorContext context = Context("abcdef");
        var selected = new SingleCursorState(new Range(1, 2, 1, 5), SelectionStartKind.Simple, 0, new Position(1, 5), 0);

        SingleCursorState left = MoveOperations.MoveLeft(context.CursorConfig, context.ViewModel, selected, false, 1);
        SingleCursorState right = MoveOperations.MoveRight(context.CursorConfig, context.ViewModel, selected, false, 1);

        Assert.Equal(new Position(1, 2), left.Position);
        Assert.Equal(new Position(1, 5), right.Position);
    }

    [Fact]
    public void MoveDown_FromLongLine_KeepsLeftoverVisibleColumns()
    {
        CursorContext context = Context("aaaaaaaaaaaa\nbb\ncccccccccccc");

        SingleCursorState first = MoveOperations.MoveDown(context.CursorConfig, context.ViewModel, At(1, 10), false, 1);
        SingleCursorState second = MoveOperations.MoveDown(context.CursorConfig, context.ViewModel, first, false, 1);

        Assert.Equal(new Position(2, 3), first.Position);
        Assert.Equal(7, first.LeftoverVisibleColumns);
        Assert.Equal(new Position(3, 10), second.Position);
    }

    [Fact]
    public void MoveDown_WithTabSize4_UsesVisibleColumns()
    {
        CursorContext context = Context("    x\n\tx");

        SingleCursorState result = MoveOperations.MoveDown(context.CursorConfig, context.ViewModel, At(1, 5), false, 1);

        Assert.Equal(new Position(2, 2), result.Position);
    }

    [Fact]
    public void MoveUpAndDown_AtBufferEdges_GoToTheLineEdge()
    {
        CursorContext context = Context("abc\ndef");

        SingleCursorState up = MoveOperations.MoveUp(context.CursorConfig, context.ViewModel, At(1, 3), false, 1);
        SingleCursorState down = MoveOperations.MoveDown(context.CursorConfig, context.ViewModel, At(2, 2), false, 1);

        Assert.Equal(new Position(1, 1), up.Position);
        Assert.Equal(new Position(2, 4), down.Position);
    }

    [Fact]
    public void MoveToEndOfLine_Sticky_StoresLeftoverAndFollowsLineEnds()
    {
        CursorContext context = Context("abc\ndefghi");

        SingleCursorState end = MoveOperations.MoveToEndOfLine(context.CursorConfig, context.ViewModel, At(1, 1), false, true);
        SingleCursorState down = MoveOperations.MoveDown(context.CursorConfig, context.ViewModel, end, false, 1);

        Assert.Equal(new Position(1, 4), end.Position);
        Assert.Equal(MaxSafeSmallInteger - 4, end.LeftoverVisibleColumns);
        Assert.Equal(new Position(2, 7), down.Position);
    }

    [Fact]
    public void MoveToBeginningOfLine_TogglesBetweenFirstNonBlankAndMinColumn()
    {
        CursorContext context = Context("  ab\n   ");

        SingleCursorState first = MoveOperations.MoveToBeginningOfLine(context.CursorConfig, context.ViewModel, At(1, 5), false);
        SingleCursorState second = MoveOperations.MoveToBeginningOfLine(context.CursorConfig, context.ViewModel, first, false);
        SingleCursorState blank = MoveOperations.MoveToBeginningOfLine(context.CursorConfig, context.ViewModel, At(2, 3), false);

        Assert.Equal(new Position(1, 3), first.Position);
        Assert.Equal(new Position(1, 1), second.Position);
        Assert.Equal(new Position(2, 1), blank.Position);
    }

    [Fact]
    public void MoveToBufferEdges_GoToTheFirstAndLastPosition()
    {
        CursorContext context = Context("abc\ndef");

        SingleCursorState start = MoveOperations.MoveToBeginningOfBuffer(context.CursorConfig, context.ViewModel, At(2, 2), false);
        SingleCursorState end = MoveOperations.MoveToEndOfBuffer(context.CursorConfig, context.ViewModel, At(1, 2), false);

        Assert.Equal(new Position(1, 1), start.Position);
        Assert.Equal(new Position(2, 4), end.Position);
    }

    [Fact]
    public void MoveToNextAndPrevBlankLine_SkipParagraphs()
    {
        CursorContext context = Context("a\n\nb\n\nc");
        SingleCursorState cursor = At(1, 1);

        cursor = MoveOperations.MoveToNextBlankLine(context.CursorConfig, context.ViewModel, cursor, false);
        Assert.Equal(new Position(2, 1), cursor.Position);
        cursor = MoveOperations.MoveToNextBlankLine(context.CursorConfig, context.ViewModel, cursor, false);
        Assert.Equal(new Position(4, 1), cursor.Position);
        cursor = MoveOperations.MoveToNextBlankLine(context.CursorConfig, context.ViewModel, cursor, false);
        Assert.Equal(new Position(5, 1), cursor.Position);

        SingleCursorState prev = MoveOperations.MoveToPrevBlankLine(context.CursorConfig, context.ViewModel, At(5, 1), false);
        Assert.Equal(new Position(4, 1), prev.Position);
    }

    [Fact]
    public void MoveRight_WithStickyTabStops_SkipsAnIndentUnit()
    {
        CursorContext context = Context("    x", new EditorCursorOptions { StickyTabStops = true });

        SingleCursorState right = MoveOperations.MoveRight(context.CursorConfig, context.ViewModel, At(1, 1), false, 1);

        Assert.Equal(new Position(1, 5), right.Position);
    }

    [Fact]
    public void TranslateDownAndUp_MoveTheWholeSelection()
    {
        CursorContext context = Context("abc\ndef\nghi");
        var selected = new SingleCursorState(new Range(2, 1, 2, 3), SelectionStartKind.Simple, 0, new Position(2, 3), 0);

        SingleCursorState down = MoveOperations.TranslateDown(context.CursorConfig, context.ViewModel, selected);
        SingleCursorState up = MoveOperations.TranslateUp(context.CursorConfig, context.ViewModel, selected);

        Assert.Equal(new Selection(3, 1, 3, 3), down.Selection);
        Assert.Equal(new Selection(1, 1, 1, 3), up.Selection);
    }
}

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Comments;

namespace LoomEditor.View.Tests.Comments;

public class CommentRangeTests
{
    // Line 1 "let value = 1;", 2 "abcdefgh", 3 "second", 4 "fourth line", 5 "" (empty)
    private static readonly TextModel Model = new("let value = 1;\nabcdefgh\nsecond\nfourth line\n");

    private static CommentRange From(int l1, int c1, int l2, int c2) =>
        CommentRange.FromSelection(Model, new Selection(l1, c1, l2, c2));

    private static CommentRange Exact(int l1, int c1, int l2, int c2) => new(l1, l2, new Range(l1, c1, l2, c2));

    [Fact]
    public void FromSelection_Empty_CursorLine() => Assert.Equal(CommentRange.Lines(2, 2), From(2, 4, 2, 4));

    [Fact]
    public void FromSelection_Word_ExactText() => Assert.Equal(Exact(1, 5, 1, 10), From(1, 5, 1, 10));

    [Fact]
    public void FromSelection_RightToLeft_SameAsLeftToRight() => Assert.Equal(From(2, 3, 4, 2), From(4, 2, 2, 3));

    [Fact]
    public void FromSelection_ShiftDownLines_WholeLines() => Assert.Equal(CommentRange.Lines(2, 3), From(2, 1, 4, 1));

    [Fact]
    public void FromSelection_TwoLinesEndingAtColumnOne_IsOneLine() => Assert.Equal(CommentRange.Lines(2, 2), From(2, 1, 3, 1));

    [Fact]
    public void FromSelection_FullLineDrag_WholeLine() =>
        Assert.Equal(CommentRange.Lines(3, 3), From(3, 1, 3, Model.GetLineMaxColumn(3)));

    [Fact]
    public void FromSelection_FullLinesDrag_WholeLines() =>
        Assert.Equal(CommentRange.Lines(2, 4), From(2, 1, 4, Model.GetLineMaxColumn(4)));

    [Fact]
    public void FromSelection_MidLineToColumnOneOfNextLine_EndsAtPreviousLineEnd() =>
        Assert.Equal(Exact(2, 3, 2, Model.GetLineMaxColumn(2)), From(2, 3, 3, 1));

    [Fact]
    public void FromSelection_OnlyTheLineBreak_IsWholeLine() =>
        Assert.Equal(CommentRange.Lines(2, 2), From(2, Model.GetLineMaxColumn(2), 3, 1));

    [Fact]
    public void FromSelection_MultiLinePartial_ExactText() => Assert.Equal(Exact(2, 3, 4, 2), From(2, 3, 4, 2));

    [Fact]
    public void FromRange_ColumnPastLineEnd_IsClamped() =>
        Assert.Equal(Exact(2, 5, 2, 9), CommentRange.FromRange(Model, new Range(2, 5, 2, 999)));

    [Fact]
    public void FromRange_EmptyLine_IsWholeLine() =>
        Assert.Equal(CommentRange.Lines(5, 5), CommentRange.FromRange(Model, new Range(5, 1, 5, 1)));
}

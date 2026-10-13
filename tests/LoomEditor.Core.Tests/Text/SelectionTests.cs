using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Text;

// Monaco has no dedicated selection.test.ts; these tests cover the behavior of
// src/vs/editor/common/core/selection.ts.
public class SelectionTests
{
    [Fact]
    public void LeftToRight_AnchorIsRangeStart()
    {
        var selection = new Selection(1, 3, 1, 10);

        Assert.Equal(new Range(1, 3, 1, 10), selection.Range);
        Assert.Equal(SelectionDirection.LeftToRight, selection.Direction);
        Assert.Equal(new Position(1, 3), selection.SelectionStart);
        Assert.Equal(new Position(1, 10), selection.Position);
    }

    [Fact]
    public void RightToLeft_AnchorIsRangeEnd()
    {
        var selection = new Selection(1, 10, 1, 3);

        Assert.Equal(new Range(1, 3, 1, 10), selection.Range);
        Assert.Equal(SelectionDirection.RightToLeft, selection.Direction);
        Assert.Equal(new Position(1, 10), selection.SelectionStart);
        Assert.Equal(new Position(1, 3), selection.Position);
    }

    [Fact]
    public void Collapsed_IsEmptyAndLeftToRight()
    {
        var selection = Selection.Collapsed(new Position(2, 4));

        Assert.True(selection.IsEmpty);
        Assert.Equal(SelectionDirection.LeftToRight, selection.Direction);
    }

    [Fact]
    public void Equality_TakesDirectionIntoAccount()
    {
        // Monaco: Selection.equalsSelection compares anchor and caret, not only the range.
        Assert.Equal(new Selection(1, 1, 2, 2), new Selection(1, 1, 2, 2));
        Assert.NotEqual(new Selection(1, 1, 2, 2), new Selection(2, 2, 1, 1));
        Assert.Equal(new Selection(1, 1, 2, 2).Range, new Selection(2, 2, 1, 1).Range);
    }

    [Theory]
    [InlineData(SelectionDirection.LeftToRight, 1, 2, 3, 4)]
    [InlineData(SelectionDirection.RightToLeft, 3, 4, 1, 2)]
    public void FromRange_UsesDirection(SelectionDirection direction, int anchorLine, int anchorColumn, int caretLine, int caretColumn)
    {
        var selection = Selection.FromRange(new Range(1, 2, 3, 4), direction);

        Assert.Equal(new Selection(anchorLine, anchorColumn, caretLine, caretColumn), selection);
        Assert.Equal(direction, selection.Direction);
    }

    [Fact]
    public void WithPosition_MovesOnlyTheCaret()
    {
        var selection = new Selection(1, 1, 1, 5).WithPosition(new Position(3, 2));

        Assert.Equal(new Selection(1, 1, 3, 2), selection);
    }

    [Fact]
    public void WithEnd_KeepsDirection()
    {
        Assert.Equal(new Selection(1, 1, 4, 4), new Selection(1, 1, 2, 2).WithEnd(new Position(4, 4)));
        Assert.Equal(new Selection(4, 4, 1, 1), new Selection(2, 2, 1, 1).WithEnd(new Position(4, 4)));
    }

    [Fact]
    public void WithStart_KeepsDirection()
    {
        Assert.Equal(new Selection(1, 1, 3, 3), new Selection(2, 2, 3, 3).WithStart(new Position(1, 1)));
        Assert.Equal(new Selection(3, 3, 1, 1), new Selection(3, 3, 2, 2).WithStart(new Position(1, 1)));
    }

    [Fact]
    public void WithEnd_BeforeStart_FlipsDirectionLikeMonaco()
    {
        var selection = new Selection(2, 2, 3, 3).WithEnd(new Position(1, 1));

        Assert.Equal(new Selection(2, 2, 1, 1), selection);
        Assert.Equal(SelectionDirection.RightToLeft, selection.Direction);
    }

    [Fact]
    public void ToString_UsesMonacoFormat()
    {
        Assert.Equal("[1,10 -> 1,3]", new Selection(1, 10, 1, 3).ToString());
    }
}

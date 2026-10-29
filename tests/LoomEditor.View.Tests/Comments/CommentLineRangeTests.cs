using LoomEditor.Core.Text;
using LoomEditor.View.Comments;

namespace LoomEditor.View.Tests.Comments;

public class CommentLineRangeTests
{
    private static (int, int) Lines(int l1, int c1, int l2, int c2) =>
        CommentLineRange.FromSelection(new Selection(l1, c1, l2, c2));

    [Fact]
    public void FromSelection_Empty_CursorLine() => Assert.Equal((4, 4), Lines(4, 2, 4, 2));

    [Fact]
    public void FromSelection_SingleLineNonEmpty_ThatLine() => Assert.Equal((2, 2), Lines(2, 1, 2, 5));

    [Fact]
    public void FromSelection_ThreeLines_StartToEnd() => Assert.Equal((2, 4), Lines(2, 3, 4, 2));

    [Fact]
    public void FromSelection_EndsAtColumnOne_ExcludesLastLine() => Assert.Equal((2, 3), Lines(2, 3, 4, 1));

    [Fact]
    public void FromSelection_TwoLinesEndingAtColumnOne_IsOneLine() => Assert.Equal((2, 2), Lines(2, 1, 3, 1));

    [Fact]
    public void FromSelection_RightToLeft_SameAsLeftToRight() => Assert.Equal(Lines(2, 3, 4, 2), Lines(4, 2, 2, 3));
}

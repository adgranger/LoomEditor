using LoomEditor.View.Comments;

namespace LoomEditor.View.Tests.Comments;

public class CommentStringsTests
{
    [Fact]
    public void ThreadTitle_FormatsLine() => Assert.Equal("Comment on line 12", CommentStrings.ThreadTitle(12));

    [Fact]
    public void ThreadTitle_Range_FormatsLines() => Assert.Equal("Comment on lines 12 to 15", CommentStrings.ThreadTitle(12, 15));

    [Fact]
    public void ThreadTitle_RangeOfOne_EqualsSingleLine() => Assert.Equal(CommentStrings.ThreadTitle(12), CommentStrings.ThreadTitle(12, 12));

    [Fact]
    public void ThreadAutomationName_Range_FormatsLines() =>
        Assert.Equal("Comment thread on lines 3 to 5", CommentStrings.ThreadAutomationName(3, 5));

    [Fact]
    public void ThreadAdded_Range_FormatsLines() => Assert.Equal("Comment thread added on lines 3 to 5", CommentStrings.ThreadAdded(3, 5));

    [Fact]
    public void AddCommentOnLines_Range_FormatsLines() =>
        Assert.Equal("Add a comment on lines 3 to 5", CommentStrings.AddCommentOnLines(3, 5));

    [Fact]
    public void AddCommentOnLines_OneLine_EqualsSingleLine() =>
        Assert.Equal(CommentStrings.AddCommentOnLine(7), CommentStrings.AddCommentOnLines(7, 7));

    [Fact]
    public void CommentCount_SingularAndPlural()
    {
        Assert.Equal("1 comment", CommentStrings.CommentCount(1));
        Assert.Equal("3 comments", CommentStrings.CommentCount(3));
    }

    [Fact]
    public void ToolbarName_EveryActionHasAName()
    {
        foreach (MarkdownToolbarAction action in Enum.GetValues<MarkdownToolbarAction>())
        {
            Assert.False(string.IsNullOrEmpty(CommentStrings.ToolbarName(action)));
        }
    }

    [Fact]
    public void ToolbarName_Unknown_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CommentStrings.ToolbarName((MarkdownToolbarAction)99));
}

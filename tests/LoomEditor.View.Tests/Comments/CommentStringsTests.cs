using LoomEditor.View.Comments;

namespace LoomEditor.View.Tests.Comments;

public class CommentStringsTests
{
    [Fact]
    public void ThreadTitle_FormatsLine() => Assert.Equal("Comment on line 12", CommentStrings.ThreadTitle(12));

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

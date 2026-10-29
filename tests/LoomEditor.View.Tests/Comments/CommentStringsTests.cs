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

    [Fact]
    public void CommentedText_Short_Prefixed() => Assert.Equal("Commented text: value", CommentStrings.CommentedText("value"));

    [Fact]
    public void CommentedText_LineBreaks_BecomeSpaces() =>
        Assert.Equal("Commented text: a b c", CommentStrings.CommentedText("a\r\nb\nc"));

    [Fact]
    public void CommentedText_Long_CutWithEllipsis() =>
        Assert.Equal("Commented text: " + new string('x', 80) + "…", CommentStrings.CommentedText(new string('x', 81)));

    [Fact]
    public void AddCommentOnSelectedText_OneLine() =>
        Assert.Equal("Add a comment on the selected text on line 4", CommentStrings.AddCommentOnSelectedText(4, 4));

    [Fact]
    public void AddCommentOnSelectedText_Lines() =>
        Assert.Equal("Add a comment on the selected text on lines 4 to 6", CommentStrings.AddCommentOnSelectedText(4, 6));

    [Fact]
    public void ThreadAddedOnText_OneLine() =>
        Assert.Equal("Comment thread added on the selected text on line 4", CommentStrings.ThreadAddedOnText(4, 4));

    [Fact]
    public void ThreadAddedOnText_Lines() =>
        Assert.Equal("Comment thread added on the selected text on lines 4 to 6", CommentStrings.ThreadAddedOnText(4, 6));
}

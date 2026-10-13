using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Text;

public class TextEditTests
{
    [Fact]
    public void Insert_CreatesEmptyRangeAtPosition()
    {
        var edit = TextEdit.Insert(new Position(2, 3), "abc");

        Assert.Equal(new Range(2, 3, 2, 3), edit.Range);
        Assert.Equal("abc", edit.Text);
        Assert.True(edit.IsInsert);
        Assert.False(edit.IsDelete);
        Assert.False(edit.IsNoOp);
    }

    [Fact]
    public void Delete_HasEmptyText()
    {
        var edit = TextEdit.Delete(new Range(1, 1, 1, 4));

        Assert.Equal(string.Empty, edit.Text);
        Assert.True(edit.IsDelete);
        Assert.False(edit.IsInsert);
    }

    [Fact]
    public void Replace_IsNeitherInsertNorDelete()
    {
        var edit = TextEdit.Replace(new Range(1, 1, 1, 4), "xyz");

        Assert.False(edit.IsInsert);
        Assert.False(edit.IsDelete);
        Assert.False(edit.IsNoOp);
    }

    [Fact]
    public void EmptyRangeAndEmptyText_IsNoOp()
    {
        Assert.True(TextEdit.Insert(new Position(1, 1), string.Empty).IsNoOp);
    }

    [Fact]
    public void Constructor_NullText_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TextEdit(new Range(1, 1, 1, 1), null!));
    }

    [Fact]
    public void Equality_ComparesRangeAndText()
    {
        Assert.Equal(TextEdit.Insert(new Position(1, 1), "a"), TextEdit.Insert(new Position(1, 1), "a"));
        Assert.NotEqual(TextEdit.Insert(new Position(1, 1), "a"), TextEdit.Insert(new Position(1, 1), "b"));
    }

    [Fact]
    public void ToString_ShowsRangeAndText()
    {
        Assert.Equal("[1,1 -> 1,4] => \"abc\"", TextEdit.Replace(new Range(1, 1, 1, 4), "abc").ToString());
    }
}

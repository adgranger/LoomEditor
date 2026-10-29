using LoomEditor.Core.Model;
using LoomEditor.View.Comments;

namespace LoomEditor.View.Tests.Comments;

public class SuggestionEditTests
{
    private static SuggestionBlock Block(string text) => new(0, text);

    [Fact]
    public void Create_SingleLine_ReplacesLineContent()
    {
        var m = new TextModel("abc\ndef");
        var e = SuggestionEdit.Create(m, 2, Block("X"))!.Value;
        Assert.Equal(new LoomEditor.Core.Text.Range(2, 1, 2, 4), e.Range);
        Assert.Equal("X", e.Text);
    }

    [Fact]
    public void Create_MultiLine_UsesModelEol()
    {
        var m = new TextModel("a\r\nb");
        Assert.Equal("x\r\ny", SuggestionEdit.Create(m, 1, Block("x\ny"))!.Value.Text);
    }

    [Fact]
    public void Create_LineOutOfRange_ReturnsNull()
    {
        var m = new TextModel("a");
        Assert.Null(SuggestionEdit.Create(m, 0, Block("x")));
        Assert.Null(SuggestionEdit.Create(m, 2, Block("x")));
    }

    [Theory]
    [InlineData("a\nb\nc", 2, "a\nc")]
    [InlineData("a\nb\nc", 3, "a\nb")]
    [InlineData("a", 1, "")]
    [InlineData("a\r\nb\r\nc", 2, "a\r\nc")]
    [InlineData("a\r\nb\r\nc", 3, "a\r\nb")]
    public void Create_Deletion_GivesExpectedText(string text, int line, string expected)
    {
        var m = new TextModel(text);
        m.ApplyEdits([SuggestionEdit.Create(m, line, Block(""))!.Value]);
        Assert.Equal(expected, m.GetValue());
    }

    [Theory]
    [InlineData("a\nb\nc", "a\nX\nY\nc")]
    [InlineData("a\r\nb\r\nc", "a\r\nX\r\nY\r\nc")]
    public void Create_AppliedEdit_GivesExpectedText(string text, string expected)
    {
        var m = new TextModel(text);
        m.ApplyEdits([SuggestionEdit.Create(m, 2, Block("X\nY"))!.Value]);
        Assert.Equal(expected, m.GetValue());
    }

    [Fact]
    public void IsStale_ContentChanged_ReturnsTrue() => Assert.True(SuggestionEdit.IsStale(new TextModel("a\nb"), 2, "c"));

    [Fact]
    public void IsStale_SameContent_ReturnsFalse() => Assert.False(SuggestionEdit.IsStale(new TextModel("a\nb"), 2, "b"));

    [Fact]
    public void IsNoOp_SameText_ReturnsTrue()
    {
        var m = new TextModel("a\nb");
        Assert.True(SuggestionEdit.IsNoOp(m, 2, Block("b")));
        Assert.False(SuggestionEdit.IsNoOp(m, 2, Block("c")));
    }

    [Theory]
    [InlineData("a\nb\nc\nd")]
    [InlineData("a\r\nb\r\nc\r\nd")]
    public void GetRangeText_ThreeLines_JoinsWithLf(string text) =>
        Assert.Equal("b\nc\nd", SuggestionEdit.GetRangeText(new TextModel(text), 2, 4));

    [Fact]
    public void GetRangeText_OneLine_IsLineContent() =>
        Assert.Equal("b", SuggestionEdit.GetRangeText(new TextModel("a\nb\nc"), 2, 2));

    [Theory]
    [InlineData("a\nb\nc\nd", "a\nX\nY\nd")]
    [InlineData("a\r\nb\r\nc\r\nd", "a\r\nX\r\nY\r\nd")]
    public void CreateRange_ReplacesAllLines(string text, string expected)
    {
        var m = new TextModel(text);
        m.ApplyEdits([SuggestionEdit.Create(m, 2, 3, Block("X\nY"))!.Value]);
        Assert.Equal(expected, m.GetValue());
    }

    [Fact]
    public void CreateRange_FewerNewLines_ShrinksDocument()
    {
        var m = new TextModel("a\nb\nc\nd");
        m.ApplyEdits([SuggestionEdit.Create(m, 2, 3, Block("X"))!.Value]);
        Assert.Equal("a\nX\nd", m.GetValue());
    }

    [Fact]
    public void CreateRange_MoreNewLines_GrowsDocument()
    {
        var m = new TextModel("a\nb\nc");
        m.ApplyEdits([SuggestionEdit.Create(m, 2, 2, Block("X\nY\nZ"))!.Value]);
        Assert.Equal("a\nX\nY\nZ\nc", m.GetValue());
    }

    [Theory]
    [InlineData("a\nb\nc\nd", 2, 3, "a\nd")]
    [InlineData("a\nb\nc\nd", 3, 4, "a\nb")]
    [InlineData("a\r\nb\r\nc\r\nd", 3, 4, "a\r\nb")]
    [InlineData("a\nb\nc", 1, 3, "")]
    public void CreateRange_Deletion_GivesExpectedText(string text, int start, int end, string expected)
    {
        var m = new TextModel(text);
        m.ApplyEdits([SuggestionEdit.Create(m, start, end, Block(""))!.Value]);
        Assert.Equal(expected, m.GetValue());
    }

    [Fact]
    public void CreateRange_EndBeforeStart_ReturnsNull() =>
        Assert.Null(SuggestionEdit.Create(new TextModel("a\nb\nc"), 3, 2, Block("x")));

    [Fact]
    public void CreateRange_EndPastLastLine_ReturnsNull() =>
        Assert.Null(SuggestionEdit.Create(new TextModel("a\nb\nc"), 2, 4, Block("x")));

    [Fact]
    public void IsStaleRange_LineInsideChanged_ReturnsTrue() =>
        Assert.True(SuggestionEdit.IsStale(new TextModel("a\nb\nc"), 1, 3, "a\nX\nc"));

    [Fact]
    public void IsStaleRange_SameText_ReturnsFalse() =>
        Assert.False(SuggestionEdit.IsStale(new TextModel("a\r\nb\r\nc"), 1, 3, "a\nb\nc"));

    [Fact]
    public void IsNoOpRange_SameLines_ReturnsTrue() =>
        Assert.True(SuggestionEdit.IsNoOp(new TextModel("a\nb\nc"), 1, 2, Block("a\nb")));
}

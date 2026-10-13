// Not ported from Monaco: sanity tests of the LoomEditor reference buffer.

using LoomEditor.Core.Text;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class NaiveTextBufferTests
{
    [Fact]
    public void InsertDelete_Edits_UpdateText()
    {
        var buffer = new NaiveTextBuffer("hello");
        buffer.Insert(5, " world");
        buffer.Delete(0, 1);

        Assert.Equal("ello world", buffer.GetText());
        Assert.Equal(10, buffer.Length);
    }

    [Fact]
    public void GetLines_MixedLineBreaks_SplitsOnEachKind()
    {
        var buffer = new NaiveTextBuffer("a\r\nb\rc\nd");

        Assert.Equal(["a", "b", "c", "d"], buffer.GetLines());
    }

    [Fact]
    public void GetLines_TrailingLineBreakAndEmpty_KeepsFinalEmptyLine()
    {
        Assert.Equal(["a", ""], new NaiveTextBuffer("a\n").GetLines());
        Assert.Equal([""], new NaiveTextBuffer().GetLines());
    }

    [Fact]
    public void GetOffsetAtAndGetPositionAt_Crlf_AreInverse()
    {
        var buffer = new NaiveTextBuffer("ab\r\ncd");

        Assert.Equal(4, buffer.GetOffsetAt(2, 1));
        Assert.Equal(new Position(2, 1), buffer.GetPositionAt(4));
        Assert.Equal(new Position(1, 3), buffer.GetPositionAt(2));
        Assert.Equal(new Position(2, 2), buffer.GetPositionAt(5));
    }
}

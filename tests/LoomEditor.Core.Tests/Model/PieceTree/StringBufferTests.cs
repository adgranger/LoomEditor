// Not ported from Monaco: Monaco's StringBuffer is a plain data holder. All tests here are LoomEditor additions.

using LoomEditor.Core.Model.PieceTree;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class StringBufferTests
{
    [Fact]
    public void Constructor_InitialText_ExposesLengthIndexerSubstringAndToString()
    {
        var buffer = new StringBuffer("abc\ndef", [0, 4]);

        Assert.Equal(7, buffer.Length);
        Assert.Equal('a', buffer[0]);
        Assert.Equal('f', buffer[6]);
        Assert.Equal("c\nd", buffer.Substring(2, 3));
        Assert.Equal("abc\ndef", buffer.ToString());
        Assert.Equal([0, 4], buffer.LineStarts);
    }

    [Fact]
    public void Append_ManyTimes_KeepsContentAndLength()
    {
        var buffer = new StringBuffer("", [0]);
        for (int i = 0; i < 10_000; i++)
        {
            buffer.Append("ab");
        }

        Assert.Equal(20_000, buffer.Length);
        Assert.Equal(string.Concat(Enumerable.Repeat("ab", 10_000)), buffer.ToString());
        Assert.Equal('b', buffer[19_999]);
    }

    [Fact]
    public void Append_EmptyString_ChangesNothing()
    {
        var buffer = new StringBuffer("xy", [0]);

        buffer.Append("");

        Assert.Equal(2, buffer.Length);
        Assert.Equal("xy", buffer.ToString());
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, -1)]
    [InlineData(0, 4)]
    [InlineData(3, 1)]
    public void Substring_OutOfRange_Throws(int start, int length)
    {
        var buffer = new StringBuffer("abc", [0]);

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Substring(start, length));
    }

    [Fact]
    public void Indexer_OutOfRange_Throws()
    {
        var buffer = new StringBuffer("abc", [0]);

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[3]);
    }

    [Fact]
    public void LineStarts_Replaced_ReturnsNewList()
    {
        var buffer = new StringBuffer("a", [0]);

        buffer.LineStarts = [0, 5];

        Assert.Equal([0, 5], buffer.LineStarts);
    }
}

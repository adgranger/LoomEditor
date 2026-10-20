using LoomEditor.Core.Model.BracketPairs.Tree;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model.BracketPairs.Tree;

public class LengthTests
{
    private static string ToStr(Length length) => length.ToObj().ToString();

    // Monaco: 'Bracket Pair Colorizer - Length' > 'Basic'
    [Fact]
    public void Basic()
    {
        var l1 = Length.Of(100, 10);
        Assert.Equal(100, l1.ToObj().LineCount);
        Assert.Equal(10, l1.ToObj().ColumnCount);

        Assert.Equal("200,10", ToStr(Length.Add(l1, Length.Of(100, 10))));
        Assert.Equal("100,20", ToStr(Length.Add(l1, Length.Of(0, 10))));
    }

    // Monaco: 'Bracket Pair Colorizer - Length' > 'lengthDiffNonNeg'
    [Fact]
    public void DiffNonNegative_Cases()
    {
        Assert.Equal("0,10", ToStr(Length.DiffNonNegative(Length.Of(100, 10), Length.Of(100, 20))));
        Assert.Equal("1,20", ToStr(Length.DiffNonNegative(Length.Of(100, 10), Length.Of(101, 20))));
        Assert.Equal("0,0", ToStr(Length.DiffNonNegative(Length.Of(101, 30), Length.Of(101, 20))));
        Assert.Equal("0,0", ToStr(Length.DiffNonNegative(Length.Of(102, 10), Length.Of(101, 20))));
    }

    [Fact]
    public void Add_LinesInSecondLength_DropsFirstColumns()
    {
        Assert.Equal(Length.Of(30, 3), Length.Add(Length.Of(10, 5), Length.Of(20, 3)));
    }

    [Fact]
    public void ToRange_And_FromPosition_RoundTrip()
    {
        Assert.Equal(new Range(1, 5, 3, 2), Length.ToRange(Length.Of(0, 4), Length.Of(2, 1)));
        Assert.Equal(Length.Of(2, 1), Length.FromPosition(new Position(3, 2)));
    }

    [Fact]
    public void OfString_CountsLinesAndLastLineLength()
    {
        Assert.Equal("2,3", ToStr(Length.OfString("ab\ncd\r\nefg")));
    }
}

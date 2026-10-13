using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Text;

// Monaco has no dedicated position.test.ts; these tests cover the behavior of
// src/vs/editor/common/core/position.ts.
public class PositionTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var position = new Position(3, 7);

        Assert.Equal(3, position.LineNumber);
        Assert.Equal(7, position.Column);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 5)]
    public void Constructor_ValueBelowOne_Throws(int lineNumber, int column)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Position(lineNumber, column));
    }

    [Fact]
    public void Equality_ComparesValues()
    {
        Assert.Equal(new Position(1, 2), new Position(1, 2));
        Assert.NotEqual(new Position(1, 2), new Position(2, 1));
        Assert.True(new Position(1, 2) == new Position(1, 2));
    }

    [Theory]
    [InlineData(1, 1, 1, 2, -1)]
    [InlineData(1, 9, 2, 1, -1)]
    [InlineData(2, 1, 2, 1, 0)]
    [InlineData(3, 1, 2, 50, 1)]
    public void CompareTo_OrdersByLineThenColumn(int line1, int column1, int line2, int column2, int expectedSign)
    {
        Assert.Equal(expectedSign, Math.Sign(new Position(line1, column1).CompareTo(new Position(line2, column2))));
    }

    [Fact]
    public void IsBefore_IsStrict()
    {
        var a = new Position(1, 5);

        Assert.True(a.IsBefore(new Position(1, 6)));
        Assert.False(a.IsBefore(a));
        Assert.True(a.IsBeforeOrEqual(a));
        Assert.False(a.IsBeforeOrEqual(new Position(1, 4)));
    }

    [Fact]
    public void ComparisonOperators()
    {
        var a = new Position(1, 5);
        var b = new Position(2, 1);

        Assert.True(a < b);
        Assert.True(a <= b);
        Assert.True(b > a);
        Assert.True(b >= a);
        Assert.False(a > b);
    }

    [Fact]
    public void With_ReplacesOnlyGivenValues()
    {
        var position = new Position(3, 7);

        Assert.Equal(new Position(5, 7), position.With(lineNumber: 5));
        Assert.Equal(new Position(3, 1), position.With(column: 1));
        Assert.Equal(position, position.With());
    }

    [Fact]
    public void Delta_ClampsToOne()
    {
        var position = new Position(3, 7);

        Assert.Equal(new Position(4, 9), position.Delta(1, 2));
        Assert.Equal(new Position(1, 1), position.Delta(-10, -10));
    }

    [Fact]
    public void MinAndMax()
    {
        var a = new Position(1, 5);
        var b = new Position(2, 1);

        Assert.Equal(a, Position.Min(a, b));
        Assert.Equal(b, Position.Max(a, b));
    }

    [Fact]
    public void ToString_UsesMonacoFormat()
    {
        Assert.Equal("(3,7)", new Position(3, 7).ToString());
    }
}

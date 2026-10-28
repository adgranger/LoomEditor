using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Text;

public class LineRangeTests
{
    // Monaco: 'contains'
    [Fact]
    public void Contains_LineNumbers_HalfOpen()
    {
        LineRange r = new(2, 3);
        Assert.False(r.Contains(1));
        Assert.True(r.Contains(2));
        Assert.False(r.Contains(3));
        Assert.False(r.Contains(4));
    }

    [Fact]
    public void OfLength_StartAndLength()
    {
        LineRange r = LineRange.OfLength(4, 3);
        Assert.Equal(new LineRange(4, 7), r);
        Assert.Equal(3, r.Length);
    }

    [Fact]
    public void Ctor_StartAfterEnd_Throws() => Assert.Throws<ArgumentException>(() => new LineRange(5, 4));

    [Theory]
    [InlineData(1, 10, 20, 30, "[1,10)")]
    [InlineData(1, 10, 3, 5, "[1,3) [5,10)")]
    [InlineData(1, 10, 1, 4, "[4,10)")]
    [InlineData(1, 10, 6, 12, "[1,6)")]
    [InlineData(3, 5, 1, 10, "")]
    public void Subtract_Cases(int a1, int a2, int b1, int b2, string expected)
    {
        string actual = string.Join(" ", LineRange.Subtract(new LineRange(a1, a2), new LineRange(b1, b2)));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Subtract_Null_ReturnsA() => Assert.Equal([new LineRange(1, 2)], LineRange.Subtract(new LineRange(1, 2), null));

    [Fact]
    public void Join_Many()
    {
        IReadOnlyList<LineRange> result = LineRange.JoinMany([[new(1, 3), new(8, 9)], [new(3, 5), new(7, 8)]]);
        Assert.Equal("[1,5) [7,9)", string.Join(" ", result));
        Assert.Equal(new LineRange(1, 9), LineRange.Join([new(2, 3), new(1, 4), new(8, 9)]));
    }

    [Fact]
    public void ToInclusiveRange_Empty_Null()
    {
        Assert.Null(new LineRange(3, 3).ToInclusiveRange());
        Assert.Equal(new Range(2, 1, 3, int.MaxValue), new LineRange(2, 4).ToInclusiveRange());
    }

    [Fact]
    public void Equals_SameBounds_True()
    {
        Assert.Equal(new LineRange(1, 2), new LineRange(1, 2));
        Assert.NotEqual(new LineRange(1, 2), new LineRange(1, 3));
    }

    [Fact]
    public void ToString_Format() => Assert.Equal("[2,5)", new LineRange(2, 5).ToString());

    [Fact]
    public void Intersect_TouchingEmpty_DisjointNull()
    {
        Assert.Equal(new LineRange(3, 3), new LineRange(1, 3).Intersect(new LineRange(3, 5)));
        Assert.Null(new LineRange(1, 2).Intersect(new LineRange(4, 5)));
        Assert.True(new LineRange(1, 3).IntersectsOrTouches(new LineRange(3, 5)));
        Assert.False(new LineRange(1, 3).IntersectsStrict(new LineRange(3, 5)));
    }

    [Fact]
    public void Distance_Range_And_Line()
    {
        Assert.Equal(2, new LineRange(1, 3).DistanceToRange(new LineRange(5, 6)));
        Assert.Equal(0, new LineRange(1, 5).DistanceToRange(new LineRange(2, 3)));
        Assert.Equal(1, new LineRange(3, 5).DistanceToLine(2));
        Assert.Equal(1, new LineRange(3, 5).DistanceToLine(6));
    }

    [Fact]
    public void MapToLineArray_And_OffsetRange()
    {
        Assert.Equal([2, 3], new LineRange(2, 4).MapToLineArray(l => l));
        Assert.Equal(new OffsetRange(1, 3), new LineRange(2, 4).ToOffsetRange());
        Assert.Equal(new LineRange(1, 6), new LineRange(2, 4).AddMargin(1, 2));
    }
}

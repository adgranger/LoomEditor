using LoomEditor.Core.Diff;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Diff;

public class LineRangeMappingTests
{
    private static LineRangeMapping M(int os, int oe, int ms, int me) => new(new LineRange(os, oe), new LineRange(ms, me));

    [Fact]
    public void Inverse_FullCoverage()
    {
        List<LineRangeMapping> result = LineRangeMapping.Inverse([M(2, 3, 2, 4), M(6, 7, 7, 8)], 8, 9);
        Assert.Equal(["{[1,2)->[1,2)}", "{[3,6)->[4,7)}", "{[7,9)->[8,10)}"], result.Select(r => r.ToString()));
    }

    [Fact]
    public void Inverse_Empty()
    {
        Assert.Equal(["{[1,4)->[1,4)}"], LineRangeMapping.Inverse([], 3, 3).Select(r => r.ToString()));
        Assert.Empty(LineRangeMapping.Inverse([], 0, 0));
    }

    [Fact]
    public void Clip_KeepsIntersectionsOfBothSides()
    {
        List<LineRangeMapping> result = LineRangeMapping.Clip([M(1, 5, 1, 5), M(8, 9, 8, 9)], new LineRange(3, 6), new LineRange(2, 4));
        Assert.Equal(["{[3,5)->[2,4)}"], result.Select(r => r.ToString()));
    }

    [Fact]
    public void Clip_EmptySide_Dropped()
    {
        Assert.Empty(LineRangeMapping.Clip([M(1, 3, 1, 3)], new LineRange(5, 9), new LineRange(1, 3)));
    }

    [Fact]
    public void Join_UnionOfBothSides()
    {
        Assert.Equal("{[1,6)->[2,9)}", M(1, 3, 2, 4).Join(M(4, 6, 7, 9)).ToString());
    }

    [Fact]
    public void ChangedLineCount_IsMaxOfSides()
    {
        Assert.Equal(5, M(1, 3, 1, 6).ChangedLineCount);
    }

    [Fact]
    public void Flip_SwapsOriginalAndModified()
    {
        Assert.Equal("{[2,4)->[1,3)}", M(1, 3, 2, 4).Flip().ToString());
    }
}

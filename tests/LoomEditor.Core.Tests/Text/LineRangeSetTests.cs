using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Text;

public class LineRangeSetTests
{
    private static LineRangeSet Set(params (int, int)[] r) => new([.. r.Select(x => new LineRange(x.Item1, x.Item2))]);

    // Monaco: 'addRange'
    [Fact]
    public void AddRange_MergesTouching()
    {
        LineRangeSet set = new();
        set.AddRange(new LineRange(2, 3));
        set.AddRange(new LineRange(3, 4));
        set.AddRange(new LineRange(10, 20));
        Assert.Equal("[2,4), [10,20)", set.ToString());

        set.AddRange(new LineRange(3, 21));
        Assert.Equal("[2,21)", set.ToString());
    }

    // Monaco: 'getUnion'
    [Fact]
    public void GetUnion_Merges()
    {
        LineRangeSet union = Set((2, 3), (5, 7), (10, 20)).GetUnion(Set((3, 4), (6, 8), (9, 11)));
        Assert.Equal("[2,4), [5,8), [9,20)", union.ToString());
    }

    // Monaco: 'intersects'
    [Fact]
    public void Intersects_Ranges()
    {
        LineRangeSet set = Set((2, 3), (5, 7), (10, 20));
        Assert.False(set.Intersects(new LineRange(1, 2)));
        Assert.True(set.Intersects(new LineRange(1, 3)));
        Assert.False(set.Intersects(new LineRange(3, 5)));
    }

    [Fact]
    public void Contains_Line()
    {
        LineRangeSet set = Set((2, 3), (5, 7));
        Assert.True(set.Contains(2));
        Assert.False(set.Contains(3));
        Assert.True(set.Contains(6));
        Assert.False(set.Contains(1));
    }

    [Fact]
    public void GetIntersection_SubtractFrom_GetWithDelta()
    {
        LineRangeSet set = Set((2, 6), (10, 20));
        Assert.Equal("[4,6), [10,12)", set.GetIntersection(Set((4, 12))).ToString());
        Assert.Equal("[1,2), [6,10), [20,25)", set.SubtractFrom(new LineRange(1, 25)).ToString());
        Assert.Equal("[1,3)", new LineRangeSet().SubtractFrom(new LineRange(1, 3)).ToString());
        Assert.Equal("[3,7), [11,21)", set.GetWithDelta(1).ToString());
    }
}

namespace LoomEditor.Rendering.Tests;

// Monaco: getSquigglySVGData (6 x 3 zigzag)
public class UnderlineGeometryTests
{
    private const float Bottom = 20;

    [Fact]
    public void SquigglePoints_ZeroWidth_Empty() => Assert.Empty(UnderlineGeometry.SquigglePoints(5, 0, Bottom));

    [Fact]
    public void SquigglePoints_StartsAtLeftAndEndsAtRight()
    {
        var points = UnderlineGeometry.SquigglePoints(5, 10, Bottom);

        Assert.Equal(5, points[0].X);
        Assert.Equal(15, points[^1].X);
    }

    [Fact]
    public void SquigglePoints_StaysBetweenPeakAndTrough() =>
        Assert.All(UnderlineGeometry.SquigglePoints(0, 50, Bottom), p => Assert.InRange(p.Y, Bottom - 2.3f - 0.001f, Bottom - 0.7f + 0.001f));

    [Fact]
    public void SquigglePoints_TroughAndPeak()
    {
        var points = UnderlineGeometry.SquigglePoints(0, 12, Bottom);

        Assert.Contains(points, p => Math.Abs(p.X - 1.8f) < 0.001f && Math.Abs(p.Y - (Bottom - 0.7f)) < 0.001f);
        Assert.Contains(points, p => Math.Abs(p.X - 4.8f) < 0.001f && Math.Abs(p.Y - (Bottom - 2.3f)) < 0.001f);
        Assert.Contains(points, p => Math.Abs(p.X - 7.8f) < 0.001f && Math.Abs(p.Y - (Bottom - 0.7f)) < 0.001f);
    }

    [Fact]
    public void SquigglePoints_StartHeight() =>
        Assert.Equal(Bottom - 0.7f - 1.6f * 0.6f, UnderlineGeometry.SquigglePoints(0, 12, Bottom)[0].Y, 0.001f);

    [Fact]
    public void DotPositions_ThreeDots() =>
        Assert.Equal(new (float, float, float)[] { (2f, 17.5f, 1.5f), (5f, 17.5f, 1.5f), (8f, 17.5f, 1.5f) }, UnderlineGeometry.DotPositions(2, Bottom));
}

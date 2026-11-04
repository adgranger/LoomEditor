namespace LoomEditor.Rendering.Tests;

public class LineLayoutClustersTests
{
    [Fact]
    public void NearestOffset_LeftHalf_ReturnsStart() => Assert.Equal(2, LineLayoutClusters.NearestOffset(offset => offset * 10, 2, 1, 22));

    [Fact]
    public void NearestOffset_RightHalf_ReturnsEnd() => Assert.Equal(3, LineLayoutClusters.NearestOffset(offset => offset * 10, 2, 1, 28));

    [Fact]
    public void NearestOffset_Tie_ReturnsLowerOffset() => Assert.Equal(2, LineLayoutClusters.NearestOffset(offset => offset * 10, 2, 1, 25));

    [Fact]
    public void IsSplittableCluster_PlainCharacters_IsTrue() => Assert.True(LineLayoutClusters.IsSplittableCluster("aéb", 0, 3));

    [Fact]
    public void IsSplittableCluster_SurrogatePair_IsFalse() => Assert.False(LineLayoutClusters.IsSplittableCluster("a\U0001F600b", 1, 2));

    [Fact]
    public void IsSplittableCluster_CombiningMark_IsFalse() => Assert.False(LineLayoutClusters.IsSplittableCluster("é", 0, 2));
}

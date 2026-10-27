using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Text;

public class OffsetRangeTests
{
    // Monaco: OffsetRange.ofLength
    [Fact]
    public void OfLength_Length_StartsAtZero()
    {
        Assert.Equal(new OffsetRange(0, 5), OffsetRange.OfLength(5));
    }

    // Monaco: OffsetRange.deltaStart
    [Fact]
    public void DeltaStart_Offset_MovesOnlyTheStart()
    {
        Assert.Equal(new OffsetRange(3, 10), new OffsetRange(1, 10).DeltaStart(2));
    }

    // Monaco: OffsetRange.deltaEnd
    [Fact]
    public void DeltaEnd_Offset_MovesOnlyTheEnd()
    {
        Assert.Equal(new OffsetRange(1, 8), new OffsetRange(1, 10).DeltaEnd(-2));
    }

    // Monaco: OffsetRange.intersectsOrTouches
    [Fact]
    public void IntersectsOrTouches_TouchingRanges_True()
    {
        Assert.True(new OffsetRange(1, 3).IntersectsOrTouches(new OffsetRange(3, 5)));
        Assert.True(new OffsetRange(1, 4).IntersectsOrTouches(new OffsetRange(3, 5)));
        Assert.False(new OffsetRange(1, 3).IntersectsOrTouches(new OffsetRange(4, 5)));
    }
}

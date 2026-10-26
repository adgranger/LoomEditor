using LoomEditor.Core.Text;
using LoomEditor.View.Hover;

namespace LoomEditor.View.Tests.Hover;

public class HoverAnchorTests
{
    // Monaco: HoverRangeAnchor.equals
    [Fact]
    public void Equals_ComparesRangesOnly()
    {
        var a = new HoverAnchor(new Range(2, 3, 2, 6), 10, 20);
        var b = new HoverAnchor(new Range(2, 3, 2, 6), 99, 1);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, new HoverAnchor(new Range(2, 3, 2, 7)));
    }

    // Monaco: HoverRangeAnchor.canAdoptVisibleHover
    [Fact]
    public void CanAdoptVisibleHover_SameLineOnly()
    {
        var anchor = new HoverAnchor(new Range(2, 3, 2, 6));

        Assert.True(anchor.CanAdoptVisibleHover(anchor, new Position(2, 9)));
        Assert.False(anchor.CanAdoptVisibleHover(anchor, new Position(3, 3)));
    }
}

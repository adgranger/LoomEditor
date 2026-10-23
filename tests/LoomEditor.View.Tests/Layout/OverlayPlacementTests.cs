using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

// Monaco: ContentHoverWidget layout
public class OverlayPlacementTests
{
    [Fact]
    public void Place_Below_WhenItFits()
    {
        OverlayRect r = OverlayPlacement.Place(10, 50, 70, 100, 40, 400, 300);
        Assert.Equal(new OverlayRect(10, 70, 100, 40), r);
    }

    [Fact]
    public void Place_Above_WhenOnlyThereItFits()
    {
        OverlayRect r = OverlayPlacement.Place(10, 200, 220, 100, 60, 400, 250);
        Assert.Equal(new OverlayRect(10, 140, 100, 60), r);
    }

    [Fact]
    public void Place_MoreSpaceSide_WhenNeither()
    {
        // 100 above, 60 below: the popup is 150 high, so it goes above, cut to 100.
        OverlayRect r = OverlayPlacement.Place(0, 100, 120, 50, 150, 400, 180);
        Assert.Equal(new OverlayRect(0, 0, 50, 100), r);
    }

    [Fact]
    public void Place_Tie_PrefersBelow()
    {
        OverlayRect r = OverlayPlacement.Place(0, 50, 50, 50, 100, 400, 100);
        Assert.Equal(new OverlayRect(0, 50, 50, 50), r);
    }

    [Fact]
    public void Place_XClampedToTheRight()
    {
        Assert.Equal(300, OverlayPlacement.Place(390, 0, 10, 100, 20, 400, 300).X);
    }

    [Fact]
    public void Place_XClampedToTheLeft()
    {
        Assert.Equal(0, OverlayPlacement.Place(0, 0, 10, 100, 20, 400, 300).X);
    }

    [Fact]
    public void Place_WiderThanContainer_WidthCut()
    {
        OverlayRect r = OverlayPlacement.Place(50, 0, 10, 900, 20, 400, 300);
        Assert.Equal(400, r.Width);
        Assert.Equal(0, r.X);
    }

    [Fact]
    public void Place_PreferAbove_Mirrored()
    {
        OverlayRect above = OverlayPlacement.Place(0, 100, 120, 50, 40, 400, 300, preferBelow: false);
        Assert.Equal(new OverlayRect(0, 60, 50, 40), above);

        OverlayRect below = OverlayPlacement.Place(0, 20, 40, 50, 60, 400, 300, preferBelow: false);
        Assert.Equal(new OverlayRect(0, 40, 50, 60), below);
    }

    [Fact]
    public void Place_NegativeInput_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => OverlayPlacement.Place(-1, 0, 10, 10, 10, 100, 100));

    [Fact]
    public void Place_NaN_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => OverlayPlacement.Place(0, 0, 10, double.NaN, 10, 100, 100));
}

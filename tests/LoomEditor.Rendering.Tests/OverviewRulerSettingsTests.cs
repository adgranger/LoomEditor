namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the decorations overview ruler: all tests are new.
public class OverviewRulerSettingsTests
{
    [Fact]
    public void InitLanes_ThreeLanes_SplitsInThirds()
    {
        // 14 wide, 1 DIP of offset: 13 DIPs split into 4 + 5 + 4.
        (int[] x, int[] w) = OverviewRulerSettings.InitLanes(1, 14, 3);

        Assert.Equal([0, 1, 5, 1, 10, 1, 5, 1], x);
        Assert.Equal([0, 4, 5, 9, 4, 13, 9, 13], w);
    }

    [Fact]
    public void InitLanes_TwoLanes_SplitsInHalves()
    {
        (int[] x, int[] w) = OverviewRulerSettings.InitLanes(1, 14, 2);

        Assert.Equal([0, 1, 1, 1, 7, 1, 1, 1], x);
        Assert.Equal([0, 6, 6, 6, 7, 13, 13, 13], w);
    }

    [Fact]
    public void InitLanes_OneLane_EveryLaneIsTheFullWidth()
    {
        (int[] x, int[] w) = OverviewRulerSettings.InitLanes(1, 14, 1);

        Assert.All(x.Skip(1), value => Assert.Equal(1, value));
        Assert.All(w.Skip(1), value => Assert.Equal(13, value));
    }
}

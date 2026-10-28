using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

public class EditorZoomStateTests
{
    [Fact]
    public void SetLevel_AboveMax_ClampsTo20()
    {
        var zoom = new EditorZoomState();
        zoom.SetLevel(50);
        Assert.Equal(20, zoom.Level);
    }

    [Fact]
    public void SetLevel_BelowMin_ClampsToMinus5()
    {
        var zoom = new EditorZoomState();
        zoom.SetLevel(-50);
        Assert.Equal(-5, zoom.Level);
    }

    [Fact]
    public void SetLevel_Same_DoesNotFire()
    {
        var zoom = new EditorZoomState();
        int count = 0;
        zoom.Changed += (_, _) => count++;
        zoom.SetLevel(0);
        zoom.SetLevel(20);
        zoom.SetLevel(30);
        Assert.Equal(1, count);
    }

    [Fact]
    public void SetLevel_NaN_Ignored()
    {
        var zoom = new EditorZoomState();
        int count = 0;
        zoom.Changed += (_, _) => count++;
        zoom.SetLevel(double.NaN);
        Assert.Equal(0, count);
        Assert.Equal(0, zoom.Level);
    }

    [Fact]
    public void Changed_FiresOnce_WithNewLevel()
    {
        var zoom = new EditorZoomState();
        var seen = new List<double>();
        zoom.Changed += (s, _) => seen.Add(((EditorZoomState)s!).Level);
        zoom.SetLevel(3);
        Assert.Equal([3.0], seen);
    }
}

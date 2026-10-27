using LoomEditor.WinUI.Controls.DiffEditor;

namespace LoomEditor.View.Tests.DiffEditor;

public class SashLayoutTests
{
    // Monaco: SashLayout._computeSashLeft
    [Fact]
    public void ComputeSashLeft_DefaultRatio_IsMidpoint()
    {
        var layout = new SashLayout(1000, 0.5, true);

        Assert.Equal(500, layout.SashLeft);
    }

    [Fact]
    public void ComputeSashLeft_ClampsToMinimumEditorWidth()
    {
        var layout = new SashLayout(1000, 0.5, true);

        Assert.Equal(100, layout.ComputeSashLeft(0.01));
        Assert.Equal(900, layout.ComputeSashLeft(0.99));
        Assert.Equal(300, layout.ComputeSashLeft(0.3));
    }

    [Fact]
    public void ComputeSashLeft_NarrowerThanTwoMinimums_Midpoint()
    {
        var layout = new SashLayout(200, 0.5, true);

        Assert.Equal(100, layout.ComputeSashLeft(0.9));
        Assert.Equal(50, new SashLayout(100, 0.5, true).ComputeSashLeft(0.9));
    }

    [Fact]
    public void ComputeSashLeft_ResizingDisabled_AlwaysMidpoint()
    {
        var layout = new SashLayout(1000, 0.5, false);
        layout.SetSashLeft(200);

        Assert.Equal(500, layout.SashLeft);
        Assert.Equal(500, layout.ComputeSashLeft(0.2));
    }

    [Fact]
    public void ResetSash_RestoresDefaultRatio()
    {
        var layout = new SashLayout(1000, 0.5, true);
        layout.SetSashLeft(300);
        Assert.Equal(300, layout.SashLeft);

        layout.ResetSash();

        Assert.Null(layout.Ratio);
        Assert.Equal(500, layout.SashLeft);
    }
}

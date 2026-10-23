using LoomEditor.Core.Decorations;

namespace LoomEditor.Core.Tests.Decorations;

public class ModelDecorationOverviewRulerOptionsTests
{
    [Fact]
    public void OverviewRulerOptions_DefaultPosition_IsCenter()
    {
        var options = new ModelDecorationOverviewRulerOptions(DecorationColor.FromValue("#ff0000"), null);

        Assert.Equal(OverviewRulerLane.Center, options.Position);
    }

    [Fact]
    public void OverviewRulerLane_Values_MatchMonaco()
    {
        Assert.Equal([1, 2, 4, 7], new[] { (int)OverviewRulerLane.Left, (int)OverviewRulerLane.Center, (int)OverviewRulerLane.Right, (int)OverviewRulerLane.Full });
    }

    [Fact]
    public void DecorationColor_Factories_SetExactlyOneField()
    {
        Assert.Equal(new DecorationColor("#fff", null), DecorationColor.FromValue("#fff"));
        Assert.Equal(new DecorationColor(null, "editorError.foreground"), DecorationColor.FromThemeColor("editorError.foreground"));
    }

    [Fact]
    public void Register_KeepsTheOverviewRulerOptions()
    {
        var ruler = new ModelDecorationOverviewRulerOptions(DecorationColor.FromValue("#00f"), null, OverviewRulerLane.Full);
        var options = ModelDecorationOptions.Register(new ModelDecorationOptionsData { Description = "t", OverviewRuler = ruler });

        Assert.Same(ruler, options.OverviewRuler);
        Assert.Null(ModelDecorationOptions.Empty.OverviewRuler);
    }
}

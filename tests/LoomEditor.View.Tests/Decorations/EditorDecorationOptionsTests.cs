using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Themes;
using LoomEditor.View.Decorations;

namespace LoomEditor.View.Tests.Decorations;

public class EditorDecorationOptionsTests
{
    [Fact]
    public void ToModelOptions_AllFields_MapsToInternalOptions()
    {
        var options = new EditorDecorationOptions
        {
            ClassName = "a",
            IsWholeLine = true,
            InlineClassName = "b",
            MarginClassName = "c",
            LinesDecorationsClassName = "d",
        };

        ModelDecorationOptions model = options.ToModelOptions();

        Assert.Equal("a", model.ClassName);
        Assert.True(model.IsWholeLine);
        Assert.Equal("b", model.InlineClassName);
        Assert.Equal("c", model.MarginClassName);
        Assert.Equal("d", model.LinesDecorationsClassName);
        Assert.Equal("host-decoration", model.Description);
        Assert.Equal(TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges, model.Stickiness);
        Assert.Null(model.OverviewRuler);
    }

    [Fact]
    public void ToModelOptions_CalledTwice_ReturnsTheSameInstance()
    {
        var options = new EditorDecorationOptions { ClassName = "a" };

        Assert.Same(options.ToModelOptions(), options.ToModelOptions());
    }

    [Fact]
    public void ToModelOptions_LightRulerColorOnly_SetsLightColorAndCenterLane()
    {
        var options = new EditorDecorationOptions { OverviewRulerColor = new ThemeColor(0x80, 0x12, 0x34, 0x56) };

        ModelDecorationOverviewRulerOptions ruler = options.ToModelOptions().OverviewRuler!;

        Assert.Equal("#12345680", ruler.Color!.Value);
        Assert.Null(ruler.DarkColor);
        Assert.Equal(OverviewRulerLane.Center, ruler.Position);
    }

    [Fact]
    public void ToModelOptions_DarkRulerColor_SetsBothColors()
    {
        var options = new EditorDecorationOptions
        {
            OverviewRulerColor = ThemeColor.FromRgb(1, 2, 3),
            OverviewRulerDarkColor = ThemeColor.FromRgb(4, 5, 6),
        };

        ModelDecorationOverviewRulerOptions ruler = options.ToModelOptions().OverviewRuler!;

        Assert.Equal("#010203FF", ruler.Color!.Value);
        Assert.Equal("#040506FF", ruler.DarkColor!.Value);
    }
}

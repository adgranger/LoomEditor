using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Tests.Model.TestHelpers;

namespace LoomEditor.Core.Tests.Decorations;

public class ModelDecorationMinimapOptionsTests
{
    private static ModelDeltaDecoration Deco(bool minimap) =>
        new(new Range(1, 1, 1, 2), ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData
        {
            Description = "test",
            Minimap = minimap ? new ModelDecorationMinimapOptions(DecorationColor.FromValue("#f00"), null, MinimapPosition.Gutter) : null,
        }));

    [Fact]
    public void MinimapOptions_KeepTheirValues()
    {
        var minimap = new ModelDecorationMinimapOptions(
            DecorationColor.FromValue("#fff"), DecorationColor.FromThemeColor("c"), MinimapPosition.Inline, MinimapSectionHeaderStyle.Underlined, "head");
        var options = ModelDecorationOptions.Register(new ModelDecorationOptionsData { Description = "t", Minimap = minimap });

        Assert.Same(minimap, options.Minimap);
        Assert.Equal([1, 2, 1, 2], new[] { (int)MinimapPosition.Inline, (int)MinimapPosition.Gutter, (int)MinimapSectionHeaderStyle.Normal, (int)MinimapSectionHeaderStyle.Underlined });
        Assert.Equal("head", minimap.SectionHeaderText);
        Assert.Null(ModelDecorationOptions.Empty.Minimap);
    }

    [Fact]
    public void DeltaDecorations_WithMinimapPosition_SetsAffectsMinimap()
    {
        TextModel model = TestTextModels.Create("abc");
        var events = new List<ModelDecorationsChangedEventArgs>();
        model.DecorationsChanged += (_, e) => events.Add(e);

        model.DeltaDecorations(0, [], [Deco(true)]);

        Assert.True(events[^1].AffectsMinimap);
    }

    [Fact]
    public void DeltaDecorations_WithoutMinimap_DoesNotSetIt()
    {
        TextModel model = TestTextModels.Create("abc");
        var events = new List<ModelDecorationsChangedEventArgs>();
        model.DecorationsChanged += (_, e) => events.Add(e);

        model.DeltaDecorations(0, [], [Deco(false)]);

        Assert.False(events[^1].AffectsMinimap);
    }
}

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Tests.Model.TestHelpers;

namespace LoomEditor.Core.Tests.Model;

public class TextModelOverviewRulerTests
{
    private static ModelDeltaDecoration Deco(Range range, bool ruler) =>
        new(range, ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData
        {
            Description = "test",
            OverviewRuler = ruler ? new ModelDecorationOverviewRulerOptions(DecorationColor.FromValue("#ff0000"), null) : null,
        }));

    [Fact]
    public void GetOverviewRulerDecorations_OnlyDecorationsWithARulerColor()
    {
        TextModel model = TestTextModels.Create("abc\ndef");
        string[] ids = model.DeltaDecorations(0, [], [Deco(new Range(1, 1, 1, 2), true), Deco(new Range(2, 1, 2, 2), false)]);

        var result = model.GetOverviewRulerDecorations();

        Assert.Equal([ids[0]], result.Select(d => d.Id));
    }

    [Fact]
    public void GetOverviewRulerDecorations_NullColor_IsIgnored()
    {
        TextModel model = TestTextModels.Create("abc");
        model.DeltaDecorations(0, [], [new ModelDeltaDecoration(new Range(1, 1, 1, 2), ModelDecorationOptions.CreateDynamic(
            new ModelDecorationOptionsData { Description = "t", OverviewRuler = new ModelDecorationOverviewRulerOptions(null, null) }))]);

        Assert.Empty(model.GetOverviewRulerDecorations());
    }

    [Fact]
    public void GetOverviewRulerDecorations_FiltersByOwner()
    {
        TextModel model = TestTextModels.Create("abc");
        model.DeltaDecorations(1, [], [Deco(new Range(1, 1, 1, 2), true)]);
        string[] two = model.DeltaDecorations(2, [], [Deco(new Range(1, 2, 1, 3), true)]);

        Assert.Equal([two[0]], model.GetOverviewRulerDecorations(2).Select(d => d.Id));
        Assert.Equal(2, model.GetOverviewRulerDecorations().Count);
    }

    [Fact]
    public void DeltaDecorations_WithRulerColor_SetsAffectsOverviewRuler()
    {
        TextModel model = TestTextModels.Create("abc");
        var events = new List<ModelDecorationsChangedEventArgs>();
        model.DecorationsChanged += (_, e) => events.Add(e);

        model.DeltaDecorations(0, [], [Deco(new Range(1, 1, 1, 2), true)]);

        Assert.True(events[^1].AffectsOverviewRuler);
    }

    [Fact]
    public void DeltaDecorations_WithoutRulerColor_DoesNotSetIt()
    {
        TextModel model = TestTextModels.Create("abc");
        var events = new List<ModelDecorationsChangedEventArgs>();
        model.DecorationsChanged += (_, e) => events.Add(e);

        model.DeltaDecorations(0, [], [Deco(new Range(1, 1, 1, 2), false)]);

        Assert.False(events[^1].AffectsOverviewRuler);
    }
}

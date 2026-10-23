using LoomEditor.Core.Decorations;
using LoomEditor.Core.Markers;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Model.TestHelpers;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Markers;

public class MarkerDecorationsTests
{
    private const int OwnerId = 7;

    private static MarkerData Data(MarkerSeverity severity, int sl, int sc, int el, int ec, params MarkerTag[] tags) =>
        new(severity, "message", sl, sc, el, ec, Tags: tags.Length == 0 ? null : tags);

    private static (TextModel Model, MarkerService Service, MarkerDecorations Decorations) Setup(string text, params MarkerData[] markers)
    {
        var model = TestTextModels.Create(text);
        var service = new MarkerService();
        service.Change("owner", markers);
        return (model, service, new MarkerDecorations(model, service, OwnerId));
    }

    private static List<ModelDecoration> All(TextModel model) =>
        [.. model.GetDecorationsInRange(new Range(1, 1, model.LineCount, model.GetLineMaxColumn(model.LineCount)), OwnerId)];

    private static ModelDecorationOptions Only(TextModel model) => Assert.Single(All(model)).Options;

    private static Range OnlyRange(TextModel model) => Assert.Single(All(model)).Range;

    // Monaco: MarkerDecorations.update
    [Fact]
    public void Constructor_CreatesOneDecorationPerMarker()
    {
        var (model, _, _) = Setup("let value = 1", Data(MarkerSeverity.Error, 1, 1, 1, 4), Data(MarkerSeverity.Warning, 1, 5, 1, 10));

        var decorations = All(model);
        Assert.Equal(2, decorations.Count);
        Assert.All(decorations, d =>
        {
            Assert.Equal(OwnerId, d.OwnerId);
            Assert.Equal("marker-decoration", d.Options.Description);
        });
    }

    // Monaco: MarkerDecorations._createDecorationOption
    [Theory]
    [InlineData(MarkerSeverity.Error, "squiggly-error")]
    [InlineData(MarkerSeverity.Warning, "squiggly-warning")]
    [InlineData(MarkerSeverity.Info, "squiggly-info")]
    [InlineData(MarkerSeverity.Hint, "squiggly-hint")]
    public void ClassNames_BySeverity(MarkerSeverity severity, string expected)
    {
        var (model, _, _) = Setup("let value = 1", Data(severity, 1, 1, 1, 4));

        Assert.Equal(expected, Only(model).ClassName);
    }

    // Monaco: MarkerDecorations._createDecorationOption
    [Theory]
    [InlineData(MarkerSeverity.Hint, 0)]
    [InlineData(MarkerSeverity.Info, 10)]
    [InlineData(MarkerSeverity.Warning, 20)]
    [InlineData(MarkerSeverity.Error, 30)]
    public void ZIndex_BySeverity(MarkerSeverity severity, int expected)
    {
        var (model, _, _) = Setup("let value = 1", Data(severity, 1, 1, 1, 4));

        Assert.Equal(expected, Only(model).ZIndex);
    }

    // Monaco: MarkerDecorations._createDecorationRange
    [Fact]
    public void Hint_IsTwoColumnsWide()
    {
        var (model, _, _) = Setup("let value = 1\nsecond", Data(MarkerSeverity.Hint, 1, 2, 2, 4));

        Assert.Equal(new Range(1, 2, 1, 4), OnlyRange(model));
    }

    [Fact]
    public void Hint_Unnecessary_KeepsTheRange_AndGetsTheUnnecessaryClass()
    {
        var (model, _, _) = Setup("let value = 1\nsecond", Data(MarkerSeverity.Hint, 1, 2, 2, 4, MarkerTag.Unnecessary));

        Assert.Equal(new Range(1, 2, 2, 4), OnlyRange(model));
        Assert.Equal("squiggly-unnecessary", Only(model).ClassName);
        Assert.Equal("squiggly-inline-unnecessary", Only(model).InlineClassName);
    }

    [Fact]
    public void Hint_Deprecated_HasNoClassButDeprecatedInlineClass()
    {
        var (model, _, _) = Setup("let value = 1", Data(MarkerSeverity.Hint, 1, 2, 1, 6, MarkerTag.Deprecated));

        Assert.Null(Only(model).ClassName);
        Assert.Equal("squiggly-inline-deprecated", Only(model).InlineClassName);
    }

    [Fact]
    public void EmptyRange_UsesTheWord()
    {
        var (model, _, _) = Setup("let value = 1", Data(MarkerSeverity.Error, 1, 6, 1, 6));

        Assert.Equal(new Range(1, 5, 1, 10), OnlyRange(model));
    }

    [Fact]
    public void EmptyRange_AtEndOfLine_StaysEmpty()
    {
        var (model, _, _) = Setup("let value = 1", Data(MarkerSeverity.Error, 1, 14, 1, 14));

        Assert.Equal(new Range(1, 14, 1, 14), OnlyRange(model));
    }

    [Fact]
    public void WholeLineMarker_StartsAtFirstNonWhitespace()
    {
        var (model, _, _) = Setup("    x", Data(MarkerSeverity.Error, 1, 1, 1, int.MaxValue));

        Assert.Equal(new Range(1, 5, 1, 6), OnlyRange(model));
    }

    [Fact]
    public void Update_OnlyChangesWhatChanged()
    {
        var (model, service, decorations) = Setup("let value = 1", Data(MarkerSeverity.Error, 1, 1, 1, 4));
        var before = Assert.Single(All(model)).Id;

        service.Change("other", [Data(MarkerSeverity.Warning, 1, 5, 1, 10)]);

        Assert.Equal(2, All(model).Count);
        Assert.Contains(All(model), d => d.Id == before);
        Assert.NotNull(decorations.GetMarker(before));
    }

    [Fact]
    public void Update_RemovedMarker_RemovesDecoration()
    {
        var (model, service, _) = Setup("let value = 1", Data(MarkerSeverity.Error, 1, 1, 1, 4));

        service.Remove("owner");

        Assert.Empty(All(model));
    }

    [Fact]
    public void Update_Take500()
    {
        var markers = Enumerable.Range(0, 600).Select(_ => Data(MarkerSeverity.Error, 1, 1, 1, 4)).ToArray();
        var (model, _, _) = Setup("let value = 1", markers);

        Assert.Equal(500, All(model).Count);
    }

    [Fact]
    public void RangesFollowEdits()
    {
        var (model, _, decorations) = Setup("let value = 1", Data(MarkerSeverity.Error, 1, 5, 1, 10));

        model.ApplyEdits([TextEdit.Insert(new Position(1, 1), "xx")]);

        var live = Assert.Single(decorations.GetLiveMarkers());
        Assert.Equal(new Range(1, 7, 1, 12), live.Range);
    }

    [Fact]
    public void GetMarker_ByDecorationId()
    {
        var (model, service, decorations) = Setup("let value = 1", Data(MarkerSeverity.Error, 1, 1, 1, 4));

        Assert.Same(service.Read()[0], decorations.GetMarker(Assert.Single(All(model)).Id));
        Assert.Null(decorations.GetMarker("unknown"));
    }

    [Fact]
    public void Dispose_RemovesAllDecorations_AndUnsubscribes()
    {
        var (model, service, decorations) = Setup("let value = 1", Data(MarkerSeverity.Error, 1, 1, 1, 4));

        decorations.Dispose();
        service.Change("other", [Data(MarkerSeverity.Warning, 1, 5, 1, 10)]);

        Assert.Empty(All(model));
    }

    [Fact]
    public void OverviewRuler_AndMinimap_OnlyForInfoWarningError()
    {
        var (model, _, _) = Setup("let value = 1", Data(MarkerSeverity.Hint, 1, 1, 1, 4));
        Assert.Null(Only(model).OverviewRuler);
        Assert.Null(Only(model).Minimap);

        var (model2, _, _) = Setup("let value = 1", Data(MarkerSeverity.Warning, 1, 1, 1, 4));
        Assert.Equal("editorOverviewRuler.warningForeground", Only(model2).OverviewRuler!.Color!.ThemeColorId);
        Assert.Equal(OverviewRulerLane.Right, Only(model2).OverviewRuler!.Position);
        Assert.Equal("minimap.warningHighlight", Only(model2).Minimap!.Color!.ThemeColorId);
    }

    [Fact]
    public void SameSeverity_SharesOptionsInstance()
    {
        var (model, _, _) = Setup("let value = 1", Data(MarkerSeverity.Error, 1, 1, 1, 4), Data(MarkerSeverity.Error, 1, 5, 1, 10));

        var decorations = All(model);
        Assert.Same(decorations[0].Options, decorations[1].Options);
    }
}

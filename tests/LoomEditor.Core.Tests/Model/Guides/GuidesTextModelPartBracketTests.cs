using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Guides;
using LoomEditor.Core.Tests.Model.BracketPairs.TestHelpers;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model.Guides;

// Monaco has no unit tests of getLinesBracketGuides (it is tested through the browser).
public class GuidesTextModelPartBracketTests
{
    private static readonly BracketGuideOptions All = new(true, HorizontalGuidesState.Disabled, false);

    private static TextModel Create(string text)
    {
        var test = TestLanguageModels.Create(
            text,
            TestLanguageModels.DefaultLanguageId,
            new LanguageConfiguration { Brackets = [new CharacterPair("{", "}"), new CharacterPair("(", ")")] });
        test.Model.UpdateOptions(new TextModelUpdateOptions(TabSize: 4, IndentSize: 4));
        return test.Model;
    }

    private static List<IndentGuide>[] Guides(TextModel model, Position? active, BracketGuideOptions options, int start = 1, int? end = null) =>
        model.Guides.GetLinesBracketGuides(start, end ?? model.LineCount, active, options);

    [Fact]
    public void GetLinesBracketGuides_MultiLinePair_HasVerticalGuidesInside()
    {
        var guides = Guides(Create("{\n  a\n}"), null, All);

        Assert.Equal(1, guides[0].Single().VisibleColumn);
        Assert.Equal(-1, guides[0].Single().Column);
        Assert.Equal(1, guides[0].Single().ForWrappedLinesAfterColumn);
        Assert.Equal(1, guides[1].Single().VisibleColumn);
        Assert.Equal(-1, guides[1].Single().ForWrappedLinesAfterColumn);
        Assert.Empty(guides[2]);
        Assert.StartsWith("bracket-indent-guide lvl-0", guides[1].Single().ClassName);
    }

    [Fact]
    public void GetLinesBracketGuides_IncludeInactiveFalse_OnlyTheActivePair()
    {
        var model = Create("{\n(\n)\n}");
        var options = All with { IncludeInactive = false };

        Assert.All(Guides(model, null, options), g => Assert.Empty(g));

        var guides = Guides(model, new Position(3, 1), options);
        Assert.Empty(guides[0]);
        Assert.Single(guides[1]);
        Assert.Empty(guides[3]);
    }

    [Fact]
    public void GetLinesBracketGuides_HorizontalEnabledForActive_AddsHorizontalLinesForTheActivePairOnly()
    {
        var model = Create("    {\n  b\n    }");
        var options = new BracketGuideOptions(true, HorizontalGuidesState.EnabledForActive, false);

        var guides = Guides(model, new Position(2, 1), options);
        Assert.Contains(guides[0], g => g.HorizontalLine is { Top: false, EndColumn: 5 });
        Assert.Contains(guides[2], g => g.HorizontalLine is { Top: true, EndColumn: 5 });

        var inactive = Guides(model, null, options);
        Assert.All(inactive, g => Assert.DoesNotContain(g, x => x.HorizontalLine is not null));
    }

    [Fact]
    public void GetLinesBracketGuides_TextBeforeClosingBracket_RendersTheEndLineAtTheBottom()
    {
        var withoutText = Guides(Create("{\n  a\n}"), null, All);
        var withText = Guides(Create("{\n  a\n  x}"), null, All);

        Assert.Empty(withoutText[2]);
        Assert.Single(withoutText[1]);
        Assert.Single(withText[2]);

        var horizontal = Guides(Create("a {\n  b\n  x }"), null, new BracketGuideOptions(true, HorizontalGuidesState.Enabled, false));
        Assert.Contains(horizontal[2], g => g.HorizontalLine is { Top: false });
    }

    [Fact]
    public void GetLinesBracketGuides_SingleLinePair_OnlyAHorizontalGuide()
    {
        var model = Create("{ a }");

        Assert.All(Guides(model, null, All), g => Assert.Empty(g));

        var guides = Guides(model, null, new BracketGuideOptions(true, HorizontalGuidesState.Enabled, false));
        var guide = Assert.Single(guides[0]);
        Assert.Equal(2, guide.Column);
        Assert.Equal(-1, guide.VisibleColumn);
        Assert.Equal(new IndentGuideHorizontalLine(false, 5), guide.HorizontalLine);
    }

    [Fact]
    public void GetLinesBracketGuides_ActiveClassName_WhenHighlightActive()
    {
        var model = Create("{\n(\n)\n}");
        var options = All with { HighlightActive = true };

        var guides = Guides(model, new Position(3, 1), options);

        Assert.DoesNotContain("indent-active", guides[0].Single().ClassName);
        Assert.EndsWith(" indent-active", Assert.Single(guides[1], g => g.ClassName.Contains("lvl-1")).ClassName);

        var noHighlight = Guides(model, new Position(3, 1), All);
        Assert.All(noHighlight[1], g => Assert.DoesNotContain("indent-active", g.ClassName));
    }

    [Fact]
    public void GetLinesBracketGuides_ActivePositionOutsideTheRange_QueriesItsPairs()
    {
        var model = Create("{\n(\n)\n}\nx");
        var options = All with { IncludeInactive = false };

        // Lines 3-4 only: the active position (1,2) is on a line outside the range, inside the outer pair.
        var guides = Guides(model, new Position(1, 2), options, 3, 4);

        Assert.Single(guides[0]);
        Assert.Empty(guides[1]);
    }
}

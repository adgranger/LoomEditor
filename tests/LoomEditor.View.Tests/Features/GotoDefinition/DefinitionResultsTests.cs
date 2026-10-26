using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;
using LoomEditor.View.Features.GotoDefinition;

namespace LoomEditor.View.Tests.Features.GotoDefinition;

public class DefinitionResultsTests
{
    private static LocationLink Link(string uri, int line = 1, int column = 1) => new(uri, new Range(line, column, line, column));

    // Monaco: 'nearestReference'
    [Fact]
    public void NearestReference_PicksTheClosestInTheMostSimilarUri()
    {
        LocationLink a = Link("file:///out/obj/can");
        LocationLink b = Link("file:///out/obj/can2");
        LocationLink c = Link("file:///src/can");
        var results = new DefinitionResults([a, b, c]);

        Assert.Same(a, results.NearestReference("file:///out/obj/can", new Position(1, 1)));
        Assert.Same(b, results.NearestReference("file:///out/obj/can2", new Position(1, 1)));
        Assert.Same(c, results.NearestReference("file:///src/can", new Position(1, 1)));
        Assert.Same(b, results.NearestReference("file:///out/obj/can2/x", new Position(1, 1)));
    }

    [Fact]
    public void Sorted_ByUriThenRange()
    {
        LocationLink b2 = Link("file:///b", 2);
        LocationLink a3 = Link("file:///a", 3);
        LocationLink b1 = Link("file:///b", 1);

        var results = new DefinitionResults([b2, a3, b1]);

        Assert.Equal([a3, b1, b2], results.References);
    }

    [Fact]
    public void Deduplicated_SameUriAndRange()
    {
        var results = new DefinitionResults([Link("file:///a", 1), Link("file:///a", 1), Link("file:///a", 2)]);

        Assert.Equal(2, results.References.Count);
    }

    [Fact]
    public void FirstReference_IsTheFirstOfProviders()
    {
        LocationLink late = Link("file:///z");
        var results = new DefinitionResults([late, Link("file:///a")]);

        Assert.Same(late, results.FirstReference);
        Assert.Equal("file:///a", results.References[0].Uri);
    }

    [Fact]
    public void FirstReference_Empty_Null()
    {
        var results = new DefinitionResults([]);

        Assert.Null(results.FirstReference);
        Assert.True(results.IsEmpty);
    }

    [Fact]
    public void ReferenceAt_ContainsPosition()
    {
        var wide = new LocationLink("file:///a", new Range(2, 1, 4, 5));
        var results = new DefinitionResults([wide]);

        Assert.Same(wide, results.ReferenceAt("file:///a", new Position(3, 2)));
        Assert.Null(results.ReferenceAt("file:///a", new Position(5, 1)));
        Assert.Null(results.ReferenceAt("file:///b", new Position(3, 2)));
    }

    [Fact]
    public void AriaMessage_ZeroOneManyOneFileManyFiles()
    {
        Assert.Equal("No results found", new DefinitionResults([]).AriaMessage(null));
        Assert.Equal("Found 1 symbol in file:///a", new DefinitionResults([Link("file:///a")]).AriaMessage(null));
        Assert.Equal("Found 2 symbols in file:///a", new DefinitionResults([Link("file:///a", 1), Link("file:///a", 2)]).AriaMessage(null));
        Assert.Equal("Found 2 symbols in 2 files", new DefinitionResults([Link("file:///a"), Link("file:///b")]).AriaMessage(null));
    }

    [Fact]
    public void NearestReference_PrefersCloserPositionOnTie()
    {
        LocationLink far = Link("file:///a", 10);
        LocationLink near = Link("file:///a", 3);
        var results = new DefinitionResults([far, near]);

        Assert.Same(near, results.NearestReference("file:///a", new Position(2, 1)));
    }
}

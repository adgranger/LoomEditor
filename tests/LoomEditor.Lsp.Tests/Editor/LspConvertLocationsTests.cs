using System.Text.Json;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Lsp.Editor;

namespace LoomEditor.Lsp.Tests.Editor;

public class LspConvertLocationsTests
{
    private const string Loc = "{\"uri\":\"file:///a.cs\",\"range\":{\"start\":{\"line\":1,\"character\":2},\"end\":{\"line\":1,\"character\":5}}}";

    private static IReadOnlyList<LocationLink> Convert(string json) => LspConvert.ToLocationLinks(JsonElement.Parse(json));

    [Fact]
    public void Links_Location_ToLink()
    {
        LocationLink l = Convert(Loc).Single();

        Assert.Equal(("file:///a.cs", new Range(2, 3, 2, 6), new Range(2, 3, 2, 6), null), (l.Uri, l.Range, l.TargetSelectionRange, l.OriginSelectionRange));
    }

    [Fact]
    public void Links_LocationLinkArray()
    {
        LocationLink l = Convert("[{\"targetUri\":\"u\",\"targetRange\":{\"start\":{\"line\":0,\"character\":0},\"end\":{\"line\":9,\"character\":0}}," +
            "\"targetSelectionRange\":{\"start\":{\"line\":1,\"character\":1},\"end\":{\"line\":1,\"character\":3}}}]").Single();

        Assert.Equal(("u", new Range(1, 1, 10, 1), new Range(2, 2, 2, 4)), (l.Uri, l.Range, l.TargetSelectionRange));
    }

    [Fact]
    public void Links_RangesNotValidated()
        => Assert.Equal(new Range(1001, 5001, 1001, 5002), Convert(
            "{\"uri\":\"u\",\"range\":{\"start\":{\"line\":1000,\"character\":5000},\"end\":{\"line\":1000,\"character\":5001}}}").Single().Range);

    [Fact]
    public void Links_Origin_Converted()
    {
        LocationLink l = Convert("[{\"originSelectionRange\":{\"start\":{\"line\":0,\"character\":0},\"end\":{\"line\":0,\"character\":2}},\"targetUri\":\"u\"," +
            "\"targetRange\":{\"start\":{\"line\":0,\"character\":0},\"end\":{\"line\":0,\"character\":1}},\"targetSelectionRange\":{\"start\":{\"line\":0,\"character\":0},\"end\":{\"line\":0,\"character\":1}}}]").Single();

        Assert.Equal(new Range(1, 1, 1, 3), l.OriginSelectionRange);
    }

    [Fact]
    public void Links_Empty_Null()
    {
        Assert.Empty(LspConvert.ToLocationLinks(null));
        Assert.Empty(Convert("null"));
        Assert.Empty(Convert("[]"));
    }

    [Fact]
    public void Links_ReversedRange_Swapped()
        => Assert.Equal(new Range(1, 2, 1, 6), Convert(
            "{\"uri\":\"u\",\"range\":{\"start\":{\"line\":0,\"character\":5},\"end\":{\"line\":0,\"character\":1}}}").Single().Range);
}

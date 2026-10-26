using System.Text.Json;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.Editor;

internal static partial class LspConvert
{
    /// <summary>Converts a definition result (<c>Location</c>, <c>Location[]</c> or <c>LocationLink[]</c>).</summary>
    /// <remarks>Threading: pure, runs off the UI thread; ranges are not validated.</remarks>
    public static IReadOnlyList<LocationLink> ToLocationLinks(JsonElement? result)
    {
        List<LocationLink> links = [];
        foreach (LspLocationLink link in LspUnions.ParseDefinitionResult(result))
        {
            links.Add(new LocationLink(
                link.TargetUri,
                ToRangeUnvalidated(link.TargetRange),
                link.OriginSelectionRange is null ? null : ToRangeUnvalidated(link.OriginSelectionRange),
                ToRangeUnvalidated(link.TargetSelectionRange)));
        }

        return links;
    }
}

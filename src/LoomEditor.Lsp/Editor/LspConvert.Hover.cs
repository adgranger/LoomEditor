using System.Text.Json;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.Editor;

internal static partial class LspConvert
{
    /// <summary>Converts a <c>textDocument/hover</c> result; null when there is nothing to show.</summary>
    /// <remarks>Threading: pure, runs off the UI thread; the range is not validated.</remarks>
    public static Hover? ToHover(JsonElement? result)
    {
        if (result is not { ValueKind: JsonValueKind.Object } r || !r.TryGetProperty("contents", out JsonElement contents))
        {
            return null;
        }

        List<MarkdownString> parts = [];
        foreach (HoverContentPart part in LspUnions.ParseHoverContents(contents))
        {
            parts.Add(part.IsMarkdown ? new MarkdownString(part.Value) : MarkdownString.FromText(part.Value));
        }

        return parts.Count == 0 ? null : new Hover(parts, TryGetRange(r, "range"));
    }

    private static Range? TryGetRange(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out JsonElement e) || e.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        try
        {
            LspRange? range = e.Deserialize(LspJsonContext.Default.LspRange);
            return range is null ? null : ToRangeUnvalidated(range);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

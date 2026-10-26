using System.Text.Json;

namespace LoomEditor.Lsp.Protocol;

/// <summary>One decoded piece of hover content.</summary>
internal readonly record struct HoverContentPart(string Value, bool IsMarkdown);

/// <summary>Decoders for the union-typed values of the protocol; none throws on malformed input.</summary>
/// <remarks>Threading: pure and thread-safe.</remarks>
internal static class LspUnions
{
    private static readonly string Newline = ((char)10).ToString();

    /// <summary>Decodes <c>textDocumentSync</c>: a number is a <c>TextDocumentSyncKind</c>, an object has <c>openClose</c> and <c>change</c>.</summary>
    public static (bool OpenClose, int Change) ParseTextDocumentSync(JsonElement? value)
    {
        if (value is not { } v)
        {
            return (false, 0);
        }

        if (v.ValueKind == JsonValueKind.Number)
        {
            return v.TryGetInt32(out int n) ? (true, n) : (false, 0);
        }

        if (v.ValueKind == JsonValueKind.Object)
        {
            bool openClose = v.TryGetProperty("openClose", out JsonElement oc) && oc.ValueKind == JsonValueKind.True;
            int change = v.TryGetProperty("change", out JsonElement ch) && ch.ValueKind == JsonValueKind.Number && ch.TryGetInt32(out int c) ? c : 0;
            return (openClose, change);
        }

        return (false, 0);
    }

    /// <summary>Decodes <c>boolean | options</c>: true for <c>true</c> and for any object.</summary>
    public static bool IsEnabled(JsonElement? provider)
    {
        return provider is { ValueKind: JsonValueKind.True or JsonValueKind.Object };
    }

    /// <summary>Decodes the completion result: <c>null</c>, an item array or a <c>CompletionList</c>.</summary>
    public static IReadOnlyList<LspCompletionItem> ParseCompletionResult(JsonElement? result, out bool isIncomplete)
    {
        isIncomplete = false;
        List<LspCompletionItem> items = [];
        if (result is not { } r)
        {
            return items;
        }

        JsonElement array;
        if (r.ValueKind == JsonValueKind.Array)
        {
            array = r;
        }
        else if (r.ValueKind == JsonValueKind.Object)
        {
            isIncomplete = r.TryGetProperty("isIncomplete", out JsonElement inc) && inc.ValueKind == JsonValueKind.True;
            if (!r.TryGetProperty("items", out array) || array.ValueKind != JsonValueKind.Array)
            {
                return items;
            }
        }
        else
        {
            return items;
        }

        foreach (JsonElement element in array.EnumerateArray())
        {
            try
            {
                LspCompletionItem? item = element.Deserialize(LspJsonContext.Default.LspCompletionItem);
                if (item is not null)
                {
                    items.Add(item);
                }
            }
            catch (JsonException)
            {
                // A malformed item is skipped; the others stay usable.
            }
        }

        return items;
    }

    /// <summary>Decodes <c>Hover.contents</c> (<c>MarkedString | MarkedString[] | MarkupContent</c>); empty values are dropped.</summary>
    public static IReadOnlyList<HoverContentPart> ParseHoverContents(JsonElement contents)
    {
        List<HoverContentPart> parts = [];
        AddHoverParts(contents, parts);
        return parts;
    }

    private static void AddHoverParts(JsonElement element, List<HoverContentPart> parts)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                Add(parts, element.GetString(), true);
                break;
            case JsonValueKind.Array:
                foreach (JsonElement item in element.EnumerateArray())
                {
                    AddHoverParts(item, parts);
                }

                break;
            case JsonValueKind.Object:
                if (!TryGetString(element, "value", out string? value))
                {
                    break;
                }

                if (TryGetString(element, "language", out string? language))
                {
                    Add(parts, "```" + language + Newline + value + Newline + "```", true);
                }
                else if (TryGetString(element, "kind", out string? kind))
                {
                    Add(parts, value, kind == LspMarkupKind.Markdown);
                }

                break;
        }
    }

    private static void Add(List<HoverContentPart> parts, string? value, bool isMarkdown)
    {
        if (!string.IsNullOrEmpty(value))
        {
            parts.Add(new HoverContentPart(value, isMarkdown));
        }
    }

    private static bool TryGetString(JsonElement obj, string name, out string? value)
    {
        if (obj.TryGetProperty(name, out JsonElement e) && e.ValueKind == JsonValueKind.String)
        {
            value = e.GetString();
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>Decodes <c>documentation</c> (<c>string | MarkupContent</c>).</summary>
    public static string? ParseDocumentation(JsonElement? documentation, out bool isMarkdown)
    {
        isMarkdown = false;
        if (documentation is not { } d)
        {
            return null;
        }

        if (d.ValueKind == JsonValueKind.String)
        {
            return d.GetString();
        }

        if (d.ValueKind == JsonValueKind.Object && TryGetString(d, "value", out string? value) && TryGetString(d, "kind", out string? kind))
        {
            isMarkdown = kind == LspMarkupKind.Markdown;
            return value;
        }

        return null;
    }

    /// <summary>Decodes <c>textEdit</c> (<c>TextEdit | InsertReplaceEdit</c>).</summary>
    public static (LspRange? Insert, LspRange? Replace, string? NewText) ParseTextEdit(JsonElement? textEdit)
    {
        if (textEdit is not { ValueKind: JsonValueKind.Object } e || !TryGetString(e, "newText", out string? newText))
        {
            return (null, null, null);
        }

        if (TryGetRange(e, "range", out LspRange? range))
        {
            return (range, range, newText);
        }

        if (TryGetRange(e, "insert", out LspRange? insert) && TryGetRange(e, "replace", out LspRange? replace))
        {
            return (insert, replace, newText);
        }

        return (null, null, null);
    }

    /// <summary>Decodes the definition result: <c>null</c>, a <c>Location</c> or an array of <c>Location | LocationLink</c>.</summary>
    public static IReadOnlyList<LspLocationLink> ParseDefinitionResult(JsonElement? result)
    {
        List<LspLocationLink> links = [];
        if (result is not { } r)
        {
            return links;
        }

        if (r.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement element in r.EnumerateArray())
            {
                AddLink(element, links);
            }
        }
        else
        {
            AddLink(r, links);
        }

        return links;
    }

    private static void AddLink(JsonElement element, List<LspLocationLink> links)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        try
        {
            if (element.TryGetProperty("targetUri", out _))
            {
                LspLocationLink? link = element.Deserialize(LspJsonContext.Default.LspLocationLink);
                if (link is not null)
                {
                    links.Add(link);
                }
            }
            else
            {
                LspLocation? location = element.Deserialize(LspJsonContext.Default.LspLocation);
                if (location is not null)
                {
                    links.Add(new LspLocationLink { TargetUri = location.Uri, TargetRange = location.Range, TargetSelectionRange = location.Range });
                }
            }
        }
        catch (JsonException)
        {
            // A malformed element is skipped.
        }
    }

    private static bool TryGetRange(JsonElement obj, string name, out LspRange? range)
    {
        range = null;
        if (!obj.TryGetProperty(name, out JsonElement e) || e.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        try
        {
            range = e.Deserialize(LspJsonContext.Default.LspRange);
        }
        catch (JsonException)
        {
            return false;
        }

        return range is not null;
    }
}

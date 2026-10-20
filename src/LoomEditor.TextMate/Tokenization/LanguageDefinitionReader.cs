using System.Text.Json;
using System.Text.Json.Nodes;

namespace LoomEditor.TextMate.Tokenization;

/// <summary>Reads a <c>language.json</c> (ADR 0008 decision 6) with <see cref="JsonNode"/>: no reflection, so Native AOT safe.</summary>
/// <remarks>Threading: stateless.</remarks>
internal static class LanguageDefinitionReader
{
    /// <summary>Parses <paramref name="json"/>; <paramref name="folder"/> is the resource prefix of the language folder (<c>path</c> is relative to it).</summary>
    /// <exception cref="JsonException"><paramref name="json"/> is not valid JSON.</exception>
    /// <exception cref="FormatException">The root is not an object or <c>id</c> is missing.</exception>
    public static LanguageDefinition Read(string json, string folder)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(folder);
        if (JsonNode.Parse(json) is not JsonObject root)
        {
            throw new FormatException("language.json must be an object");
        }

        string id = AsString(root["id"]) ?? throw new FormatException("language.json has no \"id\"");
        List<GrammarDefinition> extras = [];
        if (root["grammars"] is JsonArray grammars)
        {
            foreach (JsonNode? node in grammars)
            {
                if (node is JsonObject grammarObject && ReadGrammar(grammarObject, folder) is { } extra)
                {
                    extras.Add(extra);
                }
            }
        }

        return new LanguageDefinition(
            id,
            AsStringArray(root["aliases"]) ?? [],
            AsStringArray(root["extensions"]) ?? [],
            AsStringArray(root["filenames"]) ?? [],
            ReadGrammar(root, folder),
            extras,
            AsString(root["configuration"]) is { } configuration ? folder + configuration : null);
    }

    // A grammar needs a scopeName and a path.
    private static GrammarDefinition? ReadGrammar(JsonObject node, string folder)
    {
        if (AsString(node["scopeName"]) is not { } scopeName || AsString(node["path"]) is not { } path)
        {
            return null;
        }

        return new GrammarDefinition(
            folder + path,
            scopeName,
            AsStringMap(node["embeddedLanguages"]),
            AsStringMap(node["tokenTypes"]),
            AsStringArray(node["injectTo"]),
            AsStringArray(node["balancedBracketScopes"]),
            AsStringArray(node["unbalancedBracketScopes"]));
    }

    private static string? AsString(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue(out string? text) ? text : null;

    // Like VS Code's asStringArray: not an array of strings => no value (the caller applies its default).
    private static List<string>? AsStringArray(JsonNode? node)
    {
        if (node is not JsonArray array)
        {
            return null;
        }

        var result = new List<string>(array.Count);
        foreach (JsonNode? item in array)
        {
            if (AsString(item) is not { } text)
            {
                return null;
            }

            result.Add(text);
        }

        return result;
    }

    // Entries whose value is not a string are skipped ("never hurts to be too careful").
    private static Dictionary<string, string> AsStringMap(JsonNode? node)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (node is JsonObject obj)
        {
            foreach (KeyValuePair<string, JsonNode?> entry in obj)
            {
                if (AsString(entry.Value) is { } text)
                {
                    result[entry.Key] = text;
                }
            }
        }

        return result;
    }
}

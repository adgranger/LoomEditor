// Ported from microsoft/vscode-textmate: src/parseRawGrammar.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace LoomEditor.TextMate.Parsing;

/// <summary>
/// <c>parseRawGrammar</c>: reads a grammar file into a <see cref="RawGrammar"/>. JSON is read with
/// <c>System.Text.Json</c> (comments and trailing commas allowed), not with vscode-textmate's <c>json.ts</c>, and no
/// location metadata is produced (ADR 0008, decision 3).
/// </summary>
/// <remarks>
/// <para>
/// One mapping (<see cref="FromNode"/>) walks a <see cref="JsonNode"/> tree by hand: the plist reader builds the same
/// tree. No <c>JsonSerializer</c>: a raw grammar has no fixed shape (<c>applyEndPatternLast</c> is a boolean or a
/// number, captures are keyed by numbers). Unknown properties, and properties of the wrong JSON type, are ignored, as
/// vscode-textmate reads raw objects without validation.
/// </para>
/// <para>Threading: stateless; safe on any thread.</para>
/// </remarks>
internal static class RawGrammarReader
{
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary><c>parseRawGrammar</c>: a <c>.json</c> path is read as JSON, anything else (or no path) as a plist.</summary>
    /// <exception cref="JsonException">The JSON is malformed.</exception>
    /// <exception cref="FormatException">The root is not an object.</exception>
    public static RawGrammar ParseRawGrammar(string content, string? filePath = null)
    {
        if (filePath is not null && filePath.EndsWith(".json", StringComparison.Ordinal))
        {
            return ParseJsonGrammar(content, filePath);
        }

        return ParsePlistGrammar(content, filePath);
    }

    /// <summary><c>parsePLISTGrammar</c>.</summary>
    /// <exception cref="FormatException">The plist is malformed, or its root is not a dictionary.</exception>
    public static RawGrammar ParsePlistGrammar(string contents, string? filename)
    {
        return FromNode(PlistParser.Parse(contents), filename);
    }

    /// <summary><c>parseJSONGrammar</c>.</summary>
    /// <exception cref="JsonException">The JSON is malformed.</exception>
    /// <exception cref="FormatException">The root is not an object.</exception>
    public static RawGrammar ParseJsonGrammar(string contents, string? filename)
    {
        ArgumentNullException.ThrowIfNull(contents);
        JsonNode? node = JsonNode.Parse(contents, nodeOptions: null, DocumentOptions);
        return FromNode(node, filename);
    }

    /// <summary>Maps a JSON (or plist) tree to a <see cref="RawGrammar"/>.</summary>
    /// <exception cref="FormatException">The root is not an object.</exception>
    public static RawGrammar FromNode(JsonNode? node, string? filename = null)
    {
        if (node is not JsonObject root)
        {
            throw new FormatException($"The grammar {filename ?? "(no file name)"} is not an object.");
        }

        return new RawGrammar
        {
            Repository = ReadRepository(root["repository"]),
            ScopeName = ReadString(root["scopeName"]),
            Patterns = ReadRules(root["patterns"]),
            Injections = ReadRepository(root["injections"]),
            InjectionSelector = ReadString(root["injectionSelector"]),
            FileTypes = ReadStrings(root["fileTypes"]),
            Name = ReadString(root["name"]),
            FirstLineMatch = ReadString(root["firstLineMatch"]),
        };
    }

    private static RawRule? ReadRule(JsonNode? node)
    {
        if (node is not JsonObject obj)
        {
            return null;
        }

        return new RawRule
        {
            Include = ReadString(obj["include"]),
            Name = ReadString(obj["name"]),
            ContentName = ReadString(obj["contentName"]),
            Match = ReadString(obj["match"]),
            Captures = ReadCaptures(obj["captures"]),
            Begin = ReadString(obj["begin"]),
            BeginCaptures = ReadCaptures(obj["beginCaptures"]),
            End = ReadString(obj["end"]),
            EndCaptures = ReadCaptures(obj["endCaptures"]),
            While = ReadString(obj["while"]),
            WhileCaptures = ReadCaptures(obj["whileCaptures"]),
            Patterns = ReadRules(obj["patterns"]),
            Repository = ReadRepository(obj["repository"]),
            ApplyEndPatternLast = ReadFlag(obj["applyEndPatternLast"]),
        };
    }

    private static List<RawRule>? ReadRules(JsonNode? node)
    {
        if (node is not JsonArray array)
        {
            return null;
        }

        var rules = new List<RawRule>(array.Count);
        foreach (JsonNode? item in array)
        {
            if (ReadRule(item) is RawRule rule)
            {
                rules.Add(rule);
            }
        }

        return rules;
    }

    private static RawRepository? ReadRepository(JsonNode? node)
    {
        if (node is not JsonObject obj)
        {
            return null;
        }

        var repository = new RawRepository();
        foreach (KeyValuePair<string, JsonNode?> property in obj)
        {
            if (ReadRule(property.Value) is RawRule rule)
            {
                repository[property.Key] = rule;
            }
        }

        return repository;
    }

    private static RawCaptures? ReadCaptures(JsonNode? node)
    {
        if (node is not JsonObject obj)
        {
            return null;
        }

        var captures = new RawCaptures();
        foreach (KeyValuePair<string, JsonNode?> property in obj)
        {
            if (ReadRule(property.Value) is RawRule rule)
            {
                captures[property.Key] = rule;
            }
        }

        return captures;
    }

    private static List<string>? ReadStrings(JsonNode? node)
    {
        if (node is not JsonArray array)
        {
            return null;
        }

        var strings = new List<string>(array.Count);
        foreach (JsonNode? item in array)
        {
            if (ReadString(item) is string value)
            {
                strings.Add(value);
            }
        }

        return strings;
    }

    private static string? ReadString(JsonNode? node) =>
        node is JsonValue value && value.GetValueKind() == JsonValueKind.String ? value.GetValue<string>() : null;

    // applyEndPatternLast: true, or a number other than 0.
    private static bool ReadFlag(JsonNode? node)
    {
        if (node is not JsonValue value)
        {
            return false;
        }

        return value.GetValueKind() switch
        {
            JsonValueKind.True => true,
            JsonValueKind.Number => value.TryGetValue(out double number) && number != 0,
            _ => false,
        };
    }
}

// Ported from microsoft/vscode: src/vs/workbench/contrib/codeEditor/common/languageConfigurationExtensionPoint.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using LoomEditor.Core.Languages;

namespace LoomEditor.TextMate.Tokenization;

/// <summary>
/// Reads a VS Code <c>language-configuration.json</c> into a <see cref="LanguageConfiguration"/> (Monaco:
/// <c>LanguageConfigurationFileHandler._readConfigFile</c> and <c>extractValidConfig</c>), with
/// <see cref="JsonNode"/>: no reflection, so Native AOT safe (ADR 0009).
/// </summary>
/// <remarks>
/// Behavior difference: VS Code's tolerant <c>jsonc</c> parser keeps what it could read from an invalid file; here
/// invalid JSON gives a warning and an empty configuration. Threading: stateless.
/// </remarks>
internal static partial class LanguageConfigurationReader
{
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Parses <paramref name="json"/>; every invalid entry is skipped and reported to <paramref name="warn"/>.</summary>
    /// <param name="json">The file contents (JSON with comments and trailing commas).</param>
    /// <param name="languageId">The language id, used in the warnings.</param>
    /// <param name="warn">Receives VS Code's warnings; <see cref="Debug.WriteLine(string)"/> when <see langword="null"/>.</param>
    public static LanguageConfiguration Read(string json, string languageId, Action<string>? warn = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(languageId);
        warn ??= static message => Debug.WriteLine(message);

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json, null, DocumentOptions);
        }
        catch (JsonException e)
        {
            warn($"[{languageId}]: Errors parsing language configuration: {e.Message}");
            return new LanguageConfiguration();
        }

        if (root is not JsonObject configuration)
        {
            warn($"[{languageId}]: Invalid format, JSON object expected.");
            return new LanguageConfiguration();
        }

        return ExtractValidConfig(languageId, configuration, warn);
    }

    private static LanguageConfiguration ExtractValidConfig(string languageId, JsonObject configuration, Action<string> warn)
    {
        CommentRule? comments = ExtractValidCommentRule(languageId, configuration, warn);
        List<CharacterPair>? brackets = ExtractValidBrackets(languageId, configuration, warn);
        List<AutoClosingPairConditional>? autoClosingPairs = ExtractValidAutoClosingPairs(languageId, configuration, warn);
        List<AutoClosingPair>? surroundingPairs = ExtractValidSurroundingPairs(languageId, configuration, warn);
        List<CharacterPair>? colorizedBracketPairs = ExtractValidColorizedBracketPairs(languageId, configuration, warn);
        string? autoCloseBefore = AsString(configuration["autoCloseBefore"]);
        JsonNode? wordPatternNode = configuration["wordPattern"];
        var wordPattern = IsTruthy(wordPatternNode) ? ParseRegex(languageId, "wordPattern", wordPatternNode, warn) : null;
        JsonNode? indentationRulesNode = configuration["indentationRules"];
        IndentationRule? indentationRules = IsTruthy(indentationRulesNode)
            ? MapIndentationRules(languageId, indentationRulesNode as JsonObject, warn)
            : null;
        FoldingRules? folding = null;
        JsonNode? foldingNode = configuration["folding"];
        if (IsTruthy(foldingNode))
        {
            var foldingObject = foldingNode as JsonObject;
            var rawMarkers = foldingObject?["markers"] as JsonObject;
            JsonNode? start = rawMarkers?["start"];
            JsonNode? end = rawMarkers?["end"];
            var startMarker = IsTruthy(start) ? ParseRegex(languageId, "folding.markers.start", start, warn) : null;
            var endMarker = IsTruthy(end) ? ParseRegex(languageId, "folding.markers.end", end, warn) : null;
            FoldingMarkers? markers = startMarker is not null && endMarker is not null ? new FoldingMarkers(startMarker, endMarker) : null;
            bool offSide = foldingObject?["offSide"]?.GetValueKind() == JsonValueKind.True;
            folding = new FoldingRules(offSide, markers);
        }

        List<OnEnterRule>? onEnterRules = ExtractValidOnEnterRules(languageId, configuration, warn);

        return new LanguageConfiguration
        {
            Comments = comments,
            Brackets = brackets,
            WordPattern = wordPattern,
            IndentationRules = indentationRules,
            OnEnterRules = onEnterRules,
            AutoClosingPairs = autoClosingPairs,
            SurroundingPairs = surroundingPairs,
            ColorizedBracketPairs = colorizedBracketPairs,
            AutoCloseBefore = autoCloseBefore,
            Folding = folding,
            DocComment = null,
        };
    }

    private static CommentRule? ExtractValidCommentRule(string languageId, JsonObject configuration, Action<string> warn)
    {
        if (!configuration.TryGetPropertyValue("comments", out JsonNode? sourceNode))
        {
            return null;
        }

        if (sourceNode is not JsonObject source)
        {
            warn($"[{languageId}]: language configuration: expected `comments` to be an object.");
            return null;
        }

        CommentRule? result = null;
        if (source.TryGetPropertyValue("lineComment", out JsonNode? lineComment))
        {
            if (AsString(lineComment) is { } text)
            {
                result = new CommentRule(new LineCommentConfig(text));
            }
            else if (lineComment is JsonObject lineCommentObj)
            {
                if (AsString(lineCommentObj["comment"]) is { } comment)
                {
                    bool? noIndent = lineCommentObj["noIndent"]?.GetValueKind() switch
                    {
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => null,
                    };
                    result = new CommentRule(new LineCommentConfig(comment, noIndent));
                }
                else
                {
                    warn($"[{languageId}]: language configuration: expected `comments.lineComment.comment` to be a string.");
                }
            }
            else
            {
                warn($"[{languageId}]: language configuration: expected `comments.lineComment` to be a string or an object with comment property.");
            }
        }

        if (source.TryGetPropertyValue("blockComment", out JsonNode? blockComment))
        {
            if (AsCharacterPair(blockComment) is not { } pair)
            {
                warn($"[{languageId}]: language configuration: expected `comments.blockComment` to be an array of two strings.");
            }
            else
            {
                result = (result ?? new CommentRule()) with { BlockComment = pair };
            }
        }

        return result;
    }

    private static List<CharacterPair>? ExtractValidBrackets(string languageId, JsonObject configuration, Action<string> warn)
    {
        if (!TryGetArray(languageId, configuration, "brackets", warn, out JsonArray? source))
        {
            return null;
        }

        List<CharacterPair>? result = null;
        for (int i = 0; i < source.Count; i++)
        {
            if (AsCharacterPair(source[i]) is not { } pair)
            {
                warn($"[{languageId}]: language configuration: expected `brackets[{i}]` to be an array of two strings.");
                continue;
            }

            (result ??= []).Add(pair);
        }

        return result;
    }

    private static List<AutoClosingPairConditional>? ExtractValidAutoClosingPairs(string languageId, JsonObject configuration, Action<string> warn)
    {
        if (!TryGetArray(languageId, configuration, "autoClosingPairs", warn, out JsonArray? source))
        {
            return null;
        }

        List<AutoClosingPairConditional>? result = null;
        for (int i = 0; i < source.Count; i++)
        {
            JsonNode? pair = source[i];
            if (pair is JsonArray)
            {
                if (AsCharacterPair(pair) is not { } characterPair)
                {
                    warn($"[{languageId}]: language configuration: expected `autoClosingPairs[{i}]` to be an array of two strings or an object.");
                    continue;
                }

                (result ??= []).Add(new AutoClosingPairConditional(characterPair.Open, characterPair.Close));
            }
            else
            {
                if (pair is not JsonObject pairObject)
                {
                    warn($"[{languageId}]: language configuration: expected `autoClosingPairs[{i}]` to be an array of two strings or an object.");
                    continue;
                }

                if (AsString(pairObject["open"]) is not { } open)
                {
                    warn($"[{languageId}]: language configuration: expected `autoClosingPairs[{i}].open` to be a string.");
                    continue;
                }

                if (AsString(pairObject["close"]) is not { } close)
                {
                    warn($"[{languageId}]: language configuration: expected `autoClosingPairs[{i}].close` to be a string.");
                    continue;
                }

                List<string>? notIn = null;
                if (pairObject.TryGetPropertyValue("notIn", out JsonNode? notInNode))
                {
                    notIn = AsStringArray(notInNode);
                    if (notIn is null)
                    {
                        warn($"[{languageId}]: language configuration: expected `autoClosingPairs[{i}].notIn` to be a string array.");
                        continue;
                    }
                }

                (result ??= []).Add(new AutoClosingPairConditional(open, close, notIn));
            }
        }

        return result;
    }

    private static List<AutoClosingPair>? ExtractValidSurroundingPairs(string languageId, JsonObject configuration, Action<string> warn)
    {
        if (!TryGetArray(languageId, configuration, "surroundingPairs", warn, out JsonArray? source))
        {
            return null;
        }

        List<AutoClosingPair>? result = null;
        for (int i = 0; i < source.Count; i++)
        {
            JsonNode? pair = source[i];
            if (pair is JsonArray)
            {
                if (AsCharacterPair(pair) is not { } characterPair)
                {
                    warn($"[{languageId}]: language configuration: expected `surroundingPairs[{i}]` to be an array of two strings or an object.");
                    continue;
                }

                (result ??= []).Add(new AutoClosingPair(characterPair.Open, characterPair.Close));
            }
            else
            {
                if (pair is not JsonObject pairObject)
                {
                    warn($"[{languageId}]: language configuration: expected `surroundingPairs[{i}]` to be an array of two strings or an object.");
                    continue;
                }

                if (AsString(pairObject["open"]) is not { } open)
                {
                    warn($"[{languageId}]: language configuration: expected `surroundingPairs[{i}].open` to be a string.");
                    continue;
                }

                if (AsString(pairObject["close"]) is not { } close)
                {
                    warn($"[{languageId}]: language configuration: expected `surroundingPairs[{i}].close` to be a string.");
                    continue;
                }

                (result ??= []).Add(new AutoClosingPair(open, close));
            }
        }

        return result;
    }

    private static List<CharacterPair>? ExtractValidColorizedBracketPairs(string languageId, JsonObject configuration, Action<string> warn)
    {
        if (!TryGetArray(languageId, configuration, "colorizedBracketPairs", warn, out JsonArray? source))
        {
            return null;
        }

        List<CharacterPair> result = [];
        for (int i = 0; i < source.Count; i++)
        {
            if (AsCharacterPair(source[i]) is not { } pair)
            {
                warn($"[{languageId}]: language configuration: expected `colorizedBracketPairs[{i}]` to be an array of two strings.");
                continue;
            }

            result.Add(pair);
        }

        return result;
    }

    // `typeof source === 'undefined'` → false without a warning; not an array → false with VS Code's warning.
    private static bool TryGetArray(string languageId, JsonObject configuration, string name, Action<string> warn, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out JsonArray? array)
    {
        array = null;
        if (!configuration.TryGetPropertyValue(name, out JsonNode? node))
        {
            return false;
        }

        if (node is not JsonArray nodeArray)
        {
            warn($"[{languageId}]: language configuration: expected `{name}` to be an array.");
            return false;
        }

        array = nodeArray;
        return true;
    }

    private static string? AsString(JsonNode? node) =>
        node is JsonValue value && value.GetValueKind() == JsonValueKind.String ? value.GetValue<string>() : null;

    // Monaco's isStringArr.
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

    // Monaco's isCharacterPair.
    private static CharacterPair? AsCharacterPair(JsonNode? node) =>
        AsStringArray(node) is { Count: 2 } pair ? new CharacterPair(pair[0], pair[1]) : null;

    // JavaScript truthiness of a JSON value (undefined and null are a null node).
    private static bool IsTruthy([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] JsonNode? node) => node?.GetValueKind() switch
    {
        null or JsonValueKind.Null or JsonValueKind.False => false,
        JsonValueKind.String => node.GetValue<string>().Length > 0,
        JsonValueKind.Number => node.GetValue<double>() != 0,
        _ => true,
    };
}

// Ported from microsoft/vscode: src/vs/workbench/services/themes/common/colorThemeData.ts (load, _loadColorTheme, fromExtensionTheme)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace LoomEditor.Themes;

/// <summary>
/// Loads a VS Code color theme file (<c>.json</c>, with comments and trailing commas) and the files it includes (VS
/// Code: <c>_loadColorTheme</c>, the JSON branch). Differences: <c>.tmTheme</c> files and <c>tokenColors</c> given as a
/// path are rejected (ADR 0008, decision 8), and <c>semanticTokenColors</c> and <c>semanticHighlighting</c> are ignored.
/// </summary>
/// <remarks>Threading: stateless, thread-safe.</remarks>
internal static partial class VsCodeThemeLoader
{
    private const int MaxIncludeDepth = 32;

    private static readonly JsonDocumentOptions ParseOptions = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    /// <summary>
    /// Loads the theme at <paramref name="path"/>. Included files come first, so the including file overrides them.
    /// </summary>
    /// <param name="path">The path of the theme file, with <c>/</c> separators.</param>
    /// <param name="uiTheme">The <c>uiTheme</c> of the theme (<c>vs</c>, <c>vs-dark</c>, <c>hc-black</c>, <c>hc-light</c>); default <c>vs-dark</c>.</param>
    /// <param name="readFile">Reads a file by path (relative includes are joined to the folder of the including file); <see langword="null"/> when it does not exist.</param>
    /// <exception cref="FormatException">A file is missing, is not valid JSON or is not a valid theme.</exception>
    public static VsCodeTheme Load(string path, string? uiTheme, Func<string, string?> readFile)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(readFile);
        var colors = new Dictionary<string, ThemeColor>(StringComparer.Ordinal);
        var rules = new List<ThemeRule>();
        LoadFile(path, readFile, colors, rules, 0);
        return new VsCodeTheme(ThemeTypes.FromUiTheme(uiTheme ?? "vs-dark"), colors, rules);
    }

    private static void LoadFile(string path, Func<string, string?> readFile, Dictionary<string, ThemeColor> colors, List<ThemeRule> rules, int depth)
    {
        if (depth > MaxIncludeDepth)
        {
            throw new FormatException($"Theme file includes are nested too deeply (a cycle?): {path}");
        }

        if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException($"Only .json theme files are supported: {path}");
        }

        string content = readFile(path) ?? throw new FormatException($"Theme file not found: {path}");
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(content, null, ParseOptions);
        }
        catch (JsonException e)
        {
            throw new FormatException($"Problems parsing JSON theme file: {path}", e);
        }

        if (root is not JsonObject theme)
        {
            throw new FormatException($"Invalid format for JSON theme file: Object expected: {path}");
        }

        if (theme["include"] is { } include)
        {
            string includePath = include is JsonValue v && v.TryGetValue(out string? text)
                ? JoinPath(Dirname(path), text)
                : throw new FormatException($"Property 'include' is not a string: {path}");
            LoadFile(includePath, readFile, colors, rules, depth + 1);
        }

        if (theme["settings"] is JsonArray settings)
        {
            ConvertSettings(settings, colors, rules);
            return;
        }

        LoadColors(theme["colors"], path, colors);
        LoadTokenColors(theme["tokenColors"], path, rules);
    }

    private static void LoadColors(JsonNode? node, string path, Dictionary<string, ThemeColor> colors)
    {
        if (node is null)
        {
            return;
        }

        if (node is not JsonObject colorObject)
        {
            throw new FormatException($"Problem parsing color theme file: {path}. Property 'colors' is not of type 'object'.");
        }

        // new JSON color themes format
        foreach ((string colorId, JsonNode? value) in colorObject)
        {
            if (value is JsonValue v && v.TryGetValue(out string? colorVal))
            {
                if (colorVal == "default")
                {
                    // ignore colors that are set to to default
                    colors.Remove(colorId);
                }
                else
                {
                    colors[colorId] = ColorNormalization.FromHex(colorVal);
                }
            }
        }
    }

    private static void LoadTokenColors(JsonNode? node, string path, List<ThemeRule> rules)
    {
        switch (node)
        {
            case null:
                return;
            case JsonArray array:
                foreach (JsonNode? item in array)
                {
                    if (ReadRule(item) is { } rule)
                    {
                        rules.Add(rule);
                    }
                }

                return;
            default:
                throw new FormatException(
                    $"Problem parsing color theme file: {path}. Property 'tokenColors' should be an array specifying colors (a path to a TextMate theme file is not supported).");
        }
    }

    // A rule without scope or without settings has no effect (addRule: `rule.scope && rule.settings`): it is dropped.
    private static ThemeRule? ReadRule(JsonNode? node)
    {
        if (node is not JsonObject rule || rule["settings"] is not JsonObject settings)
        {
            return null;
        }

        IReadOnlyList<string>? scopes = ReadScopes(rule["scope"], out bool isInfoToken);
        return scopes is null
            ? null
            : new ThemeRule(scopes, AsString(settings["foreground"]), AsString(settings["background"]), AsString(settings["fontStyle"]), isInfoToken);
    }

    // vscode-textmate: a string scope is comma separated, after removing the leading and trailing commas.
    private static string[]? ReadScopes(JsonNode? node, out bool isInfoToken)
    {
        isInfoToken = false;
        switch (node)
        {
            case JsonValue value when value.TryGetValue(out string? text):
                if (text.Length == 0)
                {
                    return null;
                }

                isInfoToken = text == "token.info-token";
                return text.Trim(',').Split(',');
            case JsonArray array:
                string[] scopes = [.. array.Select(AsString).OfType<string>()];
                return scopes.Length == 0 ? null : scopes; // an empty list selects nothing
            default:
                return null;
        }
    }

    private static string? AsString(JsonNode? node) => node is JsonValue value && value.TryGetValue(out string? text) ? text : null;

    private static string Dirname(string path)
    {
        int slash = path.LastIndexOf('/');
        return slash < 0 ? string.Empty : path[..slash];
    }

    // resources.joinPath: `.` and `..` segments are resolved.
    private static string JoinPath(string directory, string relative)
    {
        var segments = new List<string>(directory.Split('/', StringSplitOptions.RemoveEmptyEntries));
        foreach (string part in relative.Replace('\\', '/').Split('/'))
        {
            if (part is "" or ".")
            {
                continue;
            }

            if (part == ".." && segments.Count > 0)
            {
                segments.RemoveAt(segments.Count - 1);
            }
            else if (part != "..")
            {
                segments.Add(part);
            }
        }

        return (directory.StartsWith('/') ? "/" : string.Empty) + string.Join('/', segments);
    }
}

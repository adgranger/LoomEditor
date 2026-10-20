using System.Text.Json;

namespace LoomEditor.TextMate.Tests.Golden;

/// <summary>One token of <c>tokenizeLine</c>: its range and its scopes joined by spaces.</summary>
internal sealed record GoldenToken(int Start, int End, string Scopes);

/// <summary>One token of <c>tokenizeLine2</c> decoded with a theme: where it starts, its color and its font style.</summary>
internal sealed record GoldenColor(int Start, string Foreground, string FontStyle);

/// <summary>A line of a sample with what the real vscode-textmate produced for it.</summary>
internal sealed record GoldenLine(string Text, IReadOnlyList<GoldenToken> Tokens, IReadOnlyDictionary<string, IReadOnlyList<GoldenColor>> Colors);

/// <summary>
/// A <c>sample.&lt;ext&gt;.tokens.json</c> file written by <c>eng/golden/generate.mjs</c> (never edit it by hand), and the
/// lines the maintainer agreed to skip (<c>known-differences.json</c>).
/// </summary>
internal static class GoldenFile
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "Golden");

    /// <summary>The languages that have a golden file.</summary>
    public static readonly string[] Languages = ["csharp", "typescript", "javascript", "json", "html", "css", "xml", "markdown", "python", "yaml"];

    /// <summary>Reads the golden file of <paramref name="language"/>.</summary>
    public static IReadOnlyList<GoldenLine> Read(string language)
    {
        string folder = Path.Combine(Root, language);
        string path = Directory.GetFiles(folder, "sample.*.tokens.json").Single();
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        var result = new List<GoldenLine>();
        foreach (JsonElement line in document.RootElement.GetProperty("lines").EnumerateArray())
        {
            List<GoldenToken> tokens = [.. line.GetProperty("tokens").EnumerateArray().Select(t =>
                new GoldenToken(t.GetProperty("start").GetInt32(), t.GetProperty("end").GetInt32(), t.GetProperty("scopes").GetString()!))];
            var colors = new Dictionary<string, IReadOnlyList<GoldenColor>>(StringComparer.Ordinal);
            foreach (JsonProperty property in line.EnumerateObject().Where(p => p.Name is "darkModern" or "lightModern"))
            {
                colors[property.Name] = [.. property.Value.EnumerateArray().Select(c =>
                    new GoldenColor(c.GetProperty("start").GetInt32(), c.GetProperty("foreground").GetString()!, c.GetProperty("fontStyle").GetString()!))];
            }

            result.Add(new GoldenLine(line.GetProperty("text").GetString()!, tokens, colors));
        }

        return result;
    }

    /// <summary>The 1-based line numbers of <paramref name="language"/> the maintainer agreed to skip (<c>Golden/known-differences.json</c>).</summary>
    public static HashSet<int> KnownDifferences(string language)
    {
        string path = Path.Combine(Root, "known-differences.json");
        if (!File.Exists(path))
        {
            return [];
        }

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        return [.. document.RootElement.EnumerateArray()
            .Where(e => e.GetProperty("language").GetString() == language)
            .Select(e => e.GetProperty("line").GetInt32())];
    }
}

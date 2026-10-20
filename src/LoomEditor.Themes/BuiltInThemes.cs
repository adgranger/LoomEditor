namespace LoomEditor.Themes;

/// <summary>
/// The ten default themes of VS Code (<c>extensions/theme-defaults</c>), embedded as resources. The ids, labels and
/// <c>uiTheme</c> values come from its <c>package.json</c> and <c>package.nls.json</c>, in the same order.
/// </summary>
/// <remarks>Threading: stateless, thread-safe.</remarks>
internal static class BuiltInThemes
{
    private static readonly (string Label, string UiTheme, string File)[] Entries =
    [
        ("Light 2026", "vs", "2026-light.json"),
        ("Dark 2026", "vs-dark", "2026-dark.json"),
        ("Dark+", "vs-dark", "dark_plus.json"),
        ("Dark Modern", "vs-dark", "dark_modern.json"),
        ("Light+", "vs", "light_plus.json"),
        ("Light Modern", "vs", "light_modern.json"),
        ("Dark (Visual Studio)", "vs-dark", "dark_vs.json"),
        ("Light (Visual Studio)", "vs", "light_vs.json"),
        ("Dark High Contrast", "hc-black", "hc_black.json"),
        ("Light High Contrast", "hc-light", "hc_light.json"),
    ];

    /// <summary>Loads the ten themes.</summary>
    public static IReadOnlyList<EditorTheme> LoadAll() =>
        [.. Entries.Select(e => EditorTheme.Load(ReadResource(e.File)!, e.UiTheme, e.Label, ReadResource))];

    /// <summary>Loads one theme by its display name.</summary>
    public static EditorTheme Load(string label)
    {
        (string Label, string UiTheme, string File) e = Array.Find(Entries, x => x.Label == label);
        return EditorTheme.Load(ReadResource(e.File)!, e.UiTheme, e.Label, ReadResource);
    }

    private static string? ReadResource(string file)
    {
        using Stream? stream = typeof(BuiltInThemes).Assembly.GetManifestResourceStream("Themes/BuiltIn/" + file);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

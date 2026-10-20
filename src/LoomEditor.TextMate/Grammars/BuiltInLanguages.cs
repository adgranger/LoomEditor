using LoomEditor.TextMate.Tokenization;

namespace LoomEditor.TextMate.Grammars;

/// <summary>
/// The languages bundled with LoomEditor (ADR 0008, decisions 6 and 7): one line per <c>language.json</c>, and the
/// reader of the grammar files embedded in this assembly. To add a language: see <c>docs/how-to/add-a-language.md</c>.
/// </summary>
/// <remarks>Threading: immutable after the type initializer; <see cref="ReadFile"/> is thread-safe.</remarks>
internal static class BuiltInLanguages
{
    /// <summary>The prefix of every embedded grammar resource (<c>Grammars/&lt;folder&gt;/&lt;file&gt;</c>).</summary>
    public const string ResourcePrefix = "Grammars/";

    // (folder, language file): a folder holds one language, except json/ (json and jsonc).
    private static readonly (string Folder, string File)[] Sources =
    [
        ("csharp", "language.json"),
        ("typescript", "language.json"),
        ("javascript", "language.json"),
        ("json", "language.json"),
        ("json", "jsonc.language.json"),
        ("html", "language.json"),
        ("css", "language.json"),
        ("xml", "language.json"),
        ("markdown", "language.json"),
        ("python", "language.json"),
        ("yaml", "language.json"),
    ];

    /// <summary>The bundled languages, in the order of <see cref="Sources"/>.</summary>
    public static readonly IReadOnlyList<LanguageDefinition> All = Load();

    /// <summary>Registers every bundled language in <paramref name="registry"/>.</summary>
    public static void RegisterAll(LanguageRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        foreach (LanguageDefinition language in All)
        {
            registry.Register(language);
        }
    }

    /// <summary>Reads an embedded grammar file by resource name (the <c>readFile</c> of <c>TMGrammarFactory</c>); <see langword="null"/> when it does not exist.</summary>
    public static string? ReadFile(string location)
    {
        using Stream? stream = typeof(BuiltInLanguages).Assembly.GetManifestResourceStream(location);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static LanguageDefinition[] Load()
    {
        var result = new LanguageDefinition[Sources.Length];
        for (int i = 0; i < Sources.Length; i++)
        {
            (string folder, string file) = Sources[i];
            string prefix = $"{ResourcePrefix}{folder}/";
            string json = ReadFile(prefix + file) ?? throw new InvalidOperationException($"Missing resource {prefix}{file}");
            result[i] = LanguageDefinitionReader.Read(json, prefix);
        }

        return result;
    }
}

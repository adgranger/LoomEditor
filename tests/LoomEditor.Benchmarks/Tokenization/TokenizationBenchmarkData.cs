using LoomEditor.Core.Model;
using LoomEditor.Core.Threading;
using LoomEditor.Core.Tokens;
using LoomEditor.TextMate.Grammars;
using LoomEditor.TextMate.Tokenization;
using LoomEditor.View.Highlighting;

namespace LoomEditor.Benchmarks.Tokenization;

/// <summary>The golden samples (copied next to the assembly, see the csproj) repeated to the sizes the benchmarks need.</summary>
internal static class TokenizationBenchmarkData
{
    /// <summary>The lines of the sample of <paramref name="language"/>, repeated until there are <paramref name="lineCount"/>.</summary>
    public static string[] Lines(string language, int lineCount)
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "Golden", language);
        string path = Directory.GetFiles(folder, "sample.*").Single(f => !f.EndsWith(".tokens.json", StringComparison.Ordinal));
        string[] sample = File.ReadAllLines(path);
        var lines = new string[lineCount];
        for (int i = 0; i < lineCount; i++)
        {
            lines[i] = sample[i % sample.Length];
        }

        return lines;
    }

    /// <summary>A model of <paramref name="lineCount"/> lines of <paramref name="language"/> and the highlighter that tokenizes it.</summary>
    public static (TextModel Model, SyntaxHighlighter Highlighter) CreateHighlightedModel(string language, int lineCount, IUiDispatcher? dispatcher)
    {
        var registry = new TokenizationRegistry();
        var languages = new LanguageRegistry();
        BuiltInLanguages.RegisterAll(languages);
        var model = new TextModel(string.Join('\n', Lines(language, lineCount)), DefaultEndOfLine.LF, null, registry, languages.Codec, NoopTokenizationScheduler.Instance);
        return (model, new SyntaxHighlighter(model, registry, languages, model.LanguageConfigurationRegistry, dispatcher));
    }
}

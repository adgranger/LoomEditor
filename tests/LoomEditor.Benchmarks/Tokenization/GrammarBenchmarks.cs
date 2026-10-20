using BenchmarkDotNet.Attributes;
using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Grammars;
using LoomEditor.TextMate.Oniguruma;
using LoomEditor.TextMate.Tokenization;
using LoomEditor.Themes;
using LoomEditor.View.Highlighting;

namespace LoomEditor.Benchmarks.Tokenization;

/// <summary>Raw grammar speed: <c>TokenizeLine2</c> over each golden sample repeated to 10 000 lines (lines per second = 10 000 / Mean).</summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
public class GrammarBenchmarks : IDisposable
{
    private const int LineCount = 10_000;

    private TMGrammarFactory _factory = null!;
    private IGrammar _grammar = null!;
    private string[] _lines = null!;

    /// <summary>The language of the sample.</summary>
    [Params("csharp", "typescript", "javascript", "json", "html", "css", "xml", "markdown", "python", "yaml")]
    public string Language { get; set; } = "csharp";

    [GlobalSetup]
    public void Setup()
    {
        var languages = new LanguageRegistry();
        BuiltInLanguages.RegisterAll(languages);
        _factory = new TMGrammarFactory(languages.CreateGrammarDefinitions(), BuiltInLanguages.ReadFile, new RawGrammarCache(), new OnigurumaEngine());
        _factory.SetTheme(ThemeToTextMate.ToRawTheme(EditorTheme.Dark), ThemeToTextMate.ToTextColorMap(EditorTheme.Dark));
        _grammar = _factory.CreateGrammar(Language, languages.Codec.EncodeLanguageId(Language)).Grammar ?? throw new InvalidOperationException(Language);
        _lines = TokenizationBenchmarkData.Lines(Language, LineCount);
    }

    [GlobalCleanup]
    public void Cleanup() => Dispose();

    /// <inheritdoc />
    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Tokenizes every line, each from the state the previous one ended in.</summary>
    [Benchmark]
    public int TokenizeLine2()
    {
        StateStack state = StateStack.Initial;
        int tokens = 0;
        foreach (string line in _lines)
        {
            TokenizeLineResult2 result = _grammar.TokenizeLine2(line, state);
            state = result.RuleStack;
            tokens += result.Tokens.Length;
        }

        return tokens;
    }
}

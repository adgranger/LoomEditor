using BenchmarkDotNet.Attributes;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Tokens;
using LoomEditor.View.Highlighting;

namespace LoomEditor.Benchmarks.Tokenization;

/// <summary>
/// The UI-thread cost of tokenizing the 60 visible lines in the middle of a 100 000-line C# file, with no worker (the
/// heuristic path: <c>findLikelyRelevantLines</c> then <c>TokenizeLine2</c>). Each call first resets the tokens.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 5)]
public class ViewportTokenizationBenchmarks
{
    private const int LineCount = 100_000;
    private const int VisibleLines = 60;

    private TextModel _model = null!;
    private SyntaxHighlighter _highlighter = null!;
    private IAttachedView _view = null!;
    private int _first;

    [GlobalSetup]
    public void Setup()
    {
        (_model, _highlighter) = TokenizationBenchmarkData.CreateHighlightedModel("csharp", LineCount, dispatcher: null);
        _highlighter.SetLanguage("csharp");
        _view = _model.AttachView();
        _first = LineCount / 2;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _view.Dispose();
        _highlighter.Dispose();
    }

    /// <summary>Resets the tokens, then tokenizes the visible lines as <c>visibleLinesStabilized</c> does.</summary>
    [Benchmark]
    public int SetVisibleLines()
    {
        _model.Tokenization.ResetTokenization();
        _view.SetVisibleLines([(_first, _first + VisibleLines - 1)], stabilized: true);
        return _model.Tokenization.GetLineTokens(_first).GetForeground(0);
    }
}

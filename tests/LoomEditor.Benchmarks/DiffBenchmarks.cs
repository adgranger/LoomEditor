using BenchmarkDotNet.Attributes;
using LoomEditor.Core.Diff;
using LoomEditor.Core.Model;

namespace LoomEditor.Benchmarks;

/// <summary>
/// The diff computation of the diff editor on generated C#-like texts where 1 % of the lines changed (fixed seed): the default computer
/// with the default options, with moves, with <c>IgnoreTrimWhitespace</c> off, and the document diff provider on two models.
/// Targets of the card: 2 000 lines under 100 ms, 20 000 lines under 1 s.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
public class DiffBenchmarks
{
    private const int MaxComputationTimeMs = 5000;

    private readonly DefaultLinesDiffComputer _computer = new();
    private readonly DocumentDiffProvider _provider = new();
    private string[] _original = null!;
    private string[] _modified = null!;
    private TextModel _originalModel = null!;
    private TextModel _modifiedModel = null!;

    /// <summary>The number of lines of each text.</summary>
    [Params(200, 2_000, 20_000)]
    public int Lines { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        _original = new string[Lines];
        _modified = new string[Lines];
        for (int i = 0; i < Lines; i++)
        {
            string line = $"    var value{i} = Compute({i % 97}, {i * 31 % 1013}); // generated line {i}";
            _original[i] = line;
            _modified[i] = random.Next(100) == 0 ? line.Replace("Compute", "Evaluate", StringComparison.Ordinal) + " // changed" : line;
        }

        _originalModel = new TextModel(string.Join('\n', _original));
        _modifiedModel = new TextModel(string.Join('\n', _modified));
    }

    /// <summary>The default options of the diff editor.</summary>
    [Benchmark]
    public int ComputeDiff() => _computer.ComputeDiff(_original, _modified, new LinesDiffComputerOptions(true, MaxComputationTimeMs, false)).Changes.Count;

    /// <summary>With the detection of moved blocks.</summary>
    [Benchmark]
    public int ComputeDiff_WithMoves() => _computer.ComputeDiff(_original, _modified, new LinesDiffComputerOptions(true, MaxComputationTimeMs, true)).Changes.Count;

    /// <summary>Whitespace changes at the ends of the lines count.</summary>
    [Benchmark]
    public int ComputeDiff_IgnoreWhitespaceOff() => _computer.ComputeDiff(_original, _modified, new LinesDiffComputerOptions(false, MaxComputationTimeMs, false)).Changes.Count;

    /// <summary>The provider of the view model: copies the lines of the two models and computes on the pool.</summary>
    [Benchmark]
    public int DocumentDiffProvider_TwoModels() =>
        _provider.ComputeDiffAsync(_originalModel, _modifiedModel, new LinesDiffComputerOptions(true, MaxComputationTimeMs, false), CancellationToken.None)
            .GetAwaiter().GetResult().Changes.Count;
}

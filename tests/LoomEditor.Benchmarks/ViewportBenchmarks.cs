using BenchmarkDotNet.Attributes;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.Benchmarks;

/// <summary>
/// The per-frame CPU work of scrolling a 100 MB document, without DirectWrite: the view model, the viewport data and
/// the line reads of the renderer. The real frames are measured by the Gallery scroll test (see docs/performance.md).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
public class ViewportBenchmarks
{
    private const int CharacterWidth = 10;
    private const int LineHeight = 19;
    private const int ViewportWidth = 1600;
    private const int ViewportHeight = 1000;
    private const int ScrollStep = 57;
    private const int SuccessivePositions = 1_000;
    private const int RandomPositions = 25;

    private string _text = null!;
    private TextModel _model = null!;
    private ViewModel _viewModel = null!;
    private double[] _positions = null!;
    private int _middleLine;

    [GlobalSetup]
    public void Setup()
    {
        _text = string.Concat(PieceTreeBenchmarkData.GenerateChunks(100 * 1024 * 1024));
        _model = new TextModel(_text);
        _viewModel = CreateViewModel(_model);

        double maxScrollTop = (double)_model.LineCount * LineHeight - ViewportHeight;
        var random = new Random(PieceTreeBenchmarkData.Seed);
        _positions = new double[SuccessivePositions + RandomPositions];
        for (int i = 0; i < SuccessivePositions; i++)
        {
            _positions[i] = i * ScrollStep;
        }

        for (int i = 0; i < RandomPositions; i++)
        {
            _positions[SuccessivePositions + i] = random.NextDouble() * maxScrollTop;
        }

        _middleLine = _model.LineCount / 2;
    }

    [GlobalCleanup]
    public void Cleanup() => _viewModel.Dispose();

    /// <summary>Opening a 100 MB text model and computing the first screen. Compare with <c>Open100MB</c>.</summary>
    [Benchmark]
    public int OpenAndLayout100MB()
    {
        var model = new TextModel(_text);
        using ViewModel viewModel = CreateViewModel(model);
        return viewModel.GetViewportData([Selection.Collapsed(new Position(1, 1))]).EndLineNumber;
    }

    /// <summary>One frame: set the scroll position, build the viewport data and read the visible lines.</summary>
    [Benchmark(OperationsPerInvoke = SuccessivePositions + RandomPositions)]
    public long ScrollFrame100MB()
    {
        long characters = 0;
        var selection = Selection.Collapsed(new Position(1, 1));
        foreach (double scrollTop in _positions)
        {
            _viewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(null, scrollTop), ScrollType.Immediate);
            ViewportData data = _viewModel.GetViewportData([selection]);
            for (int line = data.StartLineNumber; line <= data.EndLineNumber; line++)
            {
                // The renderer reads the line, or its 10,000-character prefix.
                characters += _model.GetLineContent(line).Length;
            }
        }

        return characters;
    }

    /// <summary>One insert in the middle of the document, then one frame: the layout is not rebuilt.</summary>
    [Benchmark]
    public int EditWhileScrolled()
    {
        _model.ApplyEdits([new TextEdit(new Range(_middleLine, 1, _middleLine, 1), "x\n")]);
        return _viewModel.GetViewportData([Selection.Collapsed(new Position(1, 1))]).EndLineNumber;
    }

    private static ViewModel CreateViewModel(TextModel model)
    {
        var reader = new FixedWidthReader();
        FontInfo font = FontMeasurements.Read(
            BareFontInfo.Create("Fixed", "normal", fontSize: 14, lineHeight: LineHeight, letterSpacing: 0, pixelRatio: 1),
            reader,
            pixelRatio: 1);
        var viewModel = new ViewModel(
            model,
            font,
            new EditorLayoutOptions(),
            new ViewLayoutOptions(LineHeight, ScrollBeyondLastLine: false),
            _ => new NoopDisposable(),
            TimeProvider.System);
        viewModel.SetViewportSize(ViewportWidth, ViewportHeight);
        return viewModel;
    }

    private sealed class FixedWidthReader : ICharacterWidthReader
    {
        public double GetWidth(string character, CharacterStyle style) => CharacterWidth;
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}

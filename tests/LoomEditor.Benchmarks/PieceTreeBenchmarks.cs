using System.Text;
using BenchmarkDotNet.Attributes;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Text;

namespace LoomEditor.Benchmarks;

/// <summary>Shared data generation for the piece tree benchmarks (fixed seed, so runs are comparable).</summary>
internal static class PieceTreeBenchmarkData
{
    public const int ChunkSize = 64 * 1024;
    public const int Seed = 42;

    /// <summary>Generates <paramref name="totalChars"/> characters of text (lines of 40–120 chars, LF) in 64 KB chunks.</summary>
    public static List<string> GenerateChunks(int totalChars)
    {
        var random = new Random(Seed);
        var chunks = new List<string>();
        var sb = new StringBuilder(ChunkSize);
        int produced = 0;
        while (produced < totalChars)
        {
            int lineLength = Math.Min(random.Next(40, 121), totalChars - produced - 1);
            for (int i = 0; i < lineLength; i++)
            {
                sb.Append((char)('a' + random.Next(26)));
            }

            sb.Append('\n');
            produced += lineLength + 1;
            if (sb.Length >= ChunkSize)
            {
                chunks.Add(sb.ToString());
                sb.Clear();
            }
        }

        if (sb.Length > 0)
        {
            chunks.Add(sb.ToString());
        }

        return chunks;
    }

    public static PieceTreeTextBuffer Build(List<string> chunks)
    {
        var builder = new PieceTreeTextBufferBuilder();
        foreach (string chunk in chunks)
        {
            builder.AcceptChunk(chunk);
        }

        return builder.Finish().Create(DefaultEndOfLine.LF);
    }
}

/// <summary>Opening a large document: building the buffer from 64 KB chunks.</summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
public class PieceTreeOpenBenchmarks
{
    private List<string> _chunks = null!;

    [GlobalSetup]
    public void Setup() => _chunks = PieceTreeBenchmarkData.GenerateChunks(100 * 1024 * 1024);

    [Benchmark]
    public int Open100MB() => PieceTreeBenchmarkData.Build(_chunks).GetLineCount();
}

/// <summary>Editing: random edits in a 1 MB buffer and typing at the end. A fresh buffer is built before each iteration.</summary>
[MemoryDiagnoser]
public class PieceTreeEditBenchmarks
{
    private const int EditCount = 10_000;
    private const string InsertAlphabet = "abcdefghij\n";

    private List<string> _chunks = null!;
    private PieceTreeTextBuffer _buffer = null!;

    [GlobalSetup]
    public void Setup() => _chunks = PieceTreeBenchmarkData.GenerateChunks(1024 * 1024);

    [IterationSetup]
    public void IterationSetup() => _buffer = PieceTreeBenchmarkData.Build(_chunks);

    [Benchmark]
    public int RandomEdits10K()
    {
        var random = new Random(PieceTreeBenchmarkData.Seed);
        for (int i = 0; i < EditCount; i++)
        {
            int length = _buffer.GetLength();
            int count = random.Next(1, 11);
            int offset = random.Next(0, length);
            string? text = null;
            int endOffset = offset;
            if (random.Next(2) == 0)
            {
                var chars = new char[count];
                for (int c = 0; c < count; c++)
                {
                    chars[c] = InsertAlphabet[random.Next(InsertAlphabet.Length)];
                }

                text = new string(chars);
            }
            else
            {
                endOffset = Math.Min(length, offset + count);
            }

            Apply(new Range(_buffer.GetPositionAt(offset), _buffer.GetPositionAt(endOffset)), text);
        }

        return _buffer.GetLength();
    }

    [Benchmark]
    public int TypeAtEnd10K()
    {
        for (int i = 0; i < EditCount; i++)
        {
            Position end = _buffer.GetPositionAt(_buffer.GetLength());
            Apply(new Range(end, end), "x");
        }

        return _buffer.GetLength();
    }

    private void Apply(Range range, string? text) =>
        _buffer.ApplyEdits([new ValidAnnotatedEditOperation(null, range, text, false, false, false)], false, false);
}

/// <summary>
/// The same edits as <see cref="PieceTreeEditBenchmarks"/> but calling <see cref="PieceTreeBase.Insert"/> and
/// <see cref="PieceTreeBase.Delete"/> directly, to separate the cost of the tree from the cost of <c>ApplyEdits</c>.
/// </summary>
[MemoryDiagnoser]
public class PieceTreeDirectEditBenchmarks
{
    private const int EditCount = 10_000;
    private const string InsertAlphabet = "abcdefghij\n";

    private List<string> _chunks = null!;
    private PieceTreeBase _tree = null!;

    [GlobalSetup]
    public void Setup() => _chunks = PieceTreeBenchmarkData.GenerateChunks(1024 * 1024);

    [IterationSetup]
    public void IterationSetup()
    {
        var buffers = new List<StringBuffer>(_chunks.Count);
        foreach (string chunk in _chunks)
        {
            buffers.Add(new StringBuffer(chunk, LineStarts.CreateFast(chunk)));
        }

        _tree = new PieceTreeBase(buffers, "\n", true);
    }

    [Benchmark]
    public int DirectRandomEdits10K()
    {
        var random = new Random(PieceTreeBenchmarkData.Seed);
        for (int i = 0; i < EditCount; i++)
        {
            int length = _tree.GetLength();
            int count = random.Next(1, 11);
            int offset = random.Next(0, length);
            if (random.Next(2) == 0)
            {
                var chars = new char[count];
                for (int c = 0; c < count; c++)
                {
                    chars[c] = InsertAlphabet[random.Next(InsertAlphabet.Length)];
                }

                _tree.Insert(offset, new string(chars), true);
            }
            else
            {
                _tree.Delete(offset, Math.Min(length - offset, count));
            }
        }

        return _tree.GetLength();
    }

    [Benchmark]
    public int DirectTypeAtEnd10K()
    {
        for (int i = 0; i < EditCount; i++)
        {
            _tree.Insert(_tree.GetLength(), "x", true);
        }

        return _tree.GetLength();
    }
}

/// <summary>Reading from a 100 MB buffer.</summary>
[MemoryDiagnoser]
public class PieceTreeReadBenchmarks
{
    private const int ReadCount = 100_000;

    private PieceTreeTextBuffer _buffer = null!;
    private int[] _lineNumbers = null!;
    private int[] _offsets = null!;

    [GlobalSetup]
    public void Setup()
    {
        _buffer = PieceTreeBenchmarkData.Build(PieceTreeBenchmarkData.GenerateChunks(100 * 1024 * 1024));
        var random = new Random(PieceTreeBenchmarkData.Seed);
        int lineCount = _buffer.GetLineCount();
        int length = _buffer.GetLength();
        _lineNumbers = new int[ReadCount];
        _offsets = new int[ReadCount];
        for (int i = 0; i < ReadCount; i++)
        {
            _lineNumbers[i] = random.Next(1, lineCount + 1);
            _offsets[i] = random.Next(0, length + 1);
        }
    }

    [Benchmark]
    public int GetLineContentRandom()
    {
        int total = 0;
        foreach (int lineNumber in _lineNumbers)
        {
            total += _buffer.GetLineContent(lineNumber).Length;
        }

        return total;
    }

    [Benchmark]
    public int GetPositionAtRandom()
    {
        int total = 0;
        foreach (int offset in _offsets)
        {
            total += _buffer.GetPositionAt(offset).Column;
        }

        return total;
    }
}

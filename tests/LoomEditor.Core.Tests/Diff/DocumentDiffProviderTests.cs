using LoomEditor.Core.Diff;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Diff;

public class DocumentDiffProviderTests
{
    private static readonly LinesDiffComputerOptions Options = new(IgnoreTrimWhitespace: false, MaxComputationTimeMs: 0, ComputeMoves: false);

    private static Task<DocumentDiff> Diff(TextModel a, TextModel b, ILinesDiffComputer? computer = null) =>
        DiffWith(a, b, computer, TestContext.Current.CancellationToken);

    private static Task<DocumentDiff> DiffWith(TextModel a, TextModel b, ILinesDiffComputer? computer, CancellationToken token) =>
        new DocumentDiffProvider(computer).ComputeDiffAsync(a, b, Options, token);

    private sealed class FakeComputer(Func<string[], string[], LinesDiff> compute) : ILinesDiffComputer
    {
        public LinesDiff ComputeDiff(string[] originalLines, string[] modifiedLines, LinesDiffComputerOptions options) =>
            compute(originalLines, modifiedLines);
    }

    [Fact]
    public async Task ComputeDiffAsync_IdenticalModels_IdenticalTrue()
    {
        DocumentDiff diff = await Diff(new TextModel("a\nb"), new TextModel("a\nb"));
        Assert.True(diff.Identical);
        Assert.False(diff.QuitEarly);
        Assert.Empty(diff.Changes);
    }

    [Fact]
    public async Task ComputeDiffAsync_DifferentModels_ReturnsChanges()
    {
        DocumentDiff diff = await Diff(new TextModel("a\nb\nc"), new TextModel("a\nX\nc"));
        Assert.False(diff.Identical);
        Assert.Single(diff.Changes);
    }

    [Fact]
    public async Task ComputeDiffAsync_OnlyEolDiffers_Identical()
    {
        DocumentDiff diff = await Diff(new TextModel("a\nb"), new TextModel("a\r\nb"));
        Assert.True(diff.Identical);
    }

    [Fact]
    public async Task ComputeDiffAsync_CancelledBefore_Cancelled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => DiffWith(new TextModel("a"), new TextModel("b"), null, cts.Token));
    }

    [Fact]
    public async Task ComputeDiffAsync_ModelEditedWhileRunning_UsesSnapshot()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        string[]? seen = null;
        var computer = new FakeComputer((a, _) =>
        {
            seen = a;
            entered.Set();
            release.Wait(TestContext.Current.CancellationToken);
            return new LinesDiff([], [], false);
        });
        var original = new TextModel("old1\nold2");
        Task<DocumentDiff> task = Diff(original, new TextModel("x"), computer);
        entered.Wait(TestContext.Current.CancellationToken);
        original.SetValue("new");
        release.Set();
        await task;
        Assert.Equal(["old1", "old2"], seen!);
    }

    [Fact]
    public async Task ComputeDiffAsync_QuitEarly_WhenComputerHitTimeout()
    {
        var computer = new FakeComputer((_, _) => new LinesDiff([], [], true));
        DocumentDiff diff = await Diff(new TextModel("a"), new TextModel("b"), computer);
        Assert.True(diff.QuitEarly);
    }

    [Fact]
    public async Task ComputeDiffAsync_FakeComputerThrows_TaskFaulted()
    {
        var computer = new FakeComputer((_, _) => throw new InvalidOperationException("boom"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Diff(new TextModel("a"), new TextModel("b"), computer));
    }

    [Fact]
    public async Task ComputeDiffAsync_OriginalEmpty_ModifiedNot_SingleChange()
    {
        DocumentDiff diff = await Diff(new TextModel(""), new TextModel("ab\ncde"));
        Assert.False(diff.Identical);
        DetailedLineRangeMapping change = Assert.Single(diff.Changes);
        Assert.Equal("[1,2)", change.Original.ToString());
        Assert.Equal("[1,3)", change.Modified.ToString());
        RangeMapping inner = Assert.Single(change.InnerChanges!);
        Assert.Equal(new Range(1, 1, 1, 1), inner.OriginalRange);
        Assert.Equal(new Range(1, 1, 2, 4), inner.ModifiedRange);
    }

    [Fact]
    public async Task ComputeDiffAsync_BothEmpty_Identical()
    {
        DocumentDiff diff = await Diff(new TextModel(""), new TextModel(""));
        Assert.True(diff.Identical);
        Assert.Empty(diff.Changes);
    }

    [Fact]
    public async Task ComputeDiffAsync_CancelledDuringCompute_QuitEarlyResult()
    {
        using var cts = new CancellationTokenSource();
        var computer = new FakeComputer((_, _) =>
        {
            cts.Cancel();
            return new LinesDiff([], [], false);
        });
        DocumentDiff diff = await DiffWith(new TextModel("a"), new TextModel("a"), computer, cts.Token);
        Assert.False(diff.Identical);
        Assert.True(diff.QuitEarly);
        Assert.Empty(diff.Changes);
    }

    private static DocumentDiff WithInner(Range original, Range modified) =>
        new(false, false, [new DetailedLineRangeMapping(new LineRange(1, 2), new LineRange(1, 2), [new RangeMapping(original, modified)])], []);

    private static RangeMapping Normalized(DocumentDiff diff, int originalLines, int modifiedLines) =>
        diff.Normalize(originalLines, _ => 4, modifiedLines, _ => 4).Changes[0].InnerChanges![0];

    [Fact]
    public void Normalize_FullLineInnerChange_ExtendedToNextLineStart()
    {
        RangeMapping m = Normalized(WithInner(new Range(1, 1, 1, 4), new Range(1, 1, 1, 4)), 2, 2);
        Assert.Equal(new Range(1, 1, 2, 1), m.OriginalRange);
        Assert.Equal(new Range(1, 1, 2, 1), m.ModifiedRange);
    }

    [Fact]
    public void Normalize_NotAtLineEnd_Unchanged()
    {
        RangeMapping m = Normalized(WithInner(new Range(1, 1, 1, 3), new Range(1, 1, 1, 4)), 2, 2);
        Assert.Equal(new Range(1, 1, 1, 3), m.OriginalRange);
        Assert.Equal(new Range(1, 1, 1, 4), m.ModifiedRange);
    }

    [Fact]
    public void Normalize_LastLine_Unchanged()
    {
        RangeMapping m = Normalized(WithInner(new Range(1, 1, 1, 4), new Range(1, 1, 1, 4)), 1, 2);
        Assert.Equal(new Range(1, 1, 1, 4), m.OriginalRange);
    }
}

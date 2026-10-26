using LoomEditor.View.Hover;
using LoomEditor.View.Tests.Features.TestHelpers;
using LoomEditor.View.Tests.Highlighting.TestHelpers;

namespace LoomEditor.View.Tests.Hover;

public sealed class HoverOperationTests : IDisposable
{
    private sealed record TestPart(string Name) : IHoverPart
    {
        public Range Range => new(1, 1, 1, 2);

        public int Order => 1;

        public bool IsValidForHoverAnchor(HoverAnchor anchor) => true;
    }

    private sealed class ScriptedComputer : IHoverComputer
    {
        public TaskCompletionSource<IReadOnlyList<IHoverPart>>? Pending { get; set; }

        public IReadOnlyList<IHoverPart> Sync { get; set; } = [];

        public int AsyncCalls { get; private set; }

        public CancellationToken Token { get; private set; }

        public TimeSpan? AsyncCalledAt { get; private set; }

        public Func<TimeSpan>? Clock { get; set; }

        public Task<IReadOnlyList<IHoverPart>>? ComputeAsync(HoverComputerOptions options, CancellationToken token)
        {
            AsyncCalls++;
            Token = token;
            AsyncCalledAt = Clock?.Invoke();
            return Pending?.Task;
        }

        public IReadOnlyList<IHoverPart> ComputeSync(HoverComputerOptions options) => Sync;
    }

    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(300);

    private readonly ManualEditorScheduler _scheduler = new();
    private readonly ManualUiDispatcher _dispatcher = new();
    private readonly ScriptedComputer _computer = new();
    private readonly List<HoverResult> _results = [];
    private readonly HoverOperation _operation;
    private TimeSpan _delay = Delay;

    public HoverOperationTests()
    {
        _computer.Clock = () => _scheduler.Now;
        _operation = new HoverOperation(_scheduler, _dispatcher, _computer, () => _delay);
        _operation.Result += (_, r) => _results.Add(r);
    }

    public void Dispose() => _operation.Dispose();

    private static HoverComputerOptions Options(int column = 1) =>
        new(new HoverAnchor(new Range(1, column, 1, column + 1)), HoverStartSource.Mouse);

    private static TimeSpan Ms(int ms) => TimeSpan.FromMilliseconds(ms);

    private static List<string> Names(HoverResult result) => [.. result.Value.Cast<TestPart>().Select(p => p.Name)];

    private void Advance(int ms) => _scheduler.AdvanceBy(Ms(ms));

    // Monaco: HoverOperation
    [Fact]
    public void Delayed_NoResultBeforeTheHoverDelay()
    {
        _computer.Sync = [new TestPart("m")];
        _operation.Start(HoverStartMode.Delayed, Options());

        Advance(299);

        Assert.Empty(_results);
    }

    // Monaco: HoverOperation
    [Fact]
    public void Delayed_SyncOnly_ResultAtTheDelay()
    {
        _computer.Sync = [new TestPart("m")];
        _operation.Start(HoverStartMode.Delayed, Options());

        Advance(300);

        var result = Assert.Single(_results);
        Assert.True(result.IsComplete);
        Assert.False(result.HasLoadingMessage);
        Assert.Equal(["m"], Names(result));
    }

    // Monaco: HoverOperation
    [Fact]
    public void Delayed_AsyncStartsAtHalfTheDelay()
    {
        _computer.Pending = new();
        _operation.Start(HoverStartMode.Delayed, Options());

        Advance(149);
        Assert.Equal(0, _computer.AsyncCalls);
        Advance(1);

        Assert.Equal(1, _computer.AsyncCalls);
        Assert.Equal(Ms(150), _computer.AsyncCalledAt);
    }

    // Monaco: HoverOperation
    [Fact]
    public void Delayed_AsyncCompletesBeforeTheDelay_ResultAtTheDelay_Complete()
    {
        _computer.Pending = new();
        _computer.Sync = [new TestPart("m")];
        _operation.Start(HoverStartMode.Delayed, Options());
        Advance(150);

        _computer.Pending.SetResult([new TestPart("a")]);
        _dispatcher.RunAll();
        Assert.Empty(_results);
        Advance(150);

        var result = Assert.Single(_results);
        Assert.True(result.IsComplete);
        Assert.Equal(["a", "m"], Names(result));
    }

    // Monaco: HoverOperation
    [Fact]
    public void Delayed_AsyncPending_PartialResultAtTheDelay_NotComplete()
    {
        _computer.Pending = new();
        _computer.Sync = [new TestPart("m")];
        _operation.Start(HoverStartMode.Delayed, Options());

        Advance(300);

        var result = Assert.Single(_results);
        Assert.False(result.IsComplete);
        Assert.False(result.HasLoadingMessage);
        Assert.Equal(["m"], Names(result));
    }

    // Monaco: HoverOperation
    [Fact]
    public void Delayed_AsyncCompletesLater_FiresCompleteResult()
    {
        _computer.Pending = new();
        _computer.Sync = [new TestPart("m")];
        _operation.Start(HoverStartMode.Delayed, Options());
        Advance(300);

        _computer.Pending.SetResult([new TestPart("a")]);
        _dispatcher.RunAll();

        Assert.True(_results.Count >= 2);
        var last = _results[^1];
        Assert.True(last.IsComplete);
        Assert.Equal(["m", "a"], Names(last));
    }

    // Monaco: HoverOperation
    [Fact]
    public void Delayed_AsyncPending_LoadingMessageAtThreeTimesTheDelay()
    {
        _computer.Pending = new();
        _operation.Start(HoverStartMode.Delayed, Options());
        Advance(300);

        Advance(599);
        Assert.False(_results[^1].HasLoadingMessage);
        Advance(1);

        var last = _results[^1];
        Assert.True(last.HasLoadingMessage);
        Assert.False(last.IsComplete);
    }

    // Monaco: HoverOperation.start
    [Fact]
    public void Delayed_WhileNotIdle_IsIgnored()
    {
        _computer.Sync = [new TestPart("m")];
        _operation.Start(HoverStartMode.Delayed, Options(1));

        _operation.Start(HoverStartMode.Delayed, Options(5));
        Advance(300);

        Assert.Equal(Options(1).Anchor, Assert.Single(_results).Options.Anchor);
    }

    // Monaco: HoverOperation.start
    [Fact]
    public void Immediate_FromIdle_ComputesAtOnce()
    {
        _computer.Sync = [new TestPart("m")];

        _operation.Start(HoverStartMode.Immediate, Options());

        var result = Assert.Single(_results);
        Assert.True(result.IsComplete);
        Assert.Equal(["m"], Names(result));
        Assert.Equal(1, _computer.AsyncCalls);
    }

    // Monaco: HoverOperation.start
    [Fact]
    public void Immediate_InSecondWait_RunsTheSyncPartNow()
    {
        _computer.Sync = [new TestPart("m")];
        _operation.Start(HoverStartMode.Delayed, Options());
        Advance(150);
        Assert.Empty(_results);

        _operation.Start(HoverStartMode.Immediate, Options());

        Assert.Equal(["m"], Names(Assert.Single(_results)));
        Advance(1000);
        Assert.Single(_results);
    }

    // Monaco: HoverOperation.cancel
    [Fact]
    public void Cancel_StopsEverything_AndCancelsTheToken()
    {
        _computer.Pending = new();
        _computer.Sync = [new TestPart("m")];
        _operation.Start(HoverStartMode.Delayed, Options());
        Advance(150);

        _operation.Cancel();
        Advance(2000);

        Assert.Empty(_results);
        Assert.True(_computer.Token.IsCancellationRequested);
        Assert.Equal(0, _scheduler.PendingCount);
    }

    // Monaco: HoverOperation.cancel
    [Fact]
    public void Cancel_ThenLateAsyncResult_Dropped()
    {
        _computer.Pending = new();
        _operation.Start(HoverStartMode.Delayed, Options());
        Advance(150);
        _operation.Cancel();

        _computer.Pending.SetResult([new TestPart("a")]);
        _dispatcher.RunAll();

        Assert.Empty(_results);
    }

    // Monaco: HoverOperation.dispose
    [Fact]
    public void Dispose_NeverFiresAgain()
    {
        _computer.Pending = new();
        _operation.Start(HoverStartMode.Delayed, Options());
        Advance(150);

        _operation.Dispose();
        Advance(2000);
        _operation.Start(HoverStartMode.Immediate, Options());

        Assert.Empty(_results);
    }

    // Monaco: HoverOperation.options
    [Fact]
    public void Options_AreTheOnesOfTheLastStart()
    {
        Assert.Null(_operation.Options);
        var options = Options(4);

        _operation.Start(HoverStartMode.Delayed, options);
        Assert.Same(options, _operation.Options);

        _operation.Cancel();
        Assert.Null(_operation.Options);
    }

    // Monaco: HoverOperation
    [Fact]
    public void FaultedAsync_Completes_WithSyncPartsOnly()
    {
        _computer.Pending = new();
        _computer.Sync = [new TestPart("m")];
        _operation.Start(HoverStartMode.Delayed, Options());
        Advance(300);

        _computer.Pending.SetException(new InvalidOperationException());
        _dispatcher.RunAll();

        var last = _results[^1];
        Assert.True(last.IsComplete);
        Assert.Equal(["m"], Names(last));
    }

    // Monaco: HoverOperation._hoverTime
    [Fact]
    public void DelayIsReadAtEachStart()
    {
        _computer.Sync = [new TestPart("m")];
        _operation.Start(HoverStartMode.Delayed, Options());
        Advance(300);
        _operation.Cancel();
        _results.Clear();

        _delay = Ms(100);
        _operation.Start(HoverStartMode.Delayed, Options());
        Advance(99);
        Assert.Empty(_results);
        Advance(1);

        Assert.Single(_results);
    }
}

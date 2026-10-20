using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Tests.Features.TestHelpers;

namespace LoomEditor.View.Tests.Features.Scheduling;

// New tests: Monaco's async.test.ts has none for RunOnceScheduler in the ported range.
public class RunOnceSchedulerTests
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(100);

    private readonly ManualEditorScheduler _scheduler = new();
    private int _runs;

    private RunOnceScheduler Create() => new(_scheduler, () => _runs++, Delay);

    [Fact]
    public void Schedule_DelayElapsed_RunsRunnerOnce()
    {
        RunOnceScheduler runOnce = Create();

        runOnce.Schedule();
        _scheduler.AdvanceBy(Delay - TimeSpan.FromMilliseconds(1));
        Assert.Equal(0, _runs);
        _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
        _scheduler.AdvanceBy(Delay);

        Assert.Equal(1, _runs);
    }

    [Fact]
    public void Schedule_Twice_RestartsTheDelay()
    {
        RunOnceScheduler runOnce = Create();

        runOnce.Schedule();
        _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(60));
        runOnce.Schedule();
        _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(60));
        Assert.Equal(0, _runs);
        _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(40));

        Assert.Equal(1, _runs);
    }

    [Fact]
    public void Schedule_WithDelay_UsesThatDelay()
    {
        RunOnceScheduler runOnce = Create();

        runOnce.Schedule(TimeSpan.FromMilliseconds(10));
        _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(10));

        Assert.Equal(1, _runs);
    }

    [Fact]
    public void Cancel_RunnerDoesNotRun()
    {
        RunOnceScheduler runOnce = Create();

        runOnce.Schedule();
        runOnce.Cancel();
        _scheduler.AdvanceBy(Delay);

        Assert.Equal(0, _runs);
        Assert.False(runOnce.IsScheduled);
    }

    [Fact]
    public void Flush_WhenScheduled_RunsNow()
    {
        RunOnceScheduler runOnce = Create();

        runOnce.Schedule();
        runOnce.Flush();
        _scheduler.AdvanceBy(Delay);

        Assert.Equal(1, _runs);
        Assert.False(runOnce.IsScheduled);
    }

    [Fact]
    public void Flush_WhenNotScheduled_DoesNothing()
    {
        RunOnceScheduler runOnce = Create();

        runOnce.Flush();

        Assert.Equal(0, _runs);
    }

    [Fact]
    public void IsScheduled_AfterRun_IsFalse()
    {
        RunOnceScheduler runOnce = Create();

        runOnce.Schedule();
        Assert.True(runOnce.IsScheduled);
        _scheduler.AdvanceBy(Delay);

        Assert.False(runOnce.IsScheduled);
    }

    [Fact]
    public void Dispose_RunnerNeverRuns()
    {
        RunOnceScheduler runOnce = Create();

        runOnce.Schedule();
        runOnce.Dispose();
        runOnce.Schedule();
        _scheduler.AdvanceBy(Delay);
        runOnce.Flush();

        Assert.Equal(0, _runs);
    }
}

using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Tests.Features.TestHelpers;

namespace LoomEditor.View.Tests.Features.Scheduling;

// New tests: Monaco's async.test.ts has none for TimeoutTimer in the ported range.
public class TimeoutTimerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(50);

    private readonly ManualEditorScheduler _scheduler = new();

    [Fact]
    public void CancelAndSet_ReplacesThePendingRunner()
    {
        var timer = new TimeoutTimer(_scheduler);
        var ran = new List<string>();

        timer.CancelAndSet(() => ran.Add("first"), Timeout);
        timer.CancelAndSet(() => ran.Add("second"), Timeout);
        _scheduler.AdvanceBy(Timeout);

        Assert.Equal(["second"], ran);
    }

    [Fact]
    public void SetIfNotSet_WhenSet_KeepsTheFirstRunner()
    {
        var timer = new TimeoutTimer(_scheduler);
        var ran = new List<string>();

        timer.SetIfNotSet(() => ran.Add("first"), Timeout);
        timer.SetIfNotSet(() => ran.Add("second"), Timeout);
        _scheduler.AdvanceBy(Timeout);
        timer.SetIfNotSet(() => ran.Add("third"), Timeout);
        _scheduler.AdvanceBy(Timeout);

        Assert.Equal(["first", "third"], ran);
    }

    [Fact]
    public void Cancel_RunnerDoesNotRun()
    {
        var timer = new TimeoutTimer(_scheduler);
        int runs = 0;

        timer.CancelAndSet(() => runs++, Timeout);
        timer.Cancel();
        _scheduler.AdvanceBy(Timeout);

        Assert.Equal(0, runs);
    }

    [Fact]
    public void CancelAndSet_AfterDispose_Throws()
    {
        var timer = new TimeoutTimer(_scheduler);
        timer.Dispose();

        Assert.Throws<InvalidOperationException>(() => timer.CancelAndSet(() => { }, Timeout));
        Assert.Throws<InvalidOperationException>(() => timer.SetIfNotSet(() => { }, Timeout));
    }
}

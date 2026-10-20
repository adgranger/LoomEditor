using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Tests.Features.TestHelpers;

namespace LoomEditor.View.Tests.Features.Scheduling;

// Adapted from async.test.ts > Async > Delayer: Action instead of promises, a fake clock instead of setTimeout.
public class DelayerTests
{
    private readonly ManualEditorScheduler _scheduler = new();

    [Fact]
    public void Trigger_ThreeTimes_RunsTheTaskOnce()
    {
        // Monaco: 'simple'
        int count = 0;
        var delayer = new Delayer(_scheduler, TimeSpan.Zero);
        Assert.False(delayer.IsTriggered);

        for (int i = 0; i < 3; i++)
        {
            delayer.Trigger(() => count++);
            Assert.True(delayer.IsTriggered);
        }

        _scheduler.AdvanceBy(TimeSpan.Zero);

        Assert.Equal(1, count);
        Assert.False(delayer.IsTriggered);
    }

    [Fact]
    public void Cancel_AfterTrigger_TheTaskNeverRuns()
    {
        // Monaco: 'simple cancel'
        int count = 0;
        var delayer = new Delayer(_scheduler, TimeSpan.Zero);

        delayer.Trigger(() => count++);
        Assert.True(delayer.IsTriggered);
        delayer.Cancel();
        Assert.False(delayer.IsTriggered);
        _scheduler.AdvanceBy(TimeSpan.Zero);

        Assert.Equal(0, count);
    }

    [Fact]
    public void Cancel_AfterThreeTriggers_NoTaskRuns()
    {
        // Monaco: 'cancel should cancel all calls to trigger'
        int count = 0;
        var delayer = new Delayer(_scheduler, TimeSpan.Zero);

        delayer.Trigger(() => count++);
        delayer.Trigger(() => count++);
        delayer.Trigger(() => count++);
        delayer.Cancel();
        _scheduler.AdvanceBy(TimeSpan.Zero);

        Assert.Equal(0, count);
        Assert.False(delayer.IsTriggered);
    }

    [Fact]
    public void Trigger_CancelThenTriggerAgain_OnlyTheLastTaskRuns()
    {
        // Monaco: 'trigger, cancel, then trigger again'
        int count = 0;
        var delayer = new Delayer(_scheduler, TimeSpan.Zero);

        delayer.Trigger(() => count++);
        _scheduler.AdvanceBy(TimeSpan.Zero);
        Assert.Equal(1, count);
        Assert.False(delayer.IsTriggered);

        delayer.Trigger(() => count += 10);
        delayer.Trigger(() => count += 10);
        delayer.Cancel();
        _scheduler.AdvanceBy(TimeSpan.Zero);
        Assert.Equal(1, count);
        Assert.False(delayer.IsTriggered);

        delayer.Trigger(() => count++);
        delayer.Trigger(() => count++);
        Assert.True(delayer.IsTriggered);
        _scheduler.AdvanceBy(TimeSpan.Zero);

        Assert.Equal(2, count);
        Assert.False(delayer.IsTriggered);
    }

    [Fact]
    public void Trigger_ThreeTasks_TheLastOneRuns()
    {
        // Monaco: 'last task should be the one getting called'
        var ran = new List<int>();
        var delayer = new Delayer(_scheduler, TimeSpan.Zero);

        delayer.Trigger(() => ran.Add(1));
        delayer.Trigger(() => ran.Add(2));
        delayer.Trigger(() => ran.Add(3));
        Assert.True(delayer.IsTriggered);
        _scheduler.AdvanceBy(TimeSpan.Zero);

        Assert.Equal([3], ran);
        Assert.False(delayer.IsTriggered);
    }

    [Fact]
    public void Cancel_ReleasesThePendingTask()
    {
        // Monaco: 'cancel releases the pending task'
        AssertRelease(delayer => delayer.Cancel());
    }

    [Fact]
    public void Dispose_ReleasesThePendingTask()
    {
        // Monaco: 'dispose releases the pending task'
        AssertRelease(delayer => delayer.Dispose());
    }

    private void AssertRelease(Action<Delayer> release)
    {
        var delayer = new Delayer(_scheduler, TimeSpan.Zero);
        int taskCalls = 0;

        delayer.Trigger(() => taskCalls++);
        release(delayer);
        Assert.Equal(0, _scheduler.PendingCount);
        _scheduler.AdvanceBy(TimeSpan.Zero);
        Assert.Equal(0, taskCalls);

        int result = 0;
        delayer.Trigger(() => result = 42);
        _scheduler.AdvanceBy(TimeSpan.Zero);

        Assert.Equal(42, result);
        Assert.Equal(0, taskCalls);
    }
}

using LoomEditor.View.Tests.Features.TestHelpers;

namespace LoomEditor.View.Tests.Features.Scheduling;

// New tests for the test helper itself.
public class ManualEditorSchedulerTests
{
    [Fact]
    public void AdvanceBy_RunsDueActionsInDueTimeOrder()
    {
        var scheduler = new ManualEditorScheduler();
        var ran = new List<string>();

        scheduler.Schedule(TimeSpan.FromSeconds(2), () => ran.Add("late"));
        scheduler.Schedule(TimeSpan.FromSeconds(1), () => ran.Add("early-a"));
        scheduler.Schedule(TimeSpan.FromSeconds(1), () => ran.Add("early-b"));
        scheduler.AdvanceBy(TimeSpan.FromSeconds(1));
        Assert.Equal(["early-a", "early-b"], ran);
        scheduler.AdvanceBy(TimeSpan.FromSeconds(5));

        Assert.Equal(["early-a", "early-b", "late"], ran);
        Assert.Equal(TimeSpan.FromSeconds(6), scheduler.Now);
        Assert.Equal(0, scheduler.PendingCount);
    }

    [Fact]
    public void AdvanceBy_ActionScheduledByAnAction_RunsWhenDue()
    {
        var scheduler = new ManualEditorScheduler();
        var ran = new List<TimeSpan>();

        scheduler.Schedule(TimeSpan.FromSeconds(1), () =>
        {
            ran.Add(scheduler.Now);
            scheduler.Schedule(TimeSpan.FromSeconds(1), () => ran.Add(scheduler.Now));
        });
        scheduler.AdvanceBy(TimeSpan.FromSeconds(3));

        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)], ran);
    }

    [Fact]
    public void Dispose_BeforeDue_CancelsTheAction()
    {
        var scheduler = new ManualEditorScheduler();
        int runs = 0;

        scheduler.Schedule(TimeSpan.FromSeconds(1), () => runs++).Dispose();
        scheduler.AdvanceBy(TimeSpan.FromSeconds(2));

        Assert.Equal(0, runs);
    }

    [Fact]
    public void Schedule_NegativeDelay_Throws()
    {
        var scheduler = new ManualEditorScheduler();

        Assert.Throws<ArgumentOutOfRangeException>(() => scheduler.Schedule(TimeSpan.FromTicks(-1), () => { }));
    }
}

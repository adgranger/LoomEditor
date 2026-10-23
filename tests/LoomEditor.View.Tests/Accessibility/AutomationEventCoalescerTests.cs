using LoomEditor.Core.Text;
using LoomEditor.View.Accessibility;
using LoomEditor.View.Features;
using LoomEditor.View.Tests.Accessibility.TestHelpers;
using LoomEditor.View.Tests.Features.TestHelpers;

namespace LoomEditor.View.Tests.Accessibility;

public class AutomationEventCoalescerTests
{
    private sealed class ThrowingTextSink : RecordingAutomationEventSink
    {
        public override void RaiseTextChanged() => throw new InvalidOperationException("client failure");
    }

    private static (EditorContext Context, ManualEditorScheduler Scheduler) Create(string text = "hello")
    {
        var scheduler = new ManualEditorScheduler();
        var context = new EditorContext(scheduler);
        context.Model.SetValue(text);
        context.SetSelections([new Selection(1, 1, 1, 1)]);
        scheduler.RunAll();
        return (context, scheduler);
    }

    [Fact]
    public void NoEvents_WhenNothingChanges()
    {
        (EditorContext context, ManualEditorScheduler scheduler) = Create();
        using (context)
        {
            var sink = new RecordingAutomationEventSink();
            using var coalescer = new AutomationEventCoalescer(context, sink);
            scheduler.RunAll();
            Assert.Empty(sink.Events);
        }
    }

    [Fact]
    public void SelectionChange_RaisesOneSelectionEventOnFlush()
    {
        (EditorContext context, ManualEditorScheduler scheduler) = Create();
        using (context)
        {
            var sink = new RecordingAutomationEventSink();
            using var coalescer = new AutomationEventCoalescer(context, sink);
            context.SetSelections([new Selection(1, 2, 1, 2)]);
            Assert.Empty(sink.Events);
            scheduler.RunAll();
            Assert.Equal(["selection"], sink.Events);
        }
    }

    [Fact]
    public void ManySelectionChanges_RaiseOneEvent()
    {
        (EditorContext context, ManualEditorScheduler scheduler) = Create();
        using (context)
        {
            var sink = new RecordingAutomationEventSink();
            using var coalescer = new AutomationEventCoalescer(context, sink);
            for (int column = 2; column <= 5; column++)
            {
                context.SetSelections([new Selection(1, column, 1, column)]);
            }

            scheduler.RunAll();
            Assert.Equal(["selection"], sink.Events);
        }
    }

    [Fact]
    public void TextChange_RaisesTextThenSelection()
    {
        (EditorContext context, ManualEditorScheduler scheduler) = Create();
        using (context)
        {
            var sink = new RecordingAutomationEventSink();
            using var coalescer = new AutomationEventCoalescer(context, sink);
            context.ViewModel.Type("a", "keyboard");
            scheduler.RunAll();
            Assert.Equal(["text", "selection"], sink.Events);
        }
    }

    [Fact]
    public void TextChangeOnly_RaisesOnlyText()
    {
        (EditorContext context, ManualEditorScheduler scheduler) = Create();
        using (context)
        {
            var sink = new RecordingAutomationEventSink();
            using var coalescer = new AutomationEventCoalescer(context, sink);
            context.Model.ApplyEdits([new TextEdit(new Range(1, 6, 1, 6), "!")]);
            scheduler.RunAll();
            Assert.Equal(["text"], sink.Events);
        }
    }

    [Fact]
    public void EventsAfterAFlush_ScheduleANewFlush()
    {
        (EditorContext context, ManualEditorScheduler scheduler) = Create();
        using (context)
        {
            var sink = new RecordingAutomationEventSink();
            using var coalescer = new AutomationEventCoalescer(context, sink);
            context.SetSelections([new Selection(1, 2, 1, 2)]);
            scheduler.RunAll();
            context.SetSelections([new Selection(1, 3, 1, 3)]);
            scheduler.RunAll();
            Assert.Equal(["selection", "selection"], sink.Events);
        }
    }

    [Fact]
    public void Dispose_CancelsAPendingFlush()
    {
        (EditorContext context, ManualEditorScheduler scheduler) = Create();
        using (context)
        {
            var sink = new RecordingAutomationEventSink();
            var coalescer = new AutomationEventCoalescer(context, sink);
            context.SetSelections([new Selection(1, 2, 1, 2)]);
            coalescer.Dispose();
            Assert.Equal(0, scheduler.PendingCount);
            scheduler.RunAll();
            Assert.Empty(sink.Events);
        }
    }

    [Fact]
    public void Dispose_Twice_IsSafe()
    {
        (EditorContext context, _) = Create();
        using (context)
        {
            var coalescer = new AutomationEventCoalescer(context, new RecordingAutomationEventSink());
            coalescer.Dispose();
            coalescer.Dispose();
        }
    }

    [Fact]
    public void Dispose_UnsubscribesFromTheContext()
    {
        (EditorContext context, ManualEditorScheduler scheduler) = Create();
        using (context)
        {
            var sink = new RecordingAutomationEventSink();
            new AutomationEventCoalescer(context, sink).Dispose();
            context.SetSelections([new Selection(1, 2, 1, 2)]);
            context.Model.ApplyEdits([new TextEdit(new Range(1, 6, 1, 6), "!")]);
            Assert.Equal(0, scheduler.PendingCount);
            Assert.Empty(sink.Events);
        }
    }

    [Fact]
    public void SinkThrowing_DoesNotBreakTheEditorOrTheOtherEvent()
    {
        (EditorContext context, ManualEditorScheduler scheduler) = Create();
        using (context)
        {
            var sink = new ThrowingTextSink();
            using var coalescer = new AutomationEventCoalescer(context, sink);
            context.ViewModel.Type("a", "keyboard");
            scheduler.RunAll();
            Assert.Equal(["selection"], sink.Events);

            context.ViewModel.Type("b", "keyboard");
            Assert.StartsWith("abhello", context.Model.GetValue());
        }
    }
}

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Events;

// Monaco has no test for the dispatcher: all tests are new.
public class ViewModelEventDispatcherTests
{
    private static Selection Caret(int line, int column) => Selection.Collapsed(new Position(line, column));

    private static CursorStateChangedEvent StateChanged(Selection[]? old, Selection[] now, int oldVersion = 1, int version = 1, string source = "test") =>
        new(old, now, oldVersion, version, source, CursorChangeReason.Explicit, false);

    private sealed class RecordingHandler : IViewEventHandler
    {
        public List<IReadOnlyList<ViewEvent>> Batches { get; } = [];

        public Action<IReadOnlyList<ViewEvent>>? OnHandle { get; set; }

        public void HandleEvents(IReadOnlyList<ViewEvent> events)
        {
            Batches.Add(events);
            OnHandle?.Invoke(events);
        }
    }

    [Fact]
    public void EndEmitViewEvents_TwoCursorStateEvents_AreMergedAndDeliveredOnce()
    {
        var dispatcher = new ViewModelEventDispatcher();
        var received = new List<OutgoingViewModelEvent>();
        dispatcher.OnEvent += (_, e) => received.Add(e);

        ViewModelEventsCollector collector = dispatcher.BeginEmitViewEvents();
        collector.EmitOutgoingEvent(StateChanged([Caret(1, 1)], [Caret(1, 2)], 1, 1, "first"));
        collector.EmitOutgoingEvent(StateChanged([Caret(1, 2)], [Caret(1, 5)], 1, 2, "second"));
        Assert.Empty(received);

        dispatcher.EndEmitViewEvents();

        var merged = Assert.IsType<CursorStateChangedEvent>(Assert.Single(received));
        Assert.Equal([Caret(1, 1)], merged.OldSelections);
        Assert.Equal([Caret(1, 5)], merged.Selections);
        Assert.Equal(2, merged.ModelVersionId);
        Assert.Equal("second", merged.Source);
    }

    [Fact]
    public void EmitOutgoingEvent_NoOpCursorStateEvent_IsNotDelivered()
    {
        var dispatcher = new ViewModelEventDispatcher();
        var received = new List<OutgoingViewModelEvent>();
        dispatcher.OnEvent += (_, e) => received.Add(e);

        dispatcher.EmitOutgoingEvent(StateChanged([Caret(1, 1)], [Caret(1, 1)]));

        Assert.Empty(received);
    }

    [Fact]
    public void EmitOutgoingEvent_SameSelectionsButNewVersion_IsDelivered()
    {
        var dispatcher = new ViewModelEventDispatcher();
        var received = new List<OutgoingViewModelEvent>();
        dispatcher.OnEvent += (_, e) => received.Add(e);

        dispatcher.EmitOutgoingEvent(StateChanged([Caret(1, 1)], [Caret(1, 1)], oldVersion: 1, version: 2));

        Assert.Single(received);
    }

    [Fact]
    public void EndEmitViewEvents_Nested_DeliversOnlyWhenTheOutermostEnds()
    {
        var dispatcher = new ViewModelEventDispatcher();
        var handler = new RecordingHandler();
        dispatcher.AddViewEventHandler(handler);
        var received = new List<OutgoingViewModelEvent>();
        dispatcher.OnEvent += (_, e) => received.Add(e);

        ViewModelEventsCollector outer = dispatcher.BeginEmitViewEvents();
        ViewModelEventsCollector inner = dispatcher.BeginEmitViewEvents();
        Assert.Same(outer, inner);
        inner.EmitViewEvent(new ViewCursorStateChangedEvent([Caret(1, 2)], [Caret(1, 2)], CursorChangeReason.Explicit));
        inner.EmitOutgoingEvent(new ReadOnlyEditAttemptEvent());

        dispatcher.EndEmitViewEvents();
        Assert.Empty(handler.Batches);
        Assert.Empty(received);

        dispatcher.EndEmitViewEvents();
        Assert.Single(handler.Batches);
        Assert.Single(received);
    }

    [Fact]
    public void HandleEvents_HandlerEmitsANewEvent_ItIsQueuedAndOutgoingEventsWait()
    {
        var dispatcher = new ViewModelEventDispatcher();
        var order = new List<string>();
        var first = new RecordingHandler();
        var second = new RecordingHandler();
        first.OnHandle = events =>
        {
            order.Add($"first:{events.Count}");
            if (first.Batches.Count == 1)
            {
                // Emitted while the batch is being consumed: queued after the current batch.
                ViewModelEventsCollector collector = dispatcher.BeginEmitViewEvents();
                collector.EmitViewEvent(new ViewRevealRangeRequestEvent(
                    null, false, new Range(1, 1, 1, 1), null, VerticalRevealType.Simple, true, ScrollType.Smooth));
                collector.EmitOutgoingEvent(new ReadOnlyEditAttemptEvent());
                dispatcher.EndEmitViewEvents();
                order.Add("first:after-emit");
            }
        };
        second.OnHandle = events => order.Add($"second:{events.Count}");
        dispatcher.AddViewEventHandler(first);
        dispatcher.AddViewEventHandler(second);
        dispatcher.OnEvent += (_, _) => order.Add("outgoing");

        dispatcher.EmitSingleViewEvent(new ViewCursorStateChangedEvent([Caret(1, 2)], [Caret(1, 2)], CursorChangeReason.Explicit));

        // The new event reaches every handler after the first batch has reached all of them, then the outgoing event.
        Assert.Equal(["first:1", "first:after-emit", "second:1", "first:1", "second:1", "outgoing"], order);
    }

    [Fact]
    public void EndEmitViewEvents_TwoReadOnlyEditAttempts_AreDeliveredOnce()
    {
        var dispatcher = new ViewModelEventDispatcher();
        var received = new List<OutgoingViewModelEvent>();
        dispatcher.OnEvent += (_, e) => received.Add(e);

        ViewModelEventsCollector collector = dispatcher.BeginEmitViewEvents();
        collector.EmitOutgoingEvent(new ReadOnlyEditAttemptEvent());
        collector.EmitOutgoingEvent(new ReadOnlyEditAttemptEvent());
        dispatcher.EndEmitViewEvents();

        Assert.IsType<ReadOnlyEditAttemptEvent>(Assert.Single(received));
    }

    [Fact]
    public void RemoveViewEventHandler_StopsDelivery()
    {
        var dispatcher = new ViewModelEventDispatcher();
        var handler = new RecordingHandler();
        dispatcher.AddViewEventHandler(handler);
        dispatcher.RemoveViewEventHandler(handler);

        dispatcher.EmitSingleViewEvent(new ViewCursorStateChangedEvent([Caret(1, 2)], [Caret(1, 2)], CursorChangeReason.Explicit));

        Assert.Empty(handler.Batches);
    }
}

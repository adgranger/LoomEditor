// Ported from microsoft/vscode: src/vs/editor/common/viewModelEventDispatcher.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Events;

/// <summary>Something that receives the events sent to the view (Monaco: <c>ViewEventHandler</c>).</summary>
internal interface IViewEventHandler
{
    void HandleEvents(IReadOnlyList<ViewEvent> events);
}

/// <summary>
/// Delivers view events to the view event handlers and outgoing events to the outside world, once the operation
/// that produced them is over.
/// </summary>
/// <remarks>
/// <para>
/// Monaco: <c>ViewModelEventDispatcher</c>. Outgoing events are not delivered while a collector is open or while view
/// events are being consumed; they are merged by kind and dropped when they are no-ops. Monaco warns about a handler
/// added twice; this port does not.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class ViewModelEventDispatcher
{
    private readonly List<IViewEventHandler> _eventHandlers = [];
    private List<ViewEvent>? _viewEventQueue;
    private bool _isConsumingViewEventQueue;
    private ViewModelEventsCollector? _collector;
    private int _collectorCnt;
    private readonly List<OutgoingViewModelEvent> _outgoingEvents = [];

    /// <summary>Raised for each outgoing event that is not a no-op (Monaco: <c>onEvent</c>).</summary>
    public event EventHandler<OutgoingViewModelEvent>? OnEvent;

    public void EmitOutgoingEvent(OutgoingViewModelEvent e)
    {
        AddOutgoingEvent(e);
        EmitOutgoingEvents();
    }

    private void AddOutgoingEvent(OutgoingViewModelEvent e)
    {
        for (int i = 0, len = _outgoingEvents.Count; i < len; i++)
        {
            OutgoingViewModelEvent? mergeResult = _outgoingEvents[i].Kind == e.Kind ? _outgoingEvents[i].AttemptToMerge(e) : null;
            if (mergeResult is not null)
            {
                _outgoingEvents[i] = mergeResult;
                return;
            }
        }

        // not merged
        _outgoingEvents.Add(e);
    }

    private void EmitOutgoingEvents()
    {
        while (_outgoingEvents.Count > 0)
        {
            if (_collector is not null || _isConsumingViewEventQueue)
            {
                // right now collecting or emitting view events, so let's postpone emitting
                return;
            }

            OutgoingViewModelEvent outgoingEvent = _outgoingEvents[0];
            _outgoingEvents.RemoveAt(0);
            if (outgoingEvent.IsNoOp())
            {
                continue;
            }

            OnEvent?.Invoke(this, outgoingEvent);
        }
    }

    public void AddViewEventHandler(IViewEventHandler eventHandler) => _eventHandlers.Add(eventHandler);

    public void RemoveViewEventHandler(IViewEventHandler eventHandler)
    {
        for (int i = 0; i < _eventHandlers.Count; i++)
        {
            if (ReferenceEquals(_eventHandlers[i], eventHandler))
            {
                _eventHandlers.RemoveAt(i);
                break;
            }
        }
    }

    /// <summary>Starts collecting events; nested calls share one collector. Each call must be matched by <see cref="EndEmitViewEvents"/>.</summary>
    public ViewModelEventsCollector BeginEmitViewEvents()
    {
        _collectorCnt++;
        if (_collectorCnt == 1)
        {
            _collector = new ViewModelEventsCollector();
        }

        return _collector!;
    }

    public void EndEmitViewEvents()
    {
        _collectorCnt--;
        if (_collectorCnt == 0)
        {
            List<OutgoingViewModelEvent> outgoingEvents = _collector!.OutgoingEvents;
            List<ViewEvent> viewEvents = _collector.ViewEvents;
            _collector = null;

            foreach (OutgoingViewModelEvent outgoingEvent in outgoingEvents)
            {
                AddOutgoingEvent(outgoingEvent);
            }

            if (viewEvents.Count > 0)
            {
                EmitMany(viewEvents);
            }
        }

        EmitOutgoingEvents();
    }

    public void EmitSingleViewEvent(ViewEvent viewEvent)
    {
        try
        {
            ViewModelEventsCollector eventsCollector = BeginEmitViewEvents();
            eventsCollector.EmitViewEvent(viewEvent);
        }
        finally
        {
            EndEmitViewEvents();
        }
    }

    private void EmitMany(List<ViewEvent> events)
    {
        if (_viewEventQueue is not null)
        {
            _viewEventQueue = [.. _viewEventQueue, .. events];
        }
        else
        {
            _viewEventQueue = events;
        }

        if (!_isConsumingViewEventQueue)
        {
            ConsumeViewEventQueue();
        }
    }

    private void ConsumeViewEventQueue()
    {
        try
        {
            _isConsumingViewEventQueue = true;
            DoConsumeQueue();
        }
        finally
        {
            _isConsumingViewEventQueue = false;
        }
    }

    private void DoConsumeQueue()
    {
        while (_viewEventQueue is not null)
        {
            // Empty event queue, as events might come in while sending these off
            List<ViewEvent> events = _viewEventQueue;
            _viewEventQueue = null;

            // Use a clone of the event handlers list, as they might remove themselves
            IViewEventHandler[] eventHandlers = [.. _eventHandlers];
            foreach (IViewEventHandler eventHandler in eventHandlers)
            {
                eventHandler.HandleEvents(events);
            }
        }
    }
}

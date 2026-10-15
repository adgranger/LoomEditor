// Ported from microsoft/vscode: src/vs/editor/common/viewModelEventDispatcher.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Events;

/// <summary>Collects the events of an operation, to deliver them together when it ends (Monaco: <c>ViewModelEventsCollector</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class ViewModelEventsCollector
{
    public List<ViewEvent> ViewEvents { get; } = [];

    public List<OutgoingViewModelEvent> OutgoingEvents { get; } = [];

    public void EmitViewEvent(ViewEvent viewEvent) => ViewEvents.Add(viewEvent);

    public void EmitOutgoingEvent(OutgoingViewModelEvent outgoingEvent) => OutgoingEvents.Add(outgoingEvent);
}

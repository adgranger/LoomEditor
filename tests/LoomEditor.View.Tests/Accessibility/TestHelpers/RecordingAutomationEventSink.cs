using LoomEditor.View.Accessibility;

namespace LoomEditor.View.Tests.Accessibility.TestHelpers;

/// <summary>Records the automation events as <c>"text"</c> / <c>"selection"</c>.</summary>
internal class RecordingAutomationEventSink : IAutomationEventSink
{
    public List<string> Events { get; } = [];

    public virtual void RaiseTextChanged() => Events.Add("text");

    public virtual void RaiseSelectionChanged() => Events.Add("selection");
}

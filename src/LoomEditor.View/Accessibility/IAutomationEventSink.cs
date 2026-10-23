namespace LoomEditor.View.Accessibility;

/// <summary>Receives the automation events the editor raises for assistive technologies.</summary>
/// <remarks>Threading: called on the UI thread only.</remarks>
internal interface IAutomationEventSink
{
    /// <summary>The text changed (UIA <c>TextPatternOnTextChanged</c>).</summary>
    void RaiseTextChanged();

    /// <summary>The caret or the selection changed (UIA <c>TextPatternOnTextSelectionChanged</c>).</summary>
    void RaiseSelectionChanged();
}

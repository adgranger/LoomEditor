using LoomEditor.View.Accessibility;
using Microsoft.UI.Xaml.Automation.Peers;
using LoomEditorControl = LoomEditor.WinUI.Controls.LoomEditor;

namespace LoomEditor.WinUI.Accessibility;

/// <summary>Raises the UI Automation text events of the editor.</summary>
/// <remarks>
/// Nothing is raised when no automation client listens or asked for the peer: the peer is never created here.
/// Threading: UI thread only.
/// </remarks>
internal sealed class AutomationEventSink(LoomEditorControl owner) : IAutomationEventSink
{
    public void RaiseTextChanged() => Raise(AutomationEvents.TextPatternOnTextChanged);

    public void RaiseSelectionChanged() => Raise(AutomationEvents.TextPatternOnTextSelectionChanged);

    private void Raise(AutomationEvents e)
    {
        if (AutomationPeer.ListenerExists(e) && FrameworkElementAutomationPeer.FromElement(owner) is { } peer)
        {
            peer.RaiseAutomationEvent(e);
        }
    }
}

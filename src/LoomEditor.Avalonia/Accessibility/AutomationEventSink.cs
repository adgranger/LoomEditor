using Avalonia.Automation;
using Avalonia.Automation.Peers;
using LoomEditor.Core.Model;
using LoomEditor.View.Accessibility;
using LoomEditorControl = LoomEditor.Avalonia.Controls.LoomEditor;

namespace LoomEditor.Avalonia.Accessibility;

/// <summary>
/// Raises the automation events of the editor. Avalonia has no text pattern: a text change is a change of the Value
/// property, and a selection change raises nothing (a gap, see docs/PORTING.md).
/// </summary>
/// <remarks>
/// Nothing is raised when no automation client asked for the peer: the peer is never created here.
/// Threading: UI thread only.
/// </remarks>
internal sealed class AutomationEventSink(LoomEditorControl owner) : IAutomationEventSink
{
    public void RaiseTextChanged()
    {
        if (ControlAutomationPeer.FromElement(owner) is { } peer)
        {
            peer.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, null, owner.Model.GetValue(EndOfLinePreference.LF));
        }
    }

    public void RaiseSelectionChanged()
    {
    }
}

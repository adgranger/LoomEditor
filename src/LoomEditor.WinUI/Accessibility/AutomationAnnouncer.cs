using LoomEditor.View.Accessibility;
using Microsoft.UI.Xaml.Automation.Peers;
using LoomEditorControl = LoomEditor.WinUI.Controls.LoomEditor;

namespace LoomEditor.WinUI.Accessibility;

/// <summary>Announces editor messages to screen readers as UI Automation notifications.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class AutomationAnnouncer(LoomEditorControl owner) : IEditorAnnouncer
{
    private const string ActivityId = "LoomEditor.Announcement";

    public void Announce(string message)
    {
        if (!string.IsNullOrEmpty(message) && FrameworkElementAutomationPeer.FromElement(owner) is { } peer)
        {
            peer.RaiseNotificationEvent(AutomationNotificationKind.Other, AutomationNotificationProcessing.ImportantMostRecent, message, ActivityId);
        }
    }
}

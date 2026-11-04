using Avalonia.Controls;
using LoomEditor.View.Accessibility;

namespace LoomEditor.Avalonia.Accessibility;

/// <summary>
/// Announces editor messages to screen readers through an assertive live region: an off-screen <see cref="TextBlock"/> of
/// the template whose text is the message (WinUI raises a UI Automation notification; Avalonia has none).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class AutomationAnnouncer(Func<TextBlock?> getLiveRegion) : IEditorAnnouncer
{
    public void Announce(string message)
    {
        if (string.IsNullOrEmpty(message) || getLiveRegion() is not { } liveRegion)
        {
            return;
        }

        // Cleared first, so that the same message twice is announced twice.
        liveRegion.Text = null;
        liveRegion.Text = message;
    }
}

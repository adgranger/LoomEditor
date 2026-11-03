using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace LoomEditor.Avalonia.Controls.Comments;

/// <summary>
/// A UIA <c>Group</c> for a comment thread widget. Needed because a <see cref="UserControl"/> creates no role of its own: without one, its
/// <c>AutomationProperties.Name</c> is not exposed as a group. Name and help text come from <c>AutomationProperties</c>.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class CommentThreadAutomationPeer(Control owner) : ControlAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => Owner.GetType().Name;
}

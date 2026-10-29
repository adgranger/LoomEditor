using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace LoomEditor.WinUI.Controls.Comments;

/// <summary>
/// A UIA <c>Group</c> for a comment thread, a comment row or a suggestion block. Needed because a <see cref="UserControl"/> or a
/// <see cref="Grid"/> creates no automation peer of its own: without one, its <c>AutomationProperties.Name</c> is not exposed and its children
/// are flattened into the parent. Name and full description come from <c>AutomationProperties</c>.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class CommentThreadAutomationPeer(FrameworkElement owner) : FrameworkElementAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => Owner.GetType().Name;
}

/// <summary>The grid of one comment row, exposed to UIA as a named group.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class CommentRowPanel : Grid
{
    protected override AutomationPeer OnCreateAutomationPeer() => new CommentThreadAutomationPeer(this);
}

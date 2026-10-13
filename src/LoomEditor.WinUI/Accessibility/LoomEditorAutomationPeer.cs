using Microsoft.UI.Xaml.Automation.Peers;
using LoomEditorControl = LoomEditor.WinUI.Controls.LoomEditor;

namespace LoomEditor.WinUI.Accessibility;

/// <summary>
/// Describes the editor to UI Automation (screen readers, accessibility tools, UI tests).
/// </summary>
/// <remarks>
/// Phase 0: exposes the control as an <c>Edit</c> control. Phase 7 adds <c>ITextProvider</c> so that
/// screen readers can read and navigate the text.
/// </remarks>
internal sealed partial class LoomEditorAutomationPeer(LoomEditorControl owner) : FrameworkElementAutomationPeer(owner)
{
    protected override string GetClassNameCore() => nameof(LoomEditorControl);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;

    protected override string GetNameCore()
    {
        string name = base.GetNameCore();
        return string.IsNullOrEmpty(name) ? "Code editor" : name;
    }
}

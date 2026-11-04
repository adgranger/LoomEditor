using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using LoomEditor.Core.Model;
using LoomEditorControl = LoomEditor.Avalonia.Controls.LoomEditor;

namespace LoomEditor.Avalonia.Accessibility;

/// <summary>Describes the editor to automation clients (screen readers, accessibility tools, UI tests).</summary>
/// <remarks>
/// An <c>Edit</c> control with the Value pattern. The WinUI peer also has the Text pattern; Avalonia exposes no public text
/// provider, so screen readers read the value, not lines or words (docs/PORTING.md).
/// Threading: UI thread only.
/// </remarks>
internal sealed class LoomEditorAutomationPeer(LoomEditorControl owner) : ControlAutomationPeer(owner), IValueProvider
{
    public bool IsReadOnly => owner.IsReadOnly;

    public string Value => owner.Model.GetValue(EndOfLinePreference.LF);

    public void SetValue(string? value)
    {
        if (owner.IsReadOnly)
        {
            throw new InvalidOperationException("The editor is read-only.");
        }

        owner.Text = value ?? string.Empty;
    }

    protected override string GetClassNameCore() => nameof(LoomEditorControl);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;

    protected override string? GetNameCore()
    {
        string? name = base.GetNameCore();
        return string.IsNullOrEmpty(name) ? "Code editor" : name;
    }
}

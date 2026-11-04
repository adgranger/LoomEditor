using Avalonia.Automation;
using Avalonia.Controls;
using LoomEditor.Avalonia.Controls.DiffEditor;

namespace LoomEditor.Avalonia.Controls;

#pragma warning disable CA1001 // Controls are not IDisposable: disposable fields are released in OnUnloaded.
public sealed partial class LoomDiffEditor
#pragma warning restore CA1001
{
    private Panel? _viewerHost;

    // The viewer, for the tests.
    internal AccessibleDiffViewer? AccessibleViewer => _accessibleViewer;

    private void ConnectViewerHost(Panel host) => _viewerHost = host;

    private void HideEditorsFromAutomation()
    {
        AutomationProperties.SetAccessibilityView(_original!, AccessibilityView.Raw);
        AutomationProperties.SetAccessibilityView(_modified!, AccessibilityView.Raw);
    }

    private void ShowEditorsToAutomation()
    {
        AutomationProperties.SetAccessibilityView(_original!, AccessibilityView.Default);
        AutomationProperties.SetAccessibilityView(_modified!, AccessibilityView.Default);
    }

    private static void FocusEditor(LoomEditor editor) => editor.Focus();
}

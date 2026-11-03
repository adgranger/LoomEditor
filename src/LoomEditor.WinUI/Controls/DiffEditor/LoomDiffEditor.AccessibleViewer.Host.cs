using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace LoomEditor.WinUI.Controls;

#pragma warning disable CA1001 // Controls are not IDisposable: disposable fields are released in OnUnloaded.
public sealed partial class LoomDiffEditor
#pragma warning restore CA1001
{
    private Grid? _viewerHost;

    private void ConnectViewerHost(Grid host) => _viewerHost = host;

    private void HideEditorsFromAutomation()
    {
        AutomationProperties.SetAccessibilityView(_original!, AccessibilityView.Raw);
        AutomationProperties.SetAccessibilityView(_modified!, AccessibilityView.Raw);
    }

    private void ShowEditorsToAutomation()
    {
        AutomationProperties.SetAccessibilityView(_original!, AccessibilityView.Content);
        AutomationProperties.SetAccessibilityView(_modified!, AccessibilityView.Content);
    }

    private static void FocusEditor(LoomEditor editor) => editor.Focus(FocusState.Programmatic);
}

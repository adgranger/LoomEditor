using LoomEditor.View.Layout;
#if LOOM_WINUI
using LoomEditor.WinUI.Accessibility;
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
using LoomEditor.Avalonia.Accessibility;
namespace LoomEditor.Avalonia.Controls;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif

public sealed partial class LoomEditor
{
    private void RefreshScreenReader()
    {
        var options = new EditorAccessibilityOptions
        {
            Setting = AccessibilitySupport switch
            {
                EditorAccessibilitySupport.On => View.Layout.AccessibilitySupport.Enabled,
                EditorAccessibilitySupport.Off => View.Layout.AccessibilitySupport.Disabled,
                _ => View.Layout.AccessibilitySupport.Unknown,
            },
            Detected = ScreenReaderDetector.IsScreenReaderRunning() ? View.Layout.AccessibilitySupport.Enabled : View.Layout.AccessibilitySupport.Disabled,
        };
        if (options != _viewModel.LayoutOptions.Accessibility)
        {
            _viewModel.SetOptions(_viewModel.LayoutOptions with { Accessibility = options }, _viewModel.ViewLayoutOptions);
        }
    }
}

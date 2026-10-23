using LoomEditor.View.Layout;
using LoomEditor.WinUI.Accessibility;
using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

// Screen reader mode. Monaco: the `accessibilitySupport` option and `env.accessibilitySupport`.
#pragma warning disable CA1001 // Reported on the first partial declaration; see the suppression in LoomEditor.cs.
public sealed partial class LoomEditor
#pragma warning restore CA1001
{
    /// <summary>Identifies the <see cref="AccessibilitySupport"/> dependency property.</summary>
    public static readonly DependencyProperty AccessibilitySupportProperty = DependencyProperty.Register(
        nameof(AccessibilitySupport), typeof(EditorAccessibilitySupport), typeof(LoomEditor), new PropertyMetadata(EditorAccessibilitySupport.Auto, OnAccessibilitySupportChanged));

    /// <summary>
    /// Whether the editor runs in screen reader mode: <see cref="EditorAccessibilitySupport.Auto"/> (default) detects a running
    /// screen reader, <see cref="EditorAccessibilitySupport.On"/> and <see cref="EditorAccessibilitySupport.Off"/> force it.
    /// </summary>
    /// <remarks>
    /// Windows tells a window about a screen reader starting only through <c>WM_SETTINGCHANGE</c>, which a WinUI control cannot see
    /// without subclassing the window, so the detection is repeated when the control loads and every time it gets focus.
    /// Whether the model is dominated by long lines is computed once, when <see cref="Text"/> is set (as Monaco does when the model
    /// is attached); edits typed later or made through the model do not recompute it.
    /// </remarks>
    public EditorAccessibilitySupport AccessibilitySupport
    {
        get => (EditorAccessibilitySupport)GetValue(AccessibilitySupportProperty);
        set => SetValue(AccessibilitySupportProperty, value);
    }

    private void InitializeAccessibility() => GotFocus += (_, _) => RefreshScreenReader();

    private static void OnAccessibilitySupportChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
        ((LoomEditor)sender).RefreshScreenReader();

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

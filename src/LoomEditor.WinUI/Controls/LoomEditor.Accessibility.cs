using LoomEditor.Rendering;
using LoomEditor.View.Accessibility;
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

    /// <summary>Identifies the <see cref="TabMovesFocus"/> dependency property.</summary>
    public static readonly DependencyProperty TabMovesFocusProperty = DependencyProperty.Register(
        nameof(TabMovesFocus), typeof(bool), typeof(LoomEditor), new PropertyMetadata(false, OnTabMovesFocusChanged));

    /// <summary>
    /// Whether Tab and Shift+Tab move the keyboard focus to the next control instead of inserting a tab. Ctrl+M toggles it
    /// (Monaco's <c>editor.action.toggleTabFocusMode</c>). It is per editor and not related to <c>IsTabStop</c>.
    /// </summary>
    public bool TabMovesFocus
    {
        get => (bool)GetValue(TabMovesFocusProperty);
        set => SetValue(TabMovesFocusProperty, value);
    }

    private AutomationTextDocument? _automationDocument;

    /// <summary>What UI Automation reads: created once.</summary>
    internal AutomationTextDocument AutomationDocument => _automationDocument ??= new AutomationTextDocument(_viewModel);

    /// <summary>The geometry queries of UI Automation (cheap to create).</summary>
    internal AutomationTextGeometry AutomationGeometry => new(AutomationDocument, new RendererLineMetrics(this));

    /// <summary>The renderer, or <see langword="null"/> while the canvas is not ready.</summary>
    internal TextViewRenderer? AutomationRenderer => _renderer;

    /// <summary>Whether the editor has the keyboard focus (<c>ITextProvider2.GetCaretRange</c>).</summary>
    internal bool AutomationHasFocus => HasFocus;

    private AutomationEventCoalescer? _automationEvents;

    private void StartAutomationEvents() => _automationEvents ??= new AutomationEventCoalescer(_context, new AutomationEventSink(this));

    private void StopAutomationEvents()
    {
        _automationEvents?.Dispose();
        _automationEvents = null;
    }

    private void InitializeAccessibility()
    {
        _context.Announcer = new AutomationAnnouncer(this);
        GotFocus += (_, _) => RefreshScreenReader();
        _context.TabMovesFocusChanged += (_, _) =>
        {
            if (TabMovesFocus != _context.TabMovesFocus)
            {
                TabMovesFocus = _context.TabMovesFocus;
            }
        };
    }

    private static void OnTabMovesFocusChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
        ((LoomEditor)sender)._context.TabMovesFocus = (bool)e.NewValue;

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

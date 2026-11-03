using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using LoomEditor.Avalonia.Accessibility;
using LoomEditor.View.Accessibility;

namespace LoomEditor.Avalonia.Controls;

// Screen readers: the automation peer, the announcements and screen reader mode. Monaco: the `accessibilitySupport`
// option and `env.accessibilitySupport`.
#pragma warning disable CA1001 // Reported on the first partial declaration; see the suppression in LoomEditor.cs.
public sealed partial class LoomEditor
#pragma warning restore CA1001
{
    private const string LiveRegionPartName = "PART_LiveRegion";

    /// <summary>Identifies the <see cref="AccessibilitySupport"/> styled property.</summary>
    public static readonly StyledProperty<EditorAccessibilitySupport> AccessibilitySupportProperty =
        AvaloniaProperty.Register<LoomEditor, EditorAccessibilitySupport>(nameof(AccessibilitySupport), EditorAccessibilitySupport.Auto);

    /// <summary>Identifies the <see cref="TabMovesFocus"/> styled property.</summary>
    public static readonly StyledProperty<bool> TabMovesFocusProperty =
        AvaloniaProperty.Register<LoomEditor, bool>(nameof(TabMovesFocus), false);

    private TextBlock? _liveRegion;
    private AutomationEventCoalescer? _automationEvents;

    /// <summary>
    /// Whether the editor runs in screen reader mode: <see cref="EditorAccessibilitySupport.Auto"/> (default) detects a running
    /// screen reader, <see cref="EditorAccessibilitySupport.On"/> and <see cref="EditorAccessibilitySupport.Off"/> force it.
    /// </summary>
    /// <remarks>
    /// The detection works on Windows only and is repeated when the control is attached and every time it gets focus. On macOS
    /// and Linux no screen reader is detected: set <see cref="EditorAccessibilitySupport.On"/> there.
    /// </remarks>
    public EditorAccessibilitySupport AccessibilitySupport
    {
        get => GetValue(AccessibilitySupportProperty);
        set => SetValue(AccessibilitySupportProperty, value);
    }

    /// <summary>
    /// Whether Tab and Shift+Tab move the keyboard focus to the next control instead of inserting a tab. Ctrl+M toggles it
    /// (Monaco's <c>editor.action.toggleTabFocusMode</c>). It is per editor and not related to <c>IsTabStop</c>.
    /// </summary>
    public bool TabMovesFocus
    {
        get => GetValue(TabMovesFocusProperty);
        set => SetValue(TabMovesFocusProperty, value);
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new LoomEditorAutomationPeer(this);

    private static void InitializeAccessibilityProperties()
    {
        AccessibilitySupportProperty.Changed.AddClassHandler<LoomEditor>(static (editor, _) => editor.RefreshScreenReader());
        TabMovesFocusProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) => editor._context.TabMovesFocus = (bool)e.NewValue!);
    }

    private void InitializeAccessibility()
    {
        _context.Announcer = new AutomationAnnouncer(() => _liveRegion);
        GotFocus += (_, _) => RefreshScreenReader();
        _context.TabMovesFocusChanged += (_, _) =>
        {
            if (TabMovesFocus != _context.TabMovesFocus)
            {
                TabMovesFocus = _context.TabMovesFocus;
            }
        };
    }

    private void AttachLiveRegion(INameScope nameScope) => _liveRegion = nameScope.Find<TextBlock>(LiveRegionPartName);

    private void StartAccessibility()
    {
        _automationEvents ??= new AutomationEventCoalescer(_context, new AutomationEventSink(this));
        RefreshScreenReader();
    }

    private void StopAccessibility()
    {
        _automationEvents?.Dispose();
        _automationEvents = null;
    }
}

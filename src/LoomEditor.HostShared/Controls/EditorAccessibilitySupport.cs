#if LOOM_WINUI
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif

/// <summary>Whether the editor runs in screen reader mode (Monaco: <c>accessibilitySupport</c>).</summary>
public enum EditorAccessibilitySupport
{
    /// <summary>The editor detects a running screen reader.</summary>
    Auto = 0,

    /// <summary>Screen reader mode is always on.</summary>
    On = 1,

    /// <summary>Screen reader mode is always off.</summary>
    Off = 2,
}

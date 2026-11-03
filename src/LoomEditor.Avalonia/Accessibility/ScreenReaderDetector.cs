using LoomEditor.Avalonia.Interop;

namespace LoomEditor.Avalonia.Accessibility;

/// <summary>
/// Detects a running screen reader. Windows: Narrator, NVDA and JAWS all set the system screen reader flag. macOS and
/// Linux: always <see langword="false"/> (no API through Avalonia); the host sets <c>AccessibilitySupport = On</c>.
/// </summary>
/// <remarks>Threading: stateless; thread-safe.</remarks>
internal static class ScreenReaderDetector
{
    private const uint SpiGetScreenReader = 0x0046;

    /// <summary>True when Windows reports a screen reader; false when none runs, the call fails or the OS is not Windows.</summary>
    public static bool IsScreenReaderRunning()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        int value = 0;
        return NativeMethods.SystemParametersInfo(SpiGetScreenReader, 0, ref value, 0) && value != 0;
    }
}

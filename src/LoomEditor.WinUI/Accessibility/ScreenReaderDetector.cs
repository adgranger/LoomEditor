using LoomEditor.WinUI.Interop;

namespace LoomEditor.WinUI.Accessibility;

/// <summary>Detects a running screen reader (Narrator, NVDA and JAWS all set the system screen reader flag).</summary>
/// <remarks>Threading: stateless; thread-safe.</remarks>
internal static class ScreenReaderDetector
{
    private const uint SpiGetScreenReader = 0x0046;

    /// <summary>True when Windows reports a screen reader; false when none runs or the call fails.</summary>
    public static bool IsScreenReaderRunning()
    {
        int value = 0;
        return NativeMethods.SystemParametersInfo(SpiGetScreenReader, 0, ref value, 0) && value != 0;
    }
}

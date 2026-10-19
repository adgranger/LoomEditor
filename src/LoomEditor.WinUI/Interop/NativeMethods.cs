using System.Runtime.InteropServices;

namespace LoomEditor.WinUI.Interop;

/// <summary>Win32 functions the control needs (<c>[LibraryImport]</c>: source-generated marshalling, Native AOT safe).</summary>
/// <remarks>Threading: stateless; thread-safe.</remarks>
internal static partial class NativeMethods
{
    /// <summary>A point in pixels (Win32 <c>POINT</c>).</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct Point
    {
        public int X;
        public int Y;
    }

    /// <summary>Converts client coordinates of a window to screen coordinates.</summary>
    [LibraryImport("user32.dll", EntryPoint = "ClientToScreen")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ClientToScreen(nint hwnd, ref Point point);
}

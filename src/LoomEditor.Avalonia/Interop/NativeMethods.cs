using System.Runtime.InteropServices;

namespace LoomEditor.Avalonia.Interop;

/// <summary>
/// The only Windows API of the Avalonia control (P15-14): the screen reader flag. Callers check
/// <see cref="OperatingSystem.IsWindows"/> first. <c>[LibraryImport]</c>: source-generated marshalling, Native AOT safe.
/// </summary>
/// <remarks>Threading: stateless; thread-safe.</remarks>
internal static partial class NativeMethods
{
    /// <summary>Reads a system parameter (here: the screen reader flag).</summary>
    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SystemParametersInfo(uint action, uint param, ref int value, uint winIni);
}

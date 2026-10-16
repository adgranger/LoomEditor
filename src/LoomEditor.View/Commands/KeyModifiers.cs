namespace LoomEditor.View.Commands;

/// <summary>The modifier keys held during a key press (Monaco's <c>KeyMod</c>; <c>CtrlCmd</c> is <see cref="Ctrl"/> on Windows).</summary>
/// <remarks>Threading: an enum; thread-safe.</remarks>
[Flags]
internal enum KeyModifiers
{
    None = 0,
    Ctrl = 1,
    Shift = 2,
    Alt = 4,
    Meta = 8,
}

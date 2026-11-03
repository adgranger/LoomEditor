#if LOOM_WINUI
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif

/// <summary>A side of a <see cref="LoomDiffEditor"/>.</summary>
public enum DiffSide
{
    /// <summary>The original text (left in the side-by-side view).</summary>
    Original,

    /// <summary>The modified text (right in the side-by-side view).</summary>
    Modified,
}

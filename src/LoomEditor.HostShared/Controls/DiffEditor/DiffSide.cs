#if !LOOM_AVALONIA
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#endif

/// <summary>A side of a <see cref="LoomDiffEditor"/>.</summary>
public enum DiffSide
{
    /// <summary>The original text (left in the side-by-side view).</summary>
    Original,

    /// <summary>The modified text (right in the side-by-side view).</summary>
    Modified,
}

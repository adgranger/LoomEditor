#if !LOOM_AVALONIA
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#endif

/// <summary>The side of the editor where the minimap is shown (Monaco: <c>minimap.side</c>).</summary>
public enum EditorMinimapSide
{
    /// <summary>The minimap is on the right.</summary>
    Right,

    /// <summary>The minimap is on the left.</summary>
    Left,
}

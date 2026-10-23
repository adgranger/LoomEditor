namespace LoomEditor.WinUI.Controls;

/// <summary>When the lines of the editor wrap (Monaco: <c>wordWrap</c>).</summary>
public enum EditorWordWrap
{
    /// <summary>Lines never wrap.</summary>
    Off,

    /// <summary>Lines wrap at the viewport width.</summary>
    On,

    /// <summary>Lines wrap at <see cref="LoomEditor.WordWrapColumn"/>.</summary>
    WordWrapColumn,

    /// <summary>Lines wrap at the minimum of the viewport width and <see cref="LoomEditor.WordWrapColumn"/>.</summary>
    Bounded,
}

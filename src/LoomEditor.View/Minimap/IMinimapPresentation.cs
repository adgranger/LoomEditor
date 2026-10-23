namespace LoomEditor.View.Minimap;

/// <summary>The hover and drag flags that Monaco keeps in CSS classes of the minimap; the pointer controller sets them.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface IMinimapPresentation
{
    /// <summary>The pointer is over the minimap.</summary>
    bool MouseOverMinimap { get; set; }

    /// <summary>The pointer is over the slider.</summary>
    bool SliderHovered { get; set; }

    /// <summary>The slider is being dragged.</summary>
    bool SliderActive { get; set; }
}

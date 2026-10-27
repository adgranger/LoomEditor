namespace LoomEditor.View.Layout;

/// <summary>A view zone that is in the viewport, with its position in the document.</summary>
/// <param name="Zone">The zone.</param>
/// <param name="VerticalOffset">The top of the zone in the document (not relative to the scroll position).</param>
/// <param name="Height">The height of the zone in pixels.</param>
internal readonly record struct ViewZoneViewportData(ViewZone Zone, int VerticalOffset, int Height);

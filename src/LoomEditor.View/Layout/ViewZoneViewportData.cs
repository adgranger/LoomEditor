namespace LoomEditor.View.Layout;

/// <summary>A view zone that is in the viewport, with its position in the document.</summary>
/// <param name="Zone">The zone.</param>
/// <param name="VerticalOffset">The top of the zone in the document (not relative to the scroll position).</param>
/// <param name="Height">The height of the zone in pixels.</param>
internal readonly record struct ViewZoneViewportData(ViewZone Zone, int VerticalOffset, int Height)
{
    /// <summary>
    /// The zone under a point and the offset of the point from the top of the zone, or <see langword="null"/>. <paramref name="y"/>
    /// is in document coordinates (scroll included); <paramref name="x"/> must be in the content area.
    /// </summary>
    public static (ViewZone Zone, double YInZone)? HitTest(
        IReadOnlyList<ViewZoneViewportData> zones, double x, double y, double contentLeft, double contentWidth)
    {
        if (x < contentLeft || x >= contentLeft + contentWidth)
        {
            return null;
        }

        foreach (ViewZoneViewportData data in zones)
        {
            if (data.Height > 0 && y >= data.VerticalOffset && y < data.VerticalOffset + data.Height)
            {
                return (data.Zone, y - data.VerticalOffset);
            }
        }

        return null;
    }
}

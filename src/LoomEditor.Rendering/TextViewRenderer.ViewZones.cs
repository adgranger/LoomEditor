using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

// Monaco: the DOM node of a zone covers the content area; an `Empty` zone of a diff editor has the `diagonal-fill` class.
// The other kinds are drawn by later cards.
internal sealed partial class TextViewRenderer
{
    private readonly DiagonalFillPattern _diagonalFill = new();

    // Under the text: the zones are whitespace between the lines.
    private void DrawViewZones(IRenderSurface surface, ViewportData data, EditorLayoutInfo layoutInfo, int scrollTop)
    {
        if (data.ZoneViewportData.Count == 0 || Theme.GetColor("diffEditor.diagonalFill") is not { } color)
        {
            return;
        }

        int width = layoutInfo.Width - layoutInfo.ContentLeft;
        if (width < 1)
        {
            return;
        }

        foreach (ViewZoneViewportData zone in data.ZoneViewportData)
        {
            if (zone.Zone.Kind != ViewZoneKind.Empty || zone.Height < 1)
            {
                continue;
            }

            DiagonalFillPattern.Entry pattern = _diagonalFill.Get(width, zone.Height, color);
            surface.DrawRgbaImage(
                pattern.Rgba, pattern.Width, pattern.Height, (float)layoutInfo.ContentLeft, zone.VerticalOffset - scrollTop,
                pattern.Width, pattern.Height, 1f, pattern.Version);
        }
    }
}

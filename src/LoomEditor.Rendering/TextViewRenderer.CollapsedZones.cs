using System.Globalization;
using LoomEditor.Themes;
using LoomEditor.View.DiffEditor;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

// Monaco: `CollapsedCodeOverlayWidget` (hideUnchangedRegionsFeature.ts 312–412) and `.diff-hidden-lines` (style.css 55–70).
// Behavior differences: the glyph uses the editor font (the surface has no per-text font), and the outline breadcrumb is not drawn.
internal sealed partial class TextViewRenderer
{
    private const float CollapsedStripHeight = 2;
    private const string CollapsedGlyph = "⇕";

    internal static string CollapsedLabel(int hiddenLineCount) =>
        hiddenLineCount == 1 ? "1 hidden line" : string.Create(CultureInfo.InvariantCulture, $"{hiddenLineCount} hidden lines");

    // The content part of the bar: background, the strips of the edges that can still grow, and the label.
    private void DrawCollapsedBar(IRenderSurface surface, CollapsedZoneData data, EditorLayoutInfo layoutInfo, int zoneTop, int zoneHeight)
    {
        ThemeColor background = Theme.GetColor("diffEditor.unchangedRegionBackground") ?? Theme.Background;
        ThemeColor foreground = Theme.GetColor("diffEditor.unchangedRegionForeground") ?? Theme.Foreground;
        float left = layoutInfo.ContentLeft;
        float width = layoutInfo.Width - left;
        UnchangedRegion region = data.Region;

        surface.FillRectangle(left, zoneTop, width, zoneHeight, background);
        if (region.VisibleLineCountTop > 0)
        {
            surface.FillRectangle(left, zoneTop, width, CollapsedStripHeight, foreground);
        }

        if (region.VisibleLineCountBottom > 0)
        {
            surface.FillRectangle(left, zoneTop + zoneHeight - CollapsedStripHeight, width, CollapsedStripHeight, foreground);
        }

        int hidden = region.LineCount - region.VisibleLineCountTop - region.VisibleLineCountBottom;
        surface.DrawLineLayout(_cache.Get(CollapsedLabel(hidden)), left + 8, CenteredTop(zoneTop, zoneHeight), foreground);
    }

    // The gutter part of the bars, drawn after the gutter (which hides what is scrolled under it).
    private void DrawCollapsedMargins(IRenderSurface surface, ViewportData data, EditorLayoutInfo layoutInfo, int scrollTop)
    {
        foreach (ViewZoneViewportData zone in data.ZoneViewportData)
        {
            if (zone.Height < 1 || zone.Zone is not { Kind: ViewZoneKind.Collapsed })
            {
                continue;
            }

            ThemeColor background = Theme.GetColor("diffEditor.unchangedRegionBackground") ?? Theme.Background;
            ThemeColor foreground = Theme.GetColor("diffEditor.unchangedRegionForeground") ?? Theme.Foreground;
            int top = zone.VerticalOffset - scrollTop;
            surface.FillRectangle(0, top, layoutInfo.ContentLeft, zone.Height, background);
            surface.DrawLineLayout(_cache.Get(CollapsedGlyph), layoutInfo.LineNumbersLeft, CenteredTop(top, zone.Height), foreground);
        }
    }

    private float CenteredTop(int zoneTop, int zoneHeight) => zoneTop + ((zoneHeight - _measurer.FontInfo.LineHeight) / 2f);
}

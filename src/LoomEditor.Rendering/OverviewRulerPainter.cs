// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/overviewRuler/overviewRuler.ts (_render, _renderOneLane)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

/// <summary>Draws the color bands of an <see cref="OverviewZoneManager"/> as rectangles (Monaco: the generic <c>OverviewRuler</c>).</summary>
/// <remarks>
/// The owner configures the manager (zones, line height, sizes, pixel ratio) before each <see cref="Draw"/>. Deliberate difference: the
/// bands are filled in DIPs (canvas pixels divided by the pixel ratio) instead of on a canvas of device pixels.
/// Threading: UI thread only.
/// </remarks>
internal sealed class OverviewRulerPainter(OverviewZoneManager manager)
{
    /// <summary>Fills one rectangle per merged band, in the strip whose top left corner is (<paramref name="left"/>, <paramref name="top"/>).</summary>
    /// <param name="surface">Where to draw.</param>
    /// <param name="left">The left of the strip, in DIPs.</param>
    /// <param name="top">The top of the strip, in DIPs.</param>
    /// <param name="width">The width of a band, in DIPs.</param>
    /// <param name="height">The height of the strip, in DIPs (the bands never go below it).</param>
    public void Draw(IRenderSurface surface, float left, float top, float width, float height)
    {
        ArgumentNullException.ThrowIfNull(surface);

        // Monaco: nothing is drawn until the scrollable content has a height.
        if (manager.OuterHeight == 0)
        {
            return;
        }

        IReadOnlyList<ColorZone> colorZones = manager.ResolveColorZones();
        if (colorZones.Count == 0)
        {
            return;
        }

        IReadOnlyList<ThemeColor> id2Color = manager.GetId2Color();
        float ratio = (float)manager.PixelRatio;
        float bottom = top + height;

        void Fill(int from, int to, int colorId)
        {
            float y = top + (from / ratio);
            float h = Math.Min(bottom, top + (to / ratio)) - y;
            if (h > 0)
            {
                surface.FillRectangle(left, y, width, h, id2Color[colorId]);
            }
        }

        int currentColorId = 0; // never a real color id: they start at 1
        int currentFrom = 0;
        int currentTo = 0;
        foreach (ColorZone zone in colorZones)
        {
            if (zone.ColorId != currentColorId)
            {
                if (currentColorId != 0)
                {
                    Fill(currentFrom, currentTo, currentColorId);
                }

                currentColorId = zone.ColorId;
                currentFrom = zone.From;
                currentTo = zone.To;
            }
            else if (currentTo >= zone.From)
            {
                currentTo = Math.Max(currentTo, zone.To);
            }
            else
            {
                Fill(currentFrom, currentTo, currentColorId);
                currentFrom = zone.From;
                currentTo = zone.To;
            }
        }

        Fill(currentFrom, currentTo, currentColorId);
    }
}

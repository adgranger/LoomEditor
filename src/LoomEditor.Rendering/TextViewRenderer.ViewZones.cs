using System.Globalization;
using LoomEditor.Themes;
using LoomEditor.View.DiffEditor;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

// Monaco: the DOM node of a zone covers the content area; an `Empty` zone of a diff editor has the `diagonal-fill` class.
// A `DeletedLines` zone is `renderLines.ts` (the text) and `.inline-deleted-margin-view-zone` (the margin).
internal sealed partial class TextViewRenderer
{
    private readonly DiagonalFillPattern _diagonalFill = new();

    // The layouts a frame needs for the lines of the deleted lines zones in the viewport (text and number each).
    private static int CountDeletedLines(ViewportData data)
    {
        int count = 0;
        foreach (ViewZoneViewportData zone in data.ZoneViewportData)
        {
            if (zone.Zone.Data is DeletedLinesZoneData deleted)
            {
                count += deleted.OriginalLines.Length;
            }
            else if (zone.Zone.Kind == ViewZoneKind.Collapsed)
            {
                count++;
            }
        }

        return count;
    }

    // Under the text: the zones are whitespace between the lines.
    private void DrawViewZones(IRenderSurface surface, ViewModel viewModel, ViewportData data, EditorLayoutInfo layoutInfo, int scrollTop, double scrollLeft)
    {
        if (data.ZoneViewportData.Count == 0)
        {
            return;
        }

        int width = layoutInfo.Width - layoutInfo.ContentLeft;
        if (width < 1)
        {
            return;
        }

        ThemeColor? diagonalColor = Theme.GetColor("diffEditor.diagonalFill");
        foreach (ViewZoneViewportData zone in data.ZoneViewportData)
        {
            if (zone.Height < 1)
            {
                continue;
            }

            if (zone.Zone is { Kind: ViewZoneKind.Empty } && diagonalColor is { } color)
            {
                DiagonalFillPattern.Entry pattern = _diagonalFill.Get(width, zone.Height, color);
                surface.DrawRgbaImage(
                    pattern.Rgba, pattern.Width, pattern.Height, (float)layoutInfo.ContentLeft, zone.VerticalOffset - scrollTop,
                    pattern.Width, pattern.Height, 1f, pattern.Version);
            }
            else if (zone.Zone is { Kind: ViewZoneKind.Collapsed, Data: CollapsedZoneData collapsed })
            {
                DrawCollapsedBar(surface, collapsed, layoutInfo, zone.VerticalOffset - scrollTop, zone.Height);
            }
            else if (zone.Zone is { Kind: ViewZoneKind.DeletedLines, Data: DeletedLinesZoneData deleted })
            {
                DrawDeletedLinesContent(surface, viewModel, deleted, layoutInfo, zone.VerticalOffset - scrollTop, zone.Height, scrollLeft);
            }
        }
    }

    // The gutter hides the zones that are scrolled under it: its part of the zone is drawn after it.
    private void DrawDeletedLinesMargins(IRenderSurface surface, ViewportData data, EditorLayoutInfo layoutInfo, int scrollTop, FontInfo font)
    {
        foreach (ViewZoneViewportData zone in data.ZoneViewportData)
        {
            if (zone.Zone.Data is not DeletedLinesZoneData deleted)
            {
                continue;
            }

            for (int i = 0; i < deleted.OriginalLines.Length && (i + 1) * font.LineHeight <= zone.Height; i++)
            {
                float top = zone.VerticalOffset - scrollTop + (i * font.LineHeight);
                if (!TryGetOriginalLine(deleted, i, out _))
                {
                    continue;
                }

                DrawDecorationBox(surface, "gutter-delete", 0, top, layoutInfo.ContentLeft, font.LineHeight);
                ILineLayout number = _cache.Get((deleted.OriginalLines.StartLineNumber + i).ToString(CultureInfo.InvariantCulture));
                surface.DrawLineLayout(
                    number, layoutInfo.LineNumbersLeft + layoutInfo.LineNumbersWidth - (float)number.Width, top, Theme.LineNumberForeground);
                if (deleted.RenderIndicators)
                {
                    DrawGutterIcon(surface, "delete-sign", layoutInfo, top, font.LineHeight, hovered: false);
                }
            }
        }
    }

    // A stale diff may name a line the original model no longer has: such a line is skipped (ADR 0012, decision 4).
    private static bool TryGetOriginalLine(DeletedLinesZoneData deleted, int index, out ViewLineData line)
    {
        try
        {
            line = deleted.GetOriginalLineData(deleted.OriginalLines.StartLineNumber + index);
            return true;
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or IndexOutOfRangeException)
        {
            line = null!;
            return false;
        }
    }

    private void DrawDeletedLinesContent(
        IRenderSurface surface, ViewModel viewModel, DeletedLinesZoneData deleted, EditorLayoutInfo layoutInfo, int zoneTop, int zoneHeight, double scrollLeft)
    {
        FontInfo font = _measurer.FontInfo;
        float left = layoutInfo.ContentLeft;
        float width = layoutInfo.Width - layoutInfo.ContentLeft;
        uint defaultForeground = LineStyle.ToArgb(Theme.Foreground);
        for (int i = 0; i < deleted.OriginalLines.Length && (i + 1) * font.LineHeight <= zoneHeight; i++)
        {
            if (!TryGetOriginalLine(deleted, i, out ViewLineData line))
            {
                continue;
            }

            int lineNumber = deleted.OriginalLines.StartLineNumber + i;
            float top = zoneTop + (i * font.LineHeight);
            DrawDecorationBox(surface, "line-delete", left, top, width, font.LineHeight);

            string text = line.Content.Length > StopRenderingLineAfter ? line.Content[..StopRenderingLineAfter] : line.Content;
            ILineLayout layout = _cache.Get(text, LineStyle.Create(line.Tokens, text.Length, viewModel.TokenColorMap, defaultForeground));
            float x = (float)(left - scrollLeft);
            foreach (Range range in deleted.DeletedRanges)
            {
                if (lineNumber < range.StartLineNumber || lineNumber > range.EndLineNumber)
                {
                    continue;
                }

                // Clipped to the line: a stale diff may name columns the line no longer has.
                int start = lineNumber == range.StartLineNumber ? Math.Min(range.StartColumn - 1, text.Length) : 0;
                int end = lineNumber == range.EndLineNumber ? Math.Min(range.EndColumn - 1, text.Length) : text.Length;
                if (end > start)
                {
                    double startX = layout.GetCharacterX(start);
                    DrawDecorationBox(surface, "char-delete", (float)(x + startX), top, (float)(layout.GetCharacterX(end) - startX), font.LineHeight);
                }
            }

            surface.DrawLineLayout(layout, x, top, Theme.Foreground);
        }
    }
}

// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/overviewRuler/decorationsOverviewRuler.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

// Behavior difference: the ruler is drawn every frame on the surface, in DIPs at `layoutInfo.OverviewRuler`; Monaco's
// `ShouldRenderValue` and its "rendered decorations" diffing only avoid redrawing a canvas, so they are not ported.
// The border is two 1-DIP rectangles instead of two 1px canvas strokes (crisper, and no `DrawLine` call).
internal sealed partial class TextViewRenderer
{
    private const int MinDecorationHeight = 6;
    private const int FullLane = 7;

    // Monaco: `DecorationsOverviewRuler._render` (and `onCursorStateChanged` for the cursor marks).
    private void DrawOverviewRuler(IRenderSurface surface, ViewModel viewModel, IReadOnlyList<Selection> selections, EditorLayoutInfo layoutInfo, FontInfo font)
    {
        var settings = new OverviewRulerSettings(viewModel.LayoutOptions.OverviewRuler, Theme, layoutInfo.OverviewRuler);
        if (settings.Lanes == 0)
        {
            return;
        }

        float left = layoutInfo.Width - settings.Right - settings.Width;
        float top = settings.Top;
        int canvasWidth = settings.CanvasWidth;
        int canvasHeight = settings.CanvasHeight;
        double heightRatio = (double)canvasHeight / viewModel.ViewLayout.GetScrollHeight();
        int halfMinDecorationHeight = MinDecorationHeight / 2;
        int[] x = settings.X;
        int[] w = settings.W;

        if (settings.BackgroundColor is { } background)
        {
            surface.FillRectangle(left, top, canvasWidth, canvasHeight, background);
        }

        // Monaco sorts in place (stable); `OrderBy` is stable too.
        IEnumerable<OverviewRulerDecorationsGroup> groups = viewModel.GetAllOverviewRulerDecorations(Theme)
            .OrderBy(group => group, Comparer<OverviewRulerDecorationsGroup>.Create(OverviewRulerDecorationsGroup.CompareByRenderingProps));
        foreach (OverviewRulerDecorationsGroup group in groups)
        {
            List<int> data = group.Data;
            int prevLane = 0;
            int prevY1 = 0;
            int prevY2 = 0;
            for (int i = 0, len = data.Count / 3; i < len; i++)
            {
                int lane = data[3 * i];
                int startLineNumber = data[(3 * i) + 1];
                int endLineNumber = data[(3 * i) + 2];

                int y1 = (int)(viewModel.ViewLayout.GetVerticalOffsetForLineNumber(startLineNumber) * heightRatio);
                int y2 = (int)((viewModel.ViewLayout.GetVerticalOffsetForLineNumber(endLineNumber) + font.LineHeight) * heightRatio);
                if (y2 - y1 < MinDecorationHeight)
                {
                    int yCenter = (y1 + y2) / 2;
                    if (yCenter < halfMinDecorationHeight)
                    {
                        yCenter = halfMinDecorationHeight;
                    }
                    else if (yCenter + halfMinDecorationHeight > canvasHeight)
                    {
                        yCenter = canvasHeight - halfMinDecorationHeight;
                    }

                    y1 = yCenter - halfMinDecorationHeight;
                    y2 = yCenter + halfMinDecorationHeight;
                }

                if (y1 > prevY2 + 1 || lane != prevLane)
                {
                    // flush prev
                    if (i != 0)
                    {
                        surface.FillRectangle(left + x[prevLane], top + prevY1, w[prevLane], prevY2 - prevY1, group.Color);
                    }

                    prevLane = lane;
                    prevY1 = y1;
                    prevY2 = y2;
                }
                else if (y2 > prevY2)
                {
                    // merge into prev
                    prevY2 = y2;
                }
            }

            surface.FillRectangle(left + x[prevLane], top + prevY1, w[prevLane], prevY2 - prevY1, group.Color);
        }

        if (!settings.HideCursor)
        {
            DrawOverviewRulerCursors(surface, viewModel, selections, settings, left, top, heightRatio);
        }

        if (settings.RenderBorder && settings.BorderColor is { } border)
        {
            // Left and top border, 1 DIP each (Monaco strokes a 1px line at 0 and at 1..width).
            surface.FillRectangle(left, top, 1, canvasHeight, border);
            surface.FillRectangle(left + 1, top, canvasWidth - 1, 1, border);
        }
    }

    private static void DrawOverviewRulerCursors(
        IRenderSurface surface, ViewModel viewModel, IReadOnlyList<Selection> selections, OverviewRulerSettings settings,
        float left, float top, double heightRatio)
    {
        var cursors = new List<(int LineNumber, int Column, ThemeColor? Color)>(selections.Count);
        for (int i = 0; i < selections.Count; i++)
        {
            ThemeColor? color = settings.CursorColorSingle;
            if (selections.Count > 1)
            {
                color = i == 0 ? settings.CursorColorPrimary : settings.CursorColorSecondary;
            }

            cursors.Add((selections[i].PositionLineNumber, selections[i].PositionColumn, color));
        }

        // Monaco: `Array.sort` is stable.
        cursors = [.. cursors.OrderBy(c => c.LineNumber).ThenBy(c => c.Column)];

        int canvasHeight = settings.CanvasHeight;
        const int cursorHeight = 2;
        const int halfCursorHeight = cursorHeight / 2;
        float cursorX = left + settings.X[FullLane];
        int cursorW = settings.W[FullLane];

        int prevY1 = -100;
        int prevY2 = -100;
        ThemeColor? prevColor = null;
        for (int i = 0; i < cursors.Count; i++)
        {
            if (cursors[i].Color is not { } color)
            {
                continue;
            }

            int yCenter = (int)(viewModel.ViewLayout.GetVerticalOffsetForLineNumber(cursors[i].LineNumber) * heightRatio);
            if (yCenter < halfCursorHeight)
            {
                yCenter = halfCursorHeight;
            }
            else if (yCenter + halfCursorHeight > canvasHeight)
            {
                yCenter = canvasHeight - halfCursorHeight;
            }

            int y1 = yCenter - halfCursorHeight;
            int y2 = y1 + cursorHeight;

            if (y1 > prevY2 + 1 || color != prevColor)
            {
                // flush prev
                if (i != 0 && prevColor is { } flushColor)
                {
                    surface.FillRectangle(cursorX, top + prevY1, cursorW, prevY2 - prevY1, flushColor);
                }

                prevY1 = y1;
                prevY2 = y2;
            }
            else if (y2 > prevY2)
            {
                // merge into prev
                prevY2 = y2;
            }

            prevColor = color;
        }

        if (prevColor is { } lastColor)
        {
            surface.FillRectangle(cursorX, top + prevY1, cursorW, prevY2 - prevY1, lastColor);
        }
    }
}

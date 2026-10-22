// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/overviewRuler/decorationsOverviewRuler.ts (Settings)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

/// <summary>What the decorations overview ruler needs to draw: the options, the theme colors and the lanes (Monaco: <c>Settings</c>).</summary>
/// <remarks>
/// Threading: immutable, safe from any thread. Behavior differences: the pixel ratio is 1 (the surface works in DIPs and
/// scales), and <c>equals</c> is not ported (the renderer draws every frame).
/// </remarks>
internal sealed class OverviewRulerSettings
{
    private const double CursorAlpha = 0.7;

    public OverviewRulerSettings(EditorOverviewRulerOptions options, EditorTheme theme, OverviewRulerPosition position)
    {
        Lanes = Math.Clamp(options.Lanes, 0, 3);
        RenderBorder = options.Border;
        BorderColor = theme.GetColor("editorOverviewRuler.border");
        HideCursor = options.HideCursor;
        CursorColorSingle = theme.GetColor("editorCursor.foreground")?.WithAlphaFactor(CursorAlpha);
        CursorColorPrimary = theme.GetColor("editorMultiCursor.primary.foreground")?.WithAlphaFactor(CursorAlpha);
        CursorColorSecondary = theme.GetColor("editorMultiCursor.secondary.foreground")?.WithAlphaFactor(CursorAlpha);

        // Minimap: P6-41 (the editor background when the minimap is on the right).
        const bool minimapOnTheRight = false;
        BackgroundColor = theme.GetColor("editorOverviewRuler.background") ?? (minimapOnTheRight ? theme.Background : null);

        Top = position.Top;
        Right = position.Right;
        Width = position.Width;
        Height = position.Height;
        // Lanes == 0: the overview ruler is off.
        CanvasWidth = Lanes == 0 ? 0 : Width;
        CanvasHeight = Lanes == 0 ? 0 : Height;

        (X, W) = InitLanes(1, CanvasWidth, Lanes);
    }

    public int Lanes { get; }

    public bool RenderBorder { get; }

    public ThemeColor? BorderColor { get; }

    public bool HideCursor { get; }

    public ThemeColor? CursorColorSingle { get; }

    public ThemeColor? CursorColorPrimary { get; }

    public ThemeColor? CursorColorSecondary { get; }

    public ThemeColor? BackgroundColor { get; }

    public int Top { get; }

    public int Right { get; }

    public int Width { get; }

    public int Height { get; }

    public int CanvasWidth { get; }

    public int CanvasHeight { get; }

    /// <summary>Left of each lane combination (index: Left = 1, Center = 2, Right = 4, and their sums).</summary>
    public int[] X { get; }

    /// <summary>Width of each lane combination.</summary>
    public int[] W { get; }

    /// <summary>Monaco: <c>_initLanes</c>.</summary>
    internal static (int[] X, int[] W) InitLanes(int canvasLeftOffset, int canvasWidth, int laneCount)
    {
        int remainingWidth = canvasWidth - canvasLeftOffset;

        if (laneCount >= 3)
        {
            int leftWidth = remainingWidth / 3;
            int rightWidth = remainingWidth / 3;
            int centerWidth = remainingWidth - leftWidth - rightWidth;
            int leftOffset = canvasLeftOffset;
            int centerOffset = leftOffset + leftWidth;
            int rightOffset = leftOffset + leftWidth + centerWidth;

            return (
                [0, leftOffset, centerOffset, leftOffset, rightOffset, leftOffset, centerOffset, leftOffset],
                [
                    0, leftWidth, centerWidth, leftWidth + centerWidth, rightWidth,
                    leftWidth + centerWidth + rightWidth, centerWidth + rightWidth, leftWidth + centerWidth + rightWidth,
                ]);
        }

        if (laneCount == 2)
        {
            int leftWidth = remainingWidth / 2;
            int rightWidth = remainingWidth - leftWidth;
            int leftOffset = canvasLeftOffset;
            int rightOffset = leftOffset + leftWidth;

            return (
                [0, leftOffset, leftOffset, leftOffset, rightOffset, leftOffset, leftOffset, leftOffset],
                [
                    0, leftWidth, leftWidth, leftWidth, rightWidth,
                    leftWidth + rightWidth, leftWidth + rightWidth, leftWidth + rightWidth,
                ]);
        }

        int width = remainingWidth;
        return (
            [0, canvasLeftOffset, canvasLeftOffset, canvasLeftOffset, canvasLeftOffset, canvasLeftOffset, canvasLeftOffset, canvasLeftOffset],
            [0, width, width, width, width, width, width, width]);
    }
}

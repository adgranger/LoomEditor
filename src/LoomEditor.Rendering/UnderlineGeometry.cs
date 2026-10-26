// Ported from microsoft/vscode: src/vs/editor/browser/widget/codeEditor/codeEditorWidget.ts (getSquigglySVGData)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Rendering;

/// <summary>The shapes of the underlines of decorations: a zigzag (error, warning, info) and three dots (hint).</summary>
/// <remarks>
/// Threading: pure functions, safe from any thread. Behavior difference: Monaco repeats a 6 x 3 SVG; here the same
/// zigzag is a polyline drawn with line segments.
/// </remarks>
internal static class UnderlineGeometry
{
    private const float Period = 6;

    // Monaco: `getSquigglySVGData` (6 x 3 zigzag).
    public static IReadOnlyList<(float X, float Y)> SquigglePoints(float left, float width, float bottom)
    {
        if (width <= 0)
        {
            return [];
        }

        List<(float X, float Y)> points = [(left, Y(0, bottom))];
        for (int k = 0; 1.8f + 3 * k < width; k++)
        {
            float t = 1.8f + 3 * k;
            points.Add((left + t, Y(t, bottom)));
        }

        points.Add((left + width, Y(width, bottom)));
        return points;
    }

    public static IReadOnlyList<(float X, float Y, float Size)> DotPositions(float left, float bottom) =>
        new (float, float, float)[] { (left, bottom - 2.5f, 1.5f), (left + 3, bottom - 2.5f, 1.5f), (left + 6, bottom - 2.5f, 1.5f) };

    // 0 at a trough (t = 1.8 + 6k), 1 at a peak (t = 4.8 + 6k).
    private static float Y(float t, float bottom)
    {
        float p = ((t - 1.8f) % Period + Period) % Period;
        float v = p <= 3 ? p / 3 : (Period - p) / 3;
        float trough = bottom - 0.7f;
        return trough + (bottom - 2.3f - trough) * v;
    }
}

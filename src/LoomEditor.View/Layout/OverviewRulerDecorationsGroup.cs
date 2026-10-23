// Ported from microsoft/vscode: src/vs/editor/common/viewModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Themes;

namespace LoomEditor.View.Layout;

/// <summary>
/// The overview ruler decorations of one color and z-index (Monaco: <c>OverviewRulerDecorationsGroup</c>).
/// <see cref="Data"/> holds <c>lane, startLineNumber, endLineNumber</c> triples.
/// </summary>
/// <remarks>
/// Behavior difference: <see cref="CompareByRenderingProps"/> compares the colors as ARGB values, not as CSS strings.
/// Threading: not thread-safe; owned by the caller.
/// </remarks>
/// <param name="color">The resolved color.</param>
/// <param name="zIndex">The z-index of the first decoration of the group.</param>
/// <param name="data">The <c>lane, start, end</c> triples (view line numbers).</param>
internal sealed class OverviewRulerDecorationsGroup(ThemeColor color, int zIndex, List<int> data)
{
    public ThemeColor Color { get; } = color;

    public int ZIndex { get; } = zIndex;

    public List<int> Data { get; } = data;

    /// <summary>Orders by z-index, then by color (Monaco: <c>compareByRenderingProps</c>).</summary>
    public static int CompareByRenderingProps(OverviewRulerDecorationsGroup a, OverviewRulerDecorationsGroup b)
    {
        if (a.ZIndex == b.ZIndex)
        {
            return Argb(a.Color).CompareTo(Argb(b.Color));
        }

        return a.ZIndex - b.ZIndex;
    }

    public static bool Equals(OverviewRulerDecorationsGroup a, OverviewRulerDecorationsGroup b) =>
        a.Color == b.Color && a.ZIndex == b.ZIndex && a.Data.SequenceEqual(b.Data);

    public static bool EqualsArr(IReadOnlyList<OverviewRulerDecorationsGroup> a, IReadOnlyList<OverviewRulerDecorationsGroup> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        for (int i = 0; i < a.Count; i++)
        {
            if (!Equals(a[i], b[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static uint Argb(ThemeColor c) => ((uint)c.A << 24) | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B;
}

// Ported from microsoft/vscode: src/vs/editor/common/viewLayout/viewLineRenderer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;

namespace LoomEditor.Rendering;

/// <summary>
/// The "Show more (…)" label drawn after a line that was cut at <c>stopRenderingLineAfter</c>.
/// Monaco: the <c>showMore</c> message and <c>renderOverflowingCharCount</c> of <c>viewLineRenderer.ts</c>.
/// </summary>
/// <remarks>Threading: stateless, safe from any thread. The text is English only (no localization yet).</remarks>
internal static class OverflowLabel
{
    /// <summary>Formats the label for <paramref name="overflowingCharCount"/> characters that were not rendered.</summary>
    public static string Format(int overflowingCharCount) =>
        string.Create(CultureInfo.InvariantCulture, $"Show more ({FormatCount(overflowingCharCount)})");

    private static string FormatCount(int n)
    {
        if (n < 1024)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{n} chars");
        }

        if (n < 1024 * 1024)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{n / 1024.0:F1} KB");
        }

        return string.Create(CultureInfo.InvariantCulture, $"{n / 1024.0 / 1024.0:F1} MB");
    }
}

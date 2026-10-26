// Ported from microsoft/vscode: src/vs/platform/markers/common/markers.ts (MarkerSeverity)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Markers;

/// <summary>How serious a marker is. The values are bit flags so that <see cref="MarkerService.Read"/> can filter on a mask.</summary>
/// <remarks>Threading: enum, safe to share between threads.</remarks>
public enum MarkerSeverity
{
    /// <summary>A hint (fading, no squiggle in most themes).</summary>
    Hint = 1,
    /// <summary>An informational message.</summary>
    Info = 2,
    /// <summary>A warning.</summary>
    Warning = 4,
    /// <summary>An error.</summary>
    Error = 8,
}

/// <summary>Helpers on <see cref="MarkerSeverity"/> (Monaco <c>MarkerSeverity.compare</c>; the display strings are not ported).</summary>
/// <remarks>Threading: stateless, safe from any thread.</remarks>
public static class MarkerSeverities
{
    /// <summary>Orders the most serious first: negative when <paramref name="a"/> is more serious than <paramref name="b"/>.</summary>
    public static int Compare(MarkerSeverity a, MarkerSeverity b) => (int)b - (int)a;
}

// Ported from microsoft/vscode: src/vs/platform/markers/common/markers.ts (MarkerTag)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Markers;

/// <summary>Extra presentation hint of a marker.</summary>
/// <remarks>Threading: enum, safe to share between threads.</remarks>
public enum MarkerTag
{
    /// <summary>The marked code is unused (rendered faded).</summary>
    Unnecessary = 1,
    /// <summary>The marked code is deprecated (rendered struck through).</summary>
    Deprecated = 2,
}

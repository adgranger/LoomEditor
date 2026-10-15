// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model;

/// <summary>
/// Describes the behavior of decorations and tracked ranges when typing or editing near their edges.
/// Do not edit the values: they are stored in two bits of the interval node metadata.
/// </summary>
/// <remarks>Threading: plain enum; thread-safe.</remarks>
internal enum TrackedRangeStickiness
{
    AlwaysGrowsWhenTypingAtEdges = 0,
    NeverGrowsWhenTypingAtEdges = 1,
    GrowsOnlyWhenTypingBefore = 2,
    GrowsOnlyWhenTypingAfter = 3,
}

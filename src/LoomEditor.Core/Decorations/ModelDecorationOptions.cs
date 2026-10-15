// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;

namespace LoomEditor.Core.Decorations;

/// <summary>
/// The options of a decoration or tracked range (Monaco: <c>ModelDecorationOptions</c>). Minimal in phase 3:
/// only what the interval tree stores in the node metadata.
/// </summary>
/// <remarks>Threading: immutable after initialization; thread-safe.</remarks>
internal sealed class ModelDecorationOptions
{
    public TrackedRangeStickiness Stickiness { get; init; } = TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges;

    public bool CollapseOnReplaceEdit { get; init; }

    public string? ClassName { get; init; }

    public string? GlyphMarginClassName { get; init; }

    public bool AffectsFont { get; init; }
}

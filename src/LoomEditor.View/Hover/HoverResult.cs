// Ported from microsoft/vscode: src/vs/editor/contrib/hover/browser/hoverOperation.ts (HoverResult)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Hover;

/// <summary>The parts found so far by a <see cref="HoverOperation"/>.</summary>
/// <remarks>Threading: immutable once raised.</remarks>
/// <param name="Value">A copy of the parts.</param>
/// <param name="IsComplete">True when nothing more will come.</param>
/// <param name="HasLoadingMessage">True when the "Loading…" message must be shown.</param>
/// <param name="Options">The options of the operation.</param>
internal sealed record HoverResult(IReadOnlyList<IHoverPart> Value, bool IsComplete, bool HasLoadingMessage, HoverComputerOptions Options);

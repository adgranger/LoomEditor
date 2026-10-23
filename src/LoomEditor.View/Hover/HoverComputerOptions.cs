// Ported from microsoft/vscode: src/vs/editor/contrib/hover/browser/contentHoverComputer.ts (ContentHoverComputerOptions)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Hover;

/// <summary>The arguments of a hover computation.</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed record HoverComputerOptions(HoverAnchor Anchor, HoverStartSource Source, bool InsistOnKeepingHoverVisible = false);

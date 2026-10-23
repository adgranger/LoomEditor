// Ported from microsoft/vscode: src/vs/editor/contrib/hover/browser/hoverOperation.ts (HoverStartSource)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Hover;

/// <summary>What started a hover. Monaco's <c>Click</c> is not ported.</summary>
internal enum HoverStartSource
{
    /// <summary>The mouse rested on the anchor.</summary>
    Mouse,

    /// <summary>A keyboard command.</summary>
    Keyboard,
}

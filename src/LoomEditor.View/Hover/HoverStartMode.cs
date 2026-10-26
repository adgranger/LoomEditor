// Ported from microsoft/vscode: src/vs/editor/contrib/hover/browser/hoverOperation.ts (HoverStartMode)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Hover;

/// <summary>How <see cref="HoverOperation"/> starts.</summary>
internal enum HoverStartMode
{
    /// <summary>Wait for the hover delay.</summary>
    Delayed,

    /// <summary>Compute now.</summary>
    Immediate,
}

// Ported from microsoft/vscode: src/vs/editor/common/encodedTokenAttributes.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

/// <summary>The fixed color ids (Monaco: <c>ColorId</c>, an "open ended enum at runtime": ids are plain integers).</summary>
/// <remarks>Threading: constants.</remarks>
internal static class ColorId
{
    /// <summary>No color.</summary>
    public const int None = 0;

    /// <summary>The default foreground color.</summary>
    public const int DefaultForeground = 1;

    /// <summary>The default background color.</summary>
    public const int DefaultBackground = 2;
}

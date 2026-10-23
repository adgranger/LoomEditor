// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Decorations;

/// <summary>Where a decoration is drawn in the minimap (Monaco: <c>MinimapPosition</c>).</summary>
/// <remarks>Threading: a plain enum; thread-safe. The values are Monaco's.</remarks>
internal enum MinimapPosition
{
    Inline = 1,
    Gutter = 2,
}

/// <summary>The style of a minimap section header (Monaco: <c>MinimapSectionHeaderStyle</c>).</summary>
/// <remarks>Threading: a plain enum; thread-safe. The values are Monaco's.</remarks>
internal enum MinimapSectionHeaderStyle
{
    Normal = 1,
    Underlined = 2,
}

// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Decorations;

/// <summary>
/// How a decoration is marked in the minimap (Monaco: <c>IModelDecorationMinimapOptions</c> and
/// <c>ModelDecorationMinimapOptions</c>).
/// </summary>
/// <remarks>
/// Not ported: <c>getColor</c> and <c>invalidateCachedColor</c>; the view resolves the colors with <c>DecorationColorResolver</c>. The section header
/// members are plain data: section headers are not drawn. Threading: immutable; thread-safe.
/// </remarks>
/// <param name="Color">The color, in light themes.</param>
/// <param name="DarkColor">The color, in dark themes.</param>
/// <param name="Position">Where the decoration is drawn.</param>
/// <param name="SectionHeaderStyle">The section header style, if the decoration is a section header.</param>
/// <param name="SectionHeaderText">The section header text.</param>
internal sealed record ModelDecorationMinimapOptions(
    DecorationColor? Color,
    DecorationColor? DarkColor,
    MinimapPosition Position,
    MinimapSectionHeaderStyle? SectionHeaderStyle = null,
    string? SectionHeaderText = null);

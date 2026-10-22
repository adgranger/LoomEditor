// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Decorations;

/// <summary>
/// How a decoration is marked in the overview ruler (Monaco: <c>IModelDecorationOverviewRulerOptions</c> and
/// <c>ModelDecorationOverviewRulerOptions</c>). A <c>null</c> color means no mark (Monaco: <c>color || ''</c>).
/// </summary>
/// <remarks>
/// Not ported: <c>getColor</c> and <c>invalidateCachedColor</c>; the view resolves the colors. Threading: immutable; thread-safe.
/// </remarks>
/// <param name="Color">The color, in light themes.</param>
/// <param name="DarkColor">The color, in dark themes.</param>
/// <param name="Position">The lane; <see cref="OverviewRulerLane.Center"/> by default.</param>
internal sealed record ModelDecorationOverviewRulerOptions(
    DecorationColor? Color,
    DecorationColor? DarkColor,
    OverviewRulerLane Position = OverviewRulerLane.Center);

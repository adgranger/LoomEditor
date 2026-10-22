// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Decorations;

/// <summary>Vertical lane in the overview ruler of the editor (Monaco: <c>OverviewRulerLane</c>).</summary>
/// <remarks>Threading: a plain enum; thread-safe. The values are Monaco's: the renderer indexes arrays with them.</remarks>
internal enum OverviewRulerLane
{
    Left = 1,
    Center = 2,
    Right = 4,
    Full = 7,
}

// Ported from microsoft/vscode: src/vs/editor/common/viewModel.ts (MinimapLinesRenderingData)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>The tab size and the data of the view lines the minimap draws; a line that was not needed is <see langword="null"/>.</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed record MinimapLinesRenderingData(int TabSize, ViewLineData?[] Data);

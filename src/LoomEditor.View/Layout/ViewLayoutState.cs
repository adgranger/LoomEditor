// Ported from microsoft/vscode: src/vs/editor/common/viewLayout/viewLayout.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>The scroll state to save and restore. Monaco: the return value of <c>ViewLayout.saveState</c>.</summary>
/// <remarks>Threading: immutable value, safe from any thread.</remarks>
internal readonly record struct ViewLayoutState(double ScrollTop, double ScrollTopWithoutViewZones, double ScrollLeft);

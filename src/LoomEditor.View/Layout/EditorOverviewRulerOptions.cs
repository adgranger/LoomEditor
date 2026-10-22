// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts (overviewRulerLanes, overviewRulerBorder, hideCursorInOverviewRuler)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>The decorations overview ruler options.</summary>
/// <remarks>Threading: immutable, safe from any thread. Values out of range are clamped by the renderer.</remarks>
/// <param name="Lanes">Monaco: <c>overviewRulerLanes</c> (0–3, default 3); 0 hides the ruler.</param>
/// <param name="Border">Monaco: <c>overviewRulerBorder</c> (default true).</param>
/// <param name="HideCursor">Monaco: <c>hideCursorInOverviewRuler</c> (default false).</param>
internal sealed record EditorOverviewRulerOptions(int Lanes = 3, bool Border = true, bool HideCursor = false);

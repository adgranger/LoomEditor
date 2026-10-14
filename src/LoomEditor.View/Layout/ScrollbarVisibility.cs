// Ported from microsoft/vscode: src/vs/base/common/scrollable.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>Whether a scrollbar is shown. Monaco: <c>ScrollbarVisibility</c>.</summary>
/// <remarks>Threading: immutable value, safe from any thread.</remarks>
internal enum ScrollbarVisibility
{
    Auto = 1,
    Hidden = 2,
    Visible = 3,
}

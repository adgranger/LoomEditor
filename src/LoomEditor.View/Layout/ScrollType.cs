// Ported from microsoft/vscode: src/vs/editor/common/editorCommon.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>How a scroll position change is applied. Monaco: <c>ScrollType</c>.</summary>
/// <remarks>Threading: immutable value, safe from any thread.</remarks>
internal enum ScrollType
{
    Smooth = 0,
    Immediate = 1,
}

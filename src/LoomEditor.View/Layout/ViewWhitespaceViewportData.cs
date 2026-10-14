// Ported from microsoft/vscode: src/vs/editor/common/viewModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>A whitespace that is laid out in a viewport.</summary>
/// <remarks>Threading: immutable value type; safe to share across threads.</remarks>
internal readonly record struct ViewWhitespaceViewportData(string Id, int AfterLineNumber, int VerticalOffset, int Height);

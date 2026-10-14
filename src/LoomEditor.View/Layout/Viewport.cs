// Ported from microsoft/vscode: src/vs/editor/common/viewModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>A rectangle of the editor scroll surface, in DIPs.</summary>
/// <remarks>Threading: immutable value type; safe to share across threads.</remarks>
internal readonly record struct Viewport(int Top, int Left, int Width, int Height);

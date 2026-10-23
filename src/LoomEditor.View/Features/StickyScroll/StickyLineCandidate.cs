// Ported from microsoft/vscode: src/vs/editor/contrib/stickyScroll/browser/stickyScrollProvider.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.StickyScroll;

/// <summary>A line that may stick, with its position in the scrolled content. Monaco: <c>StickyLineCandidate</c>.</summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
internal sealed record StickyLineCandidate(int StartLineNumber, int EndLineNumber, int Top, int Height);

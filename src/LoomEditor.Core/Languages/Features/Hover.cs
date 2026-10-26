// Ported from microsoft/vscode: src/vs/editor/common/languages.ts (Hover)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Languages.Features;

/// <summary>A hover result: Markdown contents and the range they apply to.</summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
/// <param name="Contents">The Markdown blocks.</param>
/// <param name="Range">The range the hover applies to; null means the word at the position.</param>
public sealed record Hover(IReadOnlyList<MarkdownString> Contents, Range? Range = null);

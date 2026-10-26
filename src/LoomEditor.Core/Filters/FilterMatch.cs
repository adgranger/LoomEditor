// Ported from microsoft/vscode: src/vs/base/common/filters.ts (IMatch)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Filters;

/// <summary>A matched range of a word, end exclusive (Monaco: <c>IMatch</c>).</summary>
/// <remarks>Threading: immutable value type, safe from any thread.</remarks>
internal readonly record struct FilterMatch(int Start, int End);

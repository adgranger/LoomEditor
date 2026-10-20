// Ported from microsoft/vscode: src/vs/editor/common/textModelBracketPairs.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.BracketPairs;

/// <summary>A bracket of the text with its nesting levels (Monaco: <c>BracketInfo</c>).</summary>
/// <param name="Range">The range of the bracket text.</param>
/// <param name="NestingLevel">The 0-based nesting level.</param>
/// <param name="NestingLevelOfEqualBracketType">The 0-based nesting level among the brackets of the same type.</param>
/// <param name="IsInvalid">Whether the bracket is unmatched.</param>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed record BracketInfo(Range Range, int NestingLevel, int NestingLevelOfEqualBracketType, bool IsInvalid);

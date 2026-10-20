// Ported from microsoft/vscode: src/vs/editor/common/textModelBracketPairs.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Supports;

namespace LoomEditor.Core.Model.BracketPairs;

/// <summary>A bracket found in the text (Monaco: <c>IFoundBracket</c>).</summary>
/// <param name="Range">The range of the bracket text.</param>
/// <param name="BracketInfo">The bracket kind: an <see cref="OpeningBracketKind"/> or a <see cref="ClosingBracketKind"/>.</param>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal readonly record struct FoundBracket(Range Range, BracketKind BracketInfo);

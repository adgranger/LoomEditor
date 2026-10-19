// Ported from microsoft/vscode: src/vs/editor/common/languages.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

/// <summary>The result of tokenizing a line (Monaco: <c>EncodedTokenizationResult</c>, without <c>fontInfo</c>).</summary>
/// <param name="Tokens">
/// The tokens in binary format. Each token occupies two array indices: for token i, <c>2*i</c> is its start index and
/// <c>2*i + 1</c> its metadata (see <see cref="TokenMetadata"/>).
/// </param>
/// <param name="EndState">The state at the end of the line.</param>
/// <remarks>Threading: immutable by convention (the array is not copied).</remarks>
internal sealed record EncodedTokenizationResult(uint[] Tokens, IState EndState);

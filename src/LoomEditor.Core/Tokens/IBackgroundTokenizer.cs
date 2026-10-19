// Ported from microsoft/vscode: src/vs/editor/common/languages.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

/// <summary>
/// Tokenizes a model in the background (Monaco: <c>IBackgroundTokenizer</c>). Not ported:
/// <c>reportMismatchingTokens</c> (debugging).
/// </summary>
/// <remarks>Threading: see the implementation.</remarks>
internal interface IBackgroundTokenizer : IDisposable
{
    /// <summary>
    /// <c>requestTokens</c>: instructs the background tokenizer to set the tokens for the given range again. This might
    /// be necessary if the renderer overwrote those tokens with heuristically computed ones for some viewport, when the
    /// change does not even propagate to that viewport.
    /// </summary>
    void RequestTokens(int startLineNumber, int endLineNumberExclusive);
}

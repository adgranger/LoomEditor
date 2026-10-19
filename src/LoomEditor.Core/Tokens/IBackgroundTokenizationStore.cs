// Ported from microsoft/vscode: src/vs/editor/common/languages.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

/// <summary>Where a background tokenizer delivers its results (Monaco: <c>IBackgroundTokenizationStore</c>, without <c>setFontInfo</c>).</summary>
/// <remarks>Threading: see the implementation.</remarks>
internal interface IBackgroundTokenizationStore
{
    /// <summary><c>setTokens</c>.</summary>
    void SetTokens(IReadOnlyList<ContiguousMultilineTokens> tokens);

    /// <summary><c>setEndState</c>.</summary>
    void SetEndState(int lineNumber, IState state);

    /// <summary>
    /// <c>backgroundTokenizationFinished</c>: indicates that the background tokenization has finished for now. (This
    /// triggers bracket pair colorization to re-parse the bracket pairs with token information.)
    /// </summary>
    void BackgroundTokenizationFinished();
}

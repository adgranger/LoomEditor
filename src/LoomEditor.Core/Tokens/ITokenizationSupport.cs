// Ported from microsoft/vscode: src/vs/editor/common/languages.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

/// <summary>
/// A tokenizer for a language (Monaco: <c>ITokenizationSupport</c>). Not ported: the string <c>tokenize</c>;
/// <c>createBackgroundTokenizer</c> comes with the text model tokenization (P4-29).
/// </summary>
/// <remarks>Threading: an implementation is used by one thread at a time.</remarks>
internal interface ITokenizationSupport
{
    /// <summary>
    /// <c>backgroundTokenizerShouldOnlyVerifyTokens</c>: if true, the background tokenizer will only be used to verify
    /// tokens against the default background tokenizer. Used for debugging.
    /// </summary>
    bool? BackgroundTokenizerShouldOnlyVerifyTokens => null;

    /// <summary><c>getInitialState</c>.</summary>
    IState GetInitialState();

    /// <summary><c>tokenizeEncoded</c>.</summary>
    EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state);
}

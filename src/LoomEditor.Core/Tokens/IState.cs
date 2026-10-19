// Ported from microsoft/vscode: src/vs/editor/common/languages.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

/// <summary>
/// The state of the tokenizer between two lines. It is useful to store flags such as in multiline comment, etc. The
/// model will clone the previous line's state and pass it in to tokenize the next line (Monaco: <c>IState</c>).
/// </summary>
/// <remarks>Threading: an implementation is immutable or used by one thread at a time.</remarks>
internal interface IState
{
    /// <summary><c>clone</c>.</summary>
    IState Clone();

    /// <summary><c>equals</c>.</summary>
    bool Equals(IState? other);
}

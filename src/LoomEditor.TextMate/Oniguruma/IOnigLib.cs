// Ported from microsoft/vscode-textmate: src/onigLib.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Oniguruma;

/// <summary><c>IOnigLib</c>: creates scanners and strings for the tokenizer.</summary>
/// <remarks>Threading: an implementation is thread-safe; what it creates is used by the thread that created it.</remarks>
internal interface IOnigLib
{
    /// <summary><c>createOnigScanner</c>: compiles <paramref name="sources"/>.</summary>
    /// <exception cref="OnigException">A pattern does not compile.</exception>
    IOnigScanner CreateOnigScanner(IReadOnlyList<string> sources);

    /// <summary><c>createOnigString</c>.</summary>
    IOnigString CreateOnigString(string str);
}

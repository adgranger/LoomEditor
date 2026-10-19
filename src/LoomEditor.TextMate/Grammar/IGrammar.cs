// Ported from microsoft/vscode-textmate: src/main.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>IGrammar</c>: a grammar that tokenizes text line by line.</summary>
/// <remarks>Threading: one thread at a time (see <see cref="Grammar"/>).</remarks>
internal interface IGrammar
{
    /// <summary>
    /// <c>tokenizeLine</c>: tokenizes <paramref name="lineText"/> using the previous line state
    /// <paramref name="prevState"/> (<see langword="null"/> for the first line).
    /// </summary>
    /// <param name="lineText">The line, without its line terminator.</param>
    /// <param name="prevState">The state at the end of the previous line.</param>
    /// <param name="timeLimit">A time limit in milliseconds; <c>0</c> for none.</param>
    TokenizeLineResult TokenizeLine(string lineText, StateStack? prevState, int timeLimit = 0);

    /// <summary>
    /// <c>tokenizeLine2</c>: like <see cref="TokenizeLine"/>, with the tokens in binary format (start index and
    /// metadata, see <see cref="EncodedTokenAttributes"/>: language, token type, font style, colors).
    /// </summary>
    /// <param name="lineText">The line, without its line terminator.</param>
    /// <param name="prevState">The state at the end of the previous line.</param>
    /// <param name="timeLimit">A time limit in milliseconds; <c>0</c> for none.</param>
    TokenizeLineResult2 TokenizeLine2(string lineText, StateStack? prevState, int timeLimit = 0);
}

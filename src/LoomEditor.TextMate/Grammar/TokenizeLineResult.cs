// Ported from microsoft/vscode-textmate: src/main.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>IToken</c>: a token of a line, with the scope names that apply to it.</summary>
/// <param name="StartIndex"><c>startIndex</c>.</param>
/// <param name="EndIndex"><c>endIndex</c>.</param>
/// <param name="Scopes"><c>scopes</c>: outermost first.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record Token(int StartIndex, int EndIndex, IReadOnlyList<string> Scopes);

/// <summary><c>ITokenizeLineResult</c> (without <c>fonts</c>, ADR 0008 decision 3).</summary>
/// <param name="Tokens"><c>tokens</c>.</param>
/// <param name="RuleStack"><c>ruleStack</c>: the <c>prevState</c> to pass on to the next line tokenization.</param>
/// <param name="StoppedEarly"><c>stoppedEarly</c>: did tokenization stop early due to reaching the time limit.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record TokenizeLineResult(IReadOnlyList<Token> Tokens, StateStack RuleStack, bool StoppedEarly);

/// <summary><c>ITokenizeLineResult2</c> (without <c>fonts</c>, ADR 0008 decision 3).</summary>
/// <param name="Tokens">
/// <c>tokens</c> in binary format: each token occupies two entries; for token i, <c>2*i</c> is its start index and
/// <c>2*i + 1</c> its metadata (see <see cref="EncodedTokenAttributes"/>).
/// </param>
/// <param name="RuleStack"><c>ruleStack</c>.</param>
/// <param name="StoppedEarly"><c>stoppedEarly</c>.</param>
/// <remarks>Threading: immutable by convention (the array is not copied).</remarks>
internal sealed record TokenizeLineResult2(uint[] Tokens, StateStack RuleStack, bool StoppedEarly);

/// <summary>Upstream's <c>TokenTypeMatcher</c>: a scope selector (<c>tokenTypes</c>) and the token type it forces.</summary>
/// <param name="Matcher">The compiled selector, matching a list of scope names.</param>
/// <param name="Type">The standard token type.</param>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct TokenTypeMatcher(Func<string[], bool> Matcher, StandardTokenType Type);

// Ported from microsoft/vscode-textmate: src/main.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>IGrammarConfiguration</c>: optional settings of a grammar.</summary>
/// <param name="EmbeddedLanguages"><c>embeddedLanguages</c>: scope name → language id (<c>IEmbeddedLanguagesMap</c>); do not use language id 0.</param>
/// <param name="TokenTypes"><c>tokenTypes</c>: selector → token type (<c>ITokenTypeMap</c>), in the order they apply.</param>
/// <param name="BalancedBracketSelectors"><c>balancedBracketSelectors</c>.</param>
/// <param name="UnbalancedBracketSelectors"><c>unbalancedBracketSelectors</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record GrammarConfiguration(
    IReadOnlyDictionary<string, int>? EmbeddedLanguages = null,
    IReadOnlyDictionary<string, StandardTokenType>? TokenTypes = null,
    IReadOnlyList<string>? BalancedBracketSelectors = null,
    IReadOnlyList<string>? UnbalancedBracketSelectors = null);

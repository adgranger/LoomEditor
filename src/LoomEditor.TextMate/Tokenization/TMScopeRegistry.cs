// Ported from microsoft/vscode: src/vs/workbench/services/textMate/common/TMScopeRegistry.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using LoomEditor.TextMate.Grammar;

namespace LoomEditor.TextMate.Tokenization;

/// <summary>A validated grammar definition (VS Code: <c>IValidGrammarDefinition</c>): languages are encoded ids, token types are enums.</summary>
/// <param name="Location">The resource name of the grammar file (VS Code: a <c>URI</c>).</param>
/// <param name="Language">The language this grammar is the grammar of, or <see langword="null"/> if only other grammars include it.</param>
/// <param name="ScopeName"><c>scopeName</c>.</param>
/// <param name="EmbeddedLanguages"><c>embeddedLanguages</c>: scope name → encoded language id.</param>
/// <param name="TokenTypes"><c>tokenTypes</c>: selector → token type.</param>
/// <param name="InjectTo"><c>injectTo</c>.</param>
/// <param name="BalancedBracketSelectors"><c>balancedBracketSelectors</c>.</param>
/// <param name="UnbalancedBracketSelectors"><c>unbalancedBracketSelectors</c>.</param>
/// <remarks>Threading: immutable (VS Code's <c>sourceExtensionId</c> is not ported: there are no extensions).</remarks>
internal sealed record ValidGrammarDefinition(
    string Location,
    string? Language,
    string ScopeName,
    IReadOnlyDictionary<string, int> EmbeddedLanguages,
    IReadOnlyDictionary<string, StandardTokenType> TokenTypes,
    IReadOnlyList<string>? InjectTo,
    IReadOnlyList<string> BalancedBracketSelectors,
    IReadOnlyList<string> UnbalancedBracketSelectors);

/// <summary>Maps scope names to the definition of their grammar (VS Code: <c>TMScopeRegistry</c>).</summary>
/// <remarks>Threading: not thread-safe (one per thread).</remarks>
internal sealed class TMScopeRegistry
{
    private readonly Dictionary<string, ValidGrammarDefinition> _scopeNameToLanguageRegistration = new(StringComparer.Ordinal);

    /// <summary><c>reset</c>.</summary>
    public void Reset() => _scopeNameToLanguageRegistration.Clear();

    /// <summary><c>register</c>: a second definition of a scope name replaces the first one.</summary>
    public void Register(ValidGrammarDefinition def)
    {
        if (_scopeNameToLanguageRegistration.TryGetValue(def.ScopeName, out ValidGrammarDefinition? existing)
            && !string.Equals(existing.Location, def.Location, StringComparison.Ordinal))
        {
            Debug.WriteLine(
                $"Overwriting grammar scope name to file mapping for scope {def.ScopeName}.\n"
                + $"Old grammar file: {existing.Location}.\nNew grammar file: {def.Location}");
        }

        _scopeNameToLanguageRegistration[def.ScopeName] = def;
    }

    /// <summary><c>getGrammarDefinition</c>.</summary>
    public ValidGrammarDefinition? GetGrammarDefinition(string scopeName) =>
        _scopeNameToLanguageRegistration.GetValueOrDefault(scopeName);
}

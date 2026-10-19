// Ported from microsoft/vscode-textmate: src/rule.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Oniguruma;
using LoomEditor.TextMate.Parsing;

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>IRuleRegistry</c>: rules by id (implemented by the grammar).</summary>
/// <remarks>Threading: single-threaded, like the grammar.</remarks>
internal interface IRuleRegistry
{
    /// <summary>
    /// <c>getRule</c>. <see langword="null"/> while the rule is being created (its factory has not returned yet), as
    /// upstream returns <c>undefined</c>.
    /// </summary>
    Rule? GetRule(RuleId ruleId);

    /// <summary><c>registerRule</c>: gives the factory a new id and stores the rule it returns.</summary>
    T RegisterRule<T>(Func<RuleId, T> factory)
        where T : Rule;
}

/// <summary><c>IGrammarRegistry</c>: finds the grammars that an <c>include</c> refers to.</summary>
/// <remarks>Threading: single-threaded, like the grammar.</remarks>
internal interface IGrammarRegistry
{
    /// <summary><c>getExternalGrammar</c>: the grammar of <paramref name="scopeName"/>, initialized, or <see langword="null"/>.</summary>
    RawGrammar? GetExternalGrammar(string scopeName, RawRepository repository);
}

/// <summary><c>IRuleFactoryHelper</c>.</summary>
/// <remarks>Threading: single-threaded, like the grammar.</remarks>
internal interface IRuleFactoryHelper : IRuleRegistry, IGrammarRegistry
{
}

/// <summary>Upstream's intersection type <c>IRuleRegistry &amp; IOnigLib</c> (what rules compile against).</summary>
/// <remarks>Threading: single-threaded, like the grammar.</remarks>
internal interface IRuleRegistryAndOnigLib : IRuleRegistry, IOnigLib
{
}

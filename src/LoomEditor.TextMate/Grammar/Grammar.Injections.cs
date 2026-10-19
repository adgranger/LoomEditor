// Ported from microsoft/vscode-textmate: src/grammar/grammar.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Parsing;

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>Injection</c>: a rule that is tried at every position of the tokenized text when its selector matches.</summary>
/// <param name="DebugSelector"><c>debugSelector</c>.</param>
/// <param name="Matcher"><c>matcher</c>: matches the scope names of the current stack.</param>
/// <param name="Priority"><c>priority</c>: 0 is the default, -1 for <c>L:</c> and 1 for <c>R:</c>.</param>
/// <param name="RuleId"><c>ruleId</c>.</param>
/// <param name="Grammar"><c>grammar</c>: the raw grammar that the rule belongs to.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record Injection(
    string DebugSelector,
    Func<string[], bool> Matcher,
    int Priority,
    RuleId RuleId,
    RawGrammar Grammar);

// _collectInjections, getInjections and collectInjections.
internal sealed partial class Grammar
{
    /// <summary><c>getInjections</c>: collected on the first call, sorted by priority.</summary>
    public List<Injection> GetInjections() => _injections ??= CollectInjections();

    private List<Injection> CollectInjections()
    {
        var result = new List<Injection>();

        // upstream: lookup(this._rootScopeName) always finds the grammar itself.
        RawGrammar grammar = _grammar;

        // add injections from the current grammar
        if (grammar.Injections is not null)
        {
            foreach (KeyValuePair<string, RawRule> entry in grammar.Injections.Entries)
            {
                AddInjections(result, entry.Key, entry.Value, grammar);
            }
        }

        // add injection grammars contributed for the current scope
        foreach (string injectionScopeName in _grammarRepository.Injections(_rootScopeName))
        {
            RawGrammar? injectionGrammar = GetExternalGrammar(injectionScopeName);
            string? selector = injectionGrammar?.InjectionSelector;
            if (injectionGrammar is not null && !string.IsNullOrEmpty(selector))
            {
                // An injection grammar is compiled like a rule with the grammar's name and patterns (upstream passes
                // the IRawGrammar where an IRawRule is expected).
                var asRule = new RawRule
                {
                    Name = injectionGrammar.Name,
                    Patterns = injectionGrammar.Patterns,
                    Repository = injectionGrammar.Repository,
                };
                AddInjections(result, selector, asRule, injectionGrammar);
            }
        }

        // sort by priority (stable, like Array.prototype.sort)
        return [.. result.OrderBy(i => i.Priority)];
    }

    // collectInjections
    private void AddInjections(List<Injection> result, string selector, RawRule rule, RawGrammar grammar)
    {
        List<MatcherWithPriority<string[]>> matchers = Matcher.CreateMatchers<string[]>(selector, NameMatcher.Matches);
        RuleId ruleId = RuleFactory.GetCompiledRuleId(rule, this, grammar.Repository!);
        foreach (MatcherWithPriority<string[]> matcher in matchers)
        {
            result.Add(new Injection(selector, matcher.Matcher, matcher.Priority, ruleId, grammar));
        }
    }
}

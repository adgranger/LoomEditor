// Ported from microsoft/vscode-textmate: src/grammar/grammarDependencies.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Parsing;

namespace LoomEditor.TextMate.Grammar;

// collectReferencesOfReference and the collectExternalReferencesIn* functions.
internal sealed partial class ScopeDependencyProcessor
{
    // Upstream's Context (Repository == null) and ContextWithRepository.
    private readonly record struct Context(RawGrammar BaseGrammar, RawGrammar SelfGrammar, RawRepository? Repository);

    private void CollectReferencesOfReference(AbsoluteRuleReference reference, ExternalReferenceCollector result)
    {
        RawGrammar? selfGrammar = _lookup(reference.ScopeName);
        if (selfGrammar is null)
        {
            if (reference.ScopeName == InitialScopeName)
            {
                throw new InvalidOperationException($"No grammar provided for <{InitialScopeName}>");
            }

            return;
        }

        RawGrammar baseGrammar = _lookup(InitialScopeName)!;

        if (reference is TopLevelRepositoryRuleReference repositoryReference)
        {
            CollectInTopLevelRepositoryRule(
                repositoryReference.RuleName,
                new Context(baseGrammar, selfGrammar, selfGrammar.Repository),
                result);
        }
        else
        {
            CollectInTopLevelRule(new Context(baseGrammar, selfGrammar, null), result);
        }

        IReadOnlyList<string>? injections = _injections(reference.ScopeName);
        if (injections is not null)
        {
            foreach (string injection in injections)
            {
                result.Add(new TopLevelRuleReference(injection));
            }
        }
    }

    private static void CollectInTopLevelRepositoryRule(string ruleName, Context context, ExternalReferenceCollector result)
    {
        if (context.Repository is not null && context.Repository.TryGetValue(ruleName, out RawRule? rule))
        {
            CollectInRules([rule], context, result);
        }
    }

    private static void CollectInTopLevelRule(Context context, ExternalReferenceCollector result)
    {
        RawGrammar selfGrammar = context.SelfGrammar;
        if (selfGrammar.Patterns is not null)
        {
            CollectInRules(selfGrammar.Patterns, context with { Repository = selfGrammar.Repository }, result);
        }

        if (selfGrammar.Injections is not null)
        {
            List<RawRule> injectionRules = [];
            foreach (KeyValuePair<string, RawRule> entry in selfGrammar.Injections.Entries)
            {
                injectionRules.Add(entry.Value);
            }

            CollectInRules(injectionRules, context with { Repository = selfGrammar.Repository }, result);
        }
    }

    private static void CollectInRules(IReadOnlyList<RawRule> rules, Context context, ExternalReferenceCollector result)
    {
        foreach (RawRule rule in rules)
        {
            if (!result.VisitedRule.Add(rule))
            {
                continue;
            }

            RawRepository? patternRepository = rule.Repository is null
                ? context.Repository
                : Merge(context.Repository, rule.Repository);

            if (rule.Patterns is not null)
            {
                CollectInRules(rule.Patterns, context with { Repository = patternRepository }, result);
            }

            if (string.IsNullOrEmpty(rule.Include))
            {
                continue;
            }

            IncludeReference reference = IncludeReference.Parse(rule.Include);

            switch (reference)
            {
                case BaseReference:
                    CollectInTopLevelRule(context with { SelfGrammar = context.BaseGrammar }, result);
                    break;
                case SelfReference:
                    CollectInTopLevelRule(context, result);
                    break;
                case RelativeReference relative:
                    CollectInTopLevelRepositoryRule(relative.RuleName, context with { Repository = patternRepository }, result);
                    break;
                case TopLevelReference topLevel:
                    CollectTopLevel(topLevel.ScopeName, null, context, patternRepository, result);
                    break;
                case TopLevelRepositoryReference topLevelRepository:
                    CollectTopLevel(topLevelRepository.ScopeName, topLevelRepository.RuleName, context, patternRepository, result);
                    break;
            }
        }
    }

    // The TopLevelReference / TopLevelRepositoryReference cases (ruleName == null for the former).
    private static void CollectTopLevel(
        string scopeName,
        string? ruleName,
        Context context,
        RawRepository? patternRepository,
        ExternalReferenceCollector result)
    {
        RawGrammar? selfGrammar =
            scopeName == context.SelfGrammar.ScopeName ? context.SelfGrammar
            : scopeName == context.BaseGrammar.ScopeName ? context.BaseGrammar
            : null;

        if (selfGrammar is not null)
        {
            var newContext = new Context(context.BaseGrammar, selfGrammar, patternRepository);
            if (ruleName is not null)
            {
                CollectInTopLevelRepositoryRule(ruleName, newContext, result);
            }
            else
            {
                CollectInTopLevelRule(newContext, result);
            }
        }
        else if (ruleName is not null)
        {
            result.Add(new TopLevelRepositoryRuleReference(scopeName, ruleName));
        }
        else
        {
            result.Add(new TopLevelRuleReference(scopeName));
        }
    }

    // mergeObjects({}, repository, ruleRepository): a new map, the rule's own repository wins.
    private static RawRepository Merge(RawRepository? repository, RawRepository ruleRepository)
    {
        var merged = new RawRepository();
        if (repository is not null)
        {
            foreach (KeyValuePair<string, RawRule> entry in repository.Entries)
            {
                merged[entry.Key] = entry.Value;
            }
        }

        foreach (KeyValuePair<string, RawRule> entry in ruleRepository.Entries)
        {
            merged[entry.Key] = entry.Value;
        }

        return merged;
    }
}

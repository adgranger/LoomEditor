// Ported from microsoft/vscode-textmate: src/rule.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Parsing;

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>RuleFactory</c>: compiles raw rules into <see cref="Rule"/>s, resolving <c>include</c>s.</summary>
/// <remarks>
/// <para>
/// The id of a compiled rule is written into its raw rule (<see cref="RawRule.Id"/>): the raw grammar is a clone owned
/// by the grammar (phase-4/README.md, trap 7), so each raw rule is compiled once.
/// </para>
/// <para>Threading: stateless; the helper (the grammar) is single-threaded.</para>
/// </remarks>
internal static partial class RuleFactory
{
    /// <summary><c>createCaptureRule</c>.</summary>
    public static CaptureRule CreateCaptureRule(IRuleFactoryHelper helper, string? name, string? contentName, RuleId retokenizeCapturedWithRuleId)
    {
        ArgumentNullException.ThrowIfNull(helper);
        return helper.RegisterRule(id => new CaptureRule(id, name, contentName, retokenizeCapturedWithRuleId));
    }

    /// <summary><c>getCompiledRuleId</c>: the id of the rule compiled from <paramref name="desc"/> (compiled on the first call).</summary>
    public static RuleId GetCompiledRuleId(RawRule desc, IRuleFactoryHelper helper, RawRepository repository)
    {
        ArgumentNullException.ThrowIfNull(desc);
        ArgumentNullException.ThrowIfNull(helper);
        if (desc.Id is null or 0)
        {
            helper.RegisterRule<Rule>(id =>
            {
                desc.Id = id.Value;

                if (!string.IsNullOrEmpty(desc.Match))
                {
                    return new MatchRule(id, desc.Name, desc.Match, CompileCaptures(desc.Captures, helper, repository));
                }

                if (desc.Begin is null)
                {
                    if (desc.Repository is not null)
                    {
                        repository = Merge(repository, desc.Repository);
                    }

                    List<RawRule>? patterns = desc.Patterns;
                    if (patterns is null && !string.IsNullOrEmpty(desc.Include))
                    {
                        patterns = [new RawRule { Include = desc.Include }];
                    }

                    return new IncludeOnlyRule(id, desc.Name, desc.ContentName, CompilePatterns(patterns, helper, repository));
                }

                if (!string.IsNullOrEmpty(desc.While))
                {
                    return new BeginWhileRule(
                        id,
                        desc.Name,
                        desc.ContentName,
                        desc.Begin, CompileCaptures(desc.BeginCaptures ?? desc.Captures, helper, repository),
                        desc.While, CompileCaptures(desc.WhileCaptures ?? desc.Captures, helper, repository),
                        CompilePatterns(desc.Patterns, helper, repository));
                }

                return new BeginEndRule(
                    id,
                    desc.Name,
                    desc.ContentName,
                    desc.Begin, CompileCaptures(desc.BeginCaptures ?? desc.Captures, helper, repository),
                    desc.End, CompileCaptures(desc.EndCaptures ?? desc.Captures, helper, repository),
                    desc.ApplyEndPatternLast,
                    CompilePatterns(desc.Patterns, helper, repository));
            });
        }

        return new RuleId(desc.Id!.Value);
    }

    // mergeObjects({}, repository, desc.repository): a new map, the rule's own repository wins.
    private static RawRepository Merge(RawRepository repository, RawRepository ruleRepository)
    {
        var merged = new RawRepository();
        foreach (KeyValuePair<string, RawRule> entry in repository.Entries)
        {
            merged[entry.Key] = entry.Value;
        }

        foreach (KeyValuePair<string, RawRule> entry in ruleRepository.Entries)
        {
            merged[entry.Key] = entry.Value;
        }

        return merged;
    }
}

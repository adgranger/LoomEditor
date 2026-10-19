// Ported from microsoft/vscode-textmate: src/rule.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Parsing;

namespace LoomEditor.TextMate.Grammar;

// _compileCaptures and _compilePatterns.
internal static partial class RuleFactory
{
    // _compileCaptures: an array indexed by capture number (largest key + 1 entries), null for captures without a rule.
    // Keys are read with parseInt, so a key that is not a number is ignored; RawCaptures enumerates integer keys first.
    private static List<CaptureRule?> CompileCaptures(RawCaptures? captures, IRuleFactoryHelper helper, RawRepository repository)
    {
        var r = new List<CaptureRule?>();

        if (captures is not null)
        {
            // Find the maximum capture id
            int maximumCaptureId = 0;
            foreach (string captureId in captures.Keys)
            {
                if (ToCaptureIndex(captureId) is int numericCaptureId && numericCaptureId > maximumCaptureId)
                {
                    maximumCaptureId = numericCaptureId;
                }
            }

            // Initialize result
            for (int i = 0; i <= maximumCaptureId; i++)
            {
                r.Add(null);
            }

            // Fill out result
            foreach (string captureId in captures.Keys)
            {
                if (ToCaptureIndex(captureId) is not int numericCaptureId)
                {
                    continue;
                }

                RawRule capture = captures[captureId];
                RuleId retokenizeCapturedWithRuleId = new(0);
                if (capture.Patterns is not null)
                {
                    retokenizeCapturedWithRuleId = GetCompiledRuleId(capture, helper, repository);
                }

                r[numericCaptureId] = CreateCaptureRule(helper, capture.Name, capture.ContentName, retokenizeCapturedWithRuleId);
            }
        }

        return r;
    }

    // parseInt(captureId, 10) as an array index; NaN and negative numbers are not indexes.
    private static int? ToCaptureIndex(string captureId)
    {
        double number = JsNumbers.ParseInt(captureId);
        return double.IsNaN(number) || number < 0 || number > int.MaxValue - 1 ? null : (int)number;
    }

    // _compilePatterns. An include that cannot be resolved is skipped (hasMissingPatterns), as upstream.
    private static CompilePatternsResult CompilePatterns(List<RawRule>? patterns, IRuleFactoryHelper helper, RawRepository repository)
    {
        var r = new List<RuleId>();

        if (patterns is not null)
        {
            foreach (RawRule pattern in patterns)
            {
                RuleId? ruleId = null;

                if (!string.IsNullOrEmpty(pattern.Include))
                {
                    IncludeReference reference = IncludeReference.Parse(pattern.Include);

                    switch (reference)
                    {
                        case BaseReference or SelfReference:
                            if (repository.TryGetValue(pattern.Include, out RawRule? selfRule))
                            {
                                ruleId = GetCompiledRuleId(selfRule, helper, repository);
                            }

                            break;

                        case RelativeReference relative:
                            // Local include found in `repository`
                            if (repository.TryGetValue(relative.RuleName, out RawRule? localIncludedRule))
                            {
                                ruleId = GetCompiledRuleId(localIncludedRule, helper, repository);
                            }

                            break;

                        case TopLevelReference or TopLevelRepositoryReference:
                            string externalGrammarName = reference is TopLevelReference topLevel
                                ? topLevel.ScopeName
                                : ((TopLevelRepositoryReference)reference).ScopeName;
                            string? externalGrammarInclude = (reference as TopLevelRepositoryReference)?.RuleName;

                            // External include
                            RawGrammar? externalGrammar = helper.GetExternalGrammar(externalGrammarName, repository);
                            RawRepository? externalRepository = externalGrammar?.Repository;
                            if (externalRepository is not null)
                            {
                                string includedName = !string.IsNullOrEmpty(externalGrammarInclude) ? externalGrammarInclude : "$self";
                                if (externalRepository.TryGetValue(includedName, out RawRule? externalIncludedRule))
                                {
                                    ruleId = GetCompiledRuleId(externalIncludedRule, helper, externalRepository);
                                }
                            }

                            break;
                    }
                }
                else
                {
                    ruleId = GetCompiledRuleId(pattern, helper, repository);
                }

                if (ruleId is RuleId id)
                {
                    Rule? rule = helper.GetRule(id);

                    bool skipRule = rule switch
                    {
                        IncludeOnlyRule includeOnly => includeOnly.HasMissingPatterns && includeOnly.Patterns.Count == 0,
                        BeginEndRule beginEnd => beginEnd.HasMissingPatterns && beginEnd.Patterns.Count == 0,
                        BeginWhileRule beginWhile => beginWhile.HasMissingPatterns && beginWhile.Patterns.Count == 0,
                        _ => false,
                    };

                    if (skipRule)
                    {
                        // REMOVING RULE ENTIRELY DUE TO EMPTY PATTERNS THAT ARE MISSING
                        continue;
                    }

                    r.Add(id);
                }
            }
        }

        return new CompilePatternsResult(r, (patterns?.Count ?? 0) != r.Count);
    }
}

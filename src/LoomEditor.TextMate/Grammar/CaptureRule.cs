// Ported from microsoft/vscode-textmate: src/rule.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>CaptureRule</c>: the scope of a capture, optionally retokenized with a rule.</summary>
/// <remarks>Threading: single-threaded, like the grammar that owns it.</remarks>
internal sealed class CaptureRule : Rule
{
    /// <summary>Creates a capture rule.</summary>
    public CaptureRule(RuleId id, string? name, string? contentName, RuleId retokenizeCapturedWithRuleId)
        : base(id, name, contentName)
    {
        RetokenizeCapturedWithRuleId = retokenizeCapturedWithRuleId;
    }

    /// <summary><c>retokenizeCapturedWithRuleId</c>: the rule that retokenizes the capture, or <c>0</c>.</summary>
    public RuleId RetokenizeCapturedWithRuleId { get; }

    /// <inheritdoc />
    public override void Dispose()
    {
        // nothing to dispose
    }

    /// <inheritdoc />
    public override void CollectPatterns(IRuleRegistry grammar, RegExpSourceList @out) => throw new NotSupportedException("Not supported!");

    /// <inheritdoc />
    public override CompiledRule Compile(IRuleRegistryAndOnigLib grammar, string? endRegexSource) => throw new NotSupportedException("Not supported!");

    /// <inheritdoc />
    public override CompiledRule CompileAG(IRuleRegistryAndOnigLib grammar, string? endRegexSource, bool allowA, bool allowG) =>
        throw new NotSupportedException("Not supported!");
}

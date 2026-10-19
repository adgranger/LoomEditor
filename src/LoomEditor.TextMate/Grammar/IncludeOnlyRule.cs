// Ported from microsoft/vscode-textmate: src/rule.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>IncludeOnlyRule</c>: a rule that only groups patterns (no <c>match</c> or <c>begin</c>).</summary>
/// <remarks>Threading: single-threaded, like the grammar that owns it.</remarks>
internal sealed class IncludeOnlyRule : Rule
{
    private RegExpSourceList? _cachedCompiledPatterns;

    /// <summary>Creates an include-only rule.</summary>
    public IncludeOnlyRule(RuleId id, string? name, string? contentName, CompilePatternsResult patterns)
        : base(id, name, contentName)
    {
        Patterns = patterns.Patterns;
        HasMissingPatterns = patterns.HasMissingPatterns;
    }

    /// <summary><c>hasMissingPatterns</c>.</summary>
    public bool HasMissingPatterns { get; }

    /// <summary><c>patterns</c>.</summary>
    public IReadOnlyList<RuleId> Patterns { get; }

    /// <inheritdoc />
    public override void Dispose()
    {
        _cachedCompiledPatterns?.Dispose();
        _cachedCompiledPatterns = null;
    }

    /// <inheritdoc />
    public override void CollectPatterns(IRuleRegistry grammar, RegExpSourceList @out)
    {
        foreach (RuleId pattern in Patterns)
        {
            Rule rule = grammar.GetRule(pattern)!;
            rule.CollectPatterns(grammar, @out);
        }
    }

    /// <inheritdoc />
    public override CompiledRule Compile(IRuleRegistryAndOnigLib grammar, string? endRegexSource) =>
        GetCachedCompiledPatterns(grammar).Compile(grammar);

    /// <inheritdoc />
    public override CompiledRule CompileAG(IRuleRegistryAndOnigLib grammar, string? endRegexSource, bool allowA, bool allowG) =>
        GetCachedCompiledPatterns(grammar).CompileAG(grammar, allowA, allowG);

    private RegExpSourceList GetCachedCompiledPatterns(IRuleRegistryAndOnigLib grammar)
    {
        if (_cachedCompiledPatterns is null)
        {
            _cachedCompiledPatterns = new RegExpSourceList();
            CollectPatterns(grammar, _cachedCompiledPatterns);
        }

        return _cachedCompiledPatterns;
    }
}

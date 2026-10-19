// Ported from microsoft/vscode-textmate: src/rule.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>MatchRule</c>: a rule with a single <c>match</c> regex.</summary>
/// <remarks>Threading: single-threaded, like the grammar that owns it.</remarks>
internal sealed class MatchRule : Rule
{
    private readonly RegExpSource _match;
    private RegExpSourceList? _cachedCompiledPatterns;

    /// <summary>Creates a match rule.</summary>
    public MatchRule(RuleId id, string? name, string match, IReadOnlyList<CaptureRule?> captures)
        : base(id, name, null)
    {
        _match = new RegExpSource(match, Id);
        Captures = captures;
    }

    /// <summary><c>captures</c>: by capture index, <see langword="null"/> for a capture without a rule.</summary>
    public IReadOnlyList<CaptureRule?> Captures { get; }

    /// <summary><c>debugMatchRegExp</c>.</summary>
    public string DebugMatchRegExp => _match.Source;

    /// <inheritdoc />
    public override void Dispose()
    {
        _cachedCompiledPatterns?.Dispose();
        _cachedCompiledPatterns = null;
    }

    /// <inheritdoc />
    public override void CollectPatterns(IRuleRegistry grammar, RegExpSourceList @out) => @out.Push(_match);

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

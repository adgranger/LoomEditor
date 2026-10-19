// Ported from microsoft/vscode-textmate: src/rule.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Oniguruma;

namespace LoomEditor.TextMate.Grammar;

/// <summary>
/// <c>BeginWhileRule</c>: a region that starts with a <c>begin</c> match and goes on while each next line matches
/// <c>while</c>.
/// </summary>
/// <remarks>Threading: single-threaded, like the grammar that owns it.</remarks>
internal sealed class BeginWhileRule : Rule
{
    private readonly RegExpSource _begin;
    private readonly RegExpSource _while;
    private RegExpSourceList? _cachedCompiledPatterns;
    private RegExpSourceList? _cachedCompiledWhilePatterns;

    /// <summary>Creates a begin/while rule.</summary>
    public BeginWhileRule(RuleId id, string? name, string? contentName, string begin, IReadOnlyList<CaptureRule?> beginCaptures,
        string @while, IReadOnlyList<CaptureRule?> whileCaptures, CompilePatternsResult patterns)
        : base(id, name, contentName)
    {
        _begin = new RegExpSource(begin, Id);
        BeginCaptures = beginCaptures;
        WhileCaptures = whileCaptures;
        _while = new RegExpSource(@while, RuleId.While);
        WhileHasBackReferences = _while.HasBackReferences;
        Patterns = patterns.Patterns;
        HasMissingPatterns = patterns.HasMissingPatterns;
    }

    /// <summary><c>beginCaptures</c>.</summary>
    public IReadOnlyList<CaptureRule?> BeginCaptures { get; }

    /// <summary><c>whileCaptures</c>.</summary>
    public IReadOnlyList<CaptureRule?> WhileCaptures { get; }

    /// <summary><c>whileHasBackReferences</c>.</summary>
    public bool WhileHasBackReferences { get; }

    /// <summary><c>hasMissingPatterns</c>.</summary>
    public bool HasMissingPatterns { get; }

    /// <summary><c>patterns</c>.</summary>
    public IReadOnlyList<RuleId> Patterns { get; }

    /// <summary><c>debugBeginRegExp</c>.</summary>
    public string DebugBeginRegExp => _begin.Source;

    /// <summary><c>debugWhileRegExp</c>.</summary>
    public string DebugWhileRegExp => _while.Source;

    /// <inheritdoc />
    public override void Dispose()
    {
        _cachedCompiledPatterns?.Dispose();
        _cachedCompiledPatterns = null;
        _cachedCompiledWhilePatterns?.Dispose();
        _cachedCompiledWhilePatterns = null;
    }

    /// <summary><c>getWhileWithResolvedBackReferences</c>.</summary>
    public string GetWhileWithResolvedBackReferences(string lineText, IReadOnlyList<OnigCaptureIndex> captureIndices) =>
        _while.ResolveBackReferences(lineText, captureIndices);

    /// <inheritdoc />
    public override void CollectPatterns(IRuleRegistry grammar, RegExpSourceList @out) => @out.Push(_begin);

    /// <inheritdoc />
    public override CompiledRule Compile(IRuleRegistryAndOnigLib grammar, string? endRegexSource) =>
        GetCachedCompiledPatterns(grammar).Compile(grammar);

    /// <inheritdoc />
    public override CompiledRule CompileAG(IRuleRegistryAndOnigLib grammar, string? endRegexSource, bool allowA, bool allowG) =>
        GetCachedCompiledPatterns(grammar).CompileAG(grammar, allowA, allowG);

    /// <summary><c>compileWhile</c>: the scanner of the while regex (rule id <see cref="RuleId.While"/>).</summary>
    public CompiledRule CompileWhile(IRuleRegistryAndOnigLib grammar, string? endRegexSource) =>
        GetCachedCompiledWhilePatterns(endRegexSource).Compile(grammar);

    /// <summary><c>compileWhileAG</c>.</summary>
    public CompiledRule CompileWhileAG(IRuleRegistryAndOnigLib grammar, string? endRegexSource, bool allowA, bool allowG) =>
        GetCachedCompiledWhilePatterns(endRegexSource).CompileAG(grammar, allowA, allowG);

    private RegExpSourceList GetCachedCompiledPatterns(IRuleRegistryAndOnigLib grammar)
    {
        if (_cachedCompiledPatterns is null)
        {
            _cachedCompiledPatterns = new RegExpSourceList();

            foreach (RuleId pattern in Patterns)
            {
                Rule rule = grammar.GetRule(pattern)!;
                rule.CollectPatterns(grammar, _cachedCompiledPatterns);
            }
        }

        return _cachedCompiledPatterns;
    }

    private RegExpSourceList GetCachedCompiledWhilePatterns(string? endRegexSource)
    {
        if (_cachedCompiledWhilePatterns is null)
        {
            _cachedCompiledWhilePatterns = new RegExpSourceList();
            _cachedCompiledWhilePatterns.Push(_while.HasBackReferences ? _while.Clone() : _while);
        }

        if (_while.HasBackReferences)
        {
            _cachedCompiledWhilePatterns.SetSource(0, string.IsNullOrEmpty(endRegexSource) ? char.MaxValue.ToString() : endRegexSource);
        }

        return _cachedCompiledWhilePatterns;
    }
}

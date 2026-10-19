// Ported from microsoft/vscode-textmate: src/rule.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Oniguruma;

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>BeginEndRule</c>: a region from a <c>begin</c> match to an <c>end</c> match, with patterns inside.</summary>
/// <remarks>Threading: single-threaded, like the grammar that owns it.</remarks>
internal sealed class BeginEndRule : Rule
{
    private readonly RegExpSource _begin;
    private readonly RegExpSource _end;
    private RegExpSourceList? _cachedCompiledPatterns;

    /// <summary>Creates a begin/end rule; without an <c>end</c>, the end regex is U+FFFF (it never matches text).</summary>
    public BeginEndRule(RuleId id, string? name, string? contentName, string begin, IReadOnlyList<CaptureRule?> beginCaptures,
        string? end, IReadOnlyList<CaptureRule?> endCaptures, bool applyEndPatternLast, CompilePatternsResult patterns)
        : base(id, name, contentName)
    {
        _begin = new RegExpSource(begin, Id);
        BeginCaptures = beginCaptures;
        _end = new RegExpSource(string.IsNullOrEmpty(end) ? char.MaxValue.ToString() : end, RuleId.End);
        EndHasBackReferences = _end.HasBackReferences;
        EndCaptures = endCaptures;
        ApplyEndPatternLast = applyEndPatternLast;
        Patterns = patterns.Patterns;
        HasMissingPatterns = patterns.HasMissingPatterns;
    }

    /// <summary><c>beginCaptures</c>.</summary>
    public IReadOnlyList<CaptureRule?> BeginCaptures { get; }

    /// <summary><c>endHasBackReferences</c>.</summary>
    public bool EndHasBackReferences { get; }

    /// <summary><c>endCaptures</c>.</summary>
    public IReadOnlyList<CaptureRule?> EndCaptures { get; }

    /// <summary><c>applyEndPatternLast</c>: the end regex is tried after the patterns instead of before.</summary>
    public bool ApplyEndPatternLast { get; }

    /// <summary><c>hasMissingPatterns</c>.</summary>
    public bool HasMissingPatterns { get; }

    /// <summary><c>patterns</c>.</summary>
    public IReadOnlyList<RuleId> Patterns { get; }

    /// <summary><c>debugBeginRegExp</c>.</summary>
    public string DebugBeginRegExp => _begin.Source;

    /// <summary><c>debugEndRegExp</c>.</summary>
    public string DebugEndRegExp => _end.Source;

    /// <inheritdoc />
    public override void Dispose()
    {
        _cachedCompiledPatterns?.Dispose();
        _cachedCompiledPatterns = null;
    }

    /// <summary><c>getEndWithResolvedBackReferences</c>.</summary>
    public string GetEndWithResolvedBackReferences(string lineText, IReadOnlyList<OnigCaptureIndex> captureIndices) =>
        _end.ResolveBackReferences(lineText, captureIndices);

    /// <inheritdoc />
    public override void CollectPatterns(IRuleRegistry grammar, RegExpSourceList @out) => @out.Push(_begin);

    /// <inheritdoc />
    public override CompiledRule Compile(IRuleRegistryAndOnigLib grammar, string? endRegexSource) =>
        GetCachedCompiledPatterns(grammar, endRegexSource).Compile(grammar);

    /// <inheritdoc />
    public override CompiledRule CompileAG(IRuleRegistryAndOnigLib grammar, string? endRegexSource, bool allowA, bool allowG) =>
        GetCachedCompiledPatterns(grammar, endRegexSource).CompileAG(grammar, allowA, allowG);

    private RegExpSourceList GetCachedCompiledPatterns(IRuleRegistryAndOnigLib grammar, string? endRegexSource)
    {
        if (_cachedCompiledPatterns is null)
        {
            _cachedCompiledPatterns = new RegExpSourceList();

            foreach (RuleId pattern in Patterns)
            {
                Rule rule = grammar.GetRule(pattern)!;
                rule.CollectPatterns(grammar, _cachedCompiledPatterns);
            }

            if (ApplyEndPatternLast)
            {
                _cachedCompiledPatterns.Push(_end.HasBackReferences ? _end.Clone() : _end);
            }
            else
            {
                _cachedCompiledPatterns.Unshift(_end.HasBackReferences ? _end.Clone() : _end);
            }
        }

        if (_end.HasBackReferences)
        {
            // The caller passes the end regex resolved with the begin captures (upstream fails later on null).
            ArgumentNullException.ThrowIfNull(endRegexSource);
            if (ApplyEndPatternLast)
            {
                _cachedCompiledPatterns.SetSource(_cachedCompiledPatterns.Length - 1, endRegexSource);
            }
            else
            {
                _cachedCompiledPatterns.SetSource(0, endRegexSource);
            }
        }

        return _cachedCompiledPatterns;
    }
}

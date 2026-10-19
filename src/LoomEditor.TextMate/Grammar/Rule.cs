// Ported from microsoft/vscode-textmate: src/rule.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Oniguruma;

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>ICompilePatternsResult</c>.</summary>
/// <param name="Patterns"><c>patterns</c>: the ids of the resolved patterns.</param>
/// <param name="HasMissingPatterns"><c>hasMissingPatterns</c>: whether some patterns could not be resolved.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record CompilePatternsResult(IReadOnlyList<RuleId> Patterns, bool HasMissingPatterns);

/// <summary>
/// <c>Rule</c>: a compiled rule of a grammar, with its scope name and content name (which may refer to captures).
/// The location metadata (<c>$location</c>) is not ported.
/// </summary>
/// <remarks>Threading: single-threaded, like the grammar that owns it; <see cref="Dispose"/> frees the compiled regexes.</remarks>
internal abstract class Rule : IDisposable
{
    private readonly bool _nameIsCapturing;
    private readonly string? _name;
    private readonly bool _contentNameIsCapturing;
    private readonly string? _contentName;

    /// <summary>Creates a rule; an empty name or content name is <see langword="null"/>.</summary>
    protected Rule(RuleId id, string? name, string? contentName)
    {
        Id = id;
        _name = string.IsNullOrEmpty(name) ? null : name;
        _nameIsCapturing = RegexSource.HasCaptures(_name);
        _contentName = string.IsNullOrEmpty(contentName) ? null : contentName;
        _contentNameIsCapturing = RegexSource.HasCaptures(_contentName);
    }

    /// <summary><c>id</c>.</summary>
    public RuleId Id { get; }

    /// <summary><c>debugName</c>: the class name and id (no location: <c>unknown</c>).</summary>
    public string DebugName => $"{GetType().Name}#{Id} @ unknown";

    /// <summary><c>dispose</c>.</summary>
    public abstract void Dispose();

    /// <summary><c>getName</c>: the scope name, with capture references replaced when the line is given.</summary>
    public string? GetName(string? lineText, IReadOnlyList<OnigCaptureIndex>? captureIndices)
    {
        if (!_nameIsCapturing || _name is null || lineText is null || captureIndices is null)
        {
            return _name;
        }

        return RegexSource.ReplaceCaptures(_name, lineText, captureIndices);
    }

    /// <summary><c>getContentName</c>: the content name, with capture references replaced.</summary>
    public string? GetContentName(string lineText, IReadOnlyList<OnigCaptureIndex> captureIndices)
    {
        if (!_contentNameIsCapturing || _contentName is null)
        {
            return _contentName;
        }

        return RegexSource.ReplaceCaptures(_contentName, lineText, captureIndices);
    }

    /// <summary><c>collectPatterns</c>: adds the regexes that start this rule to <paramref name="out"/>.</summary>
    public abstract void CollectPatterns(IRuleRegistry grammar, RegExpSourceList @out);

    /// <summary><c>compile</c>: the scanner for the inside of this rule.</summary>
    public abstract CompiledRule Compile(IRuleRegistryAndOnigLib grammar, string? endRegexSource);

    /// <summary><c>compileAG</c>: <see cref="Compile"/> with <c>\A</c> / <c>\G</c> allowed or not.</summary>
    public abstract CompiledRule CompileAG(IRuleRegistryAndOnigLib grammar, string? endRegexSource, bool allowA, bool allowG);
}

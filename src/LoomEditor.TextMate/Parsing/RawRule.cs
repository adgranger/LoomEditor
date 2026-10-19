// Ported from microsoft/vscode-textmate: src/rawGrammar.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Parsing;

/// <summary><c>IRawRule</c>: one rule of a raw TextMate grammar, as read from the grammar file.</summary>
/// <remarks>
/// <para>
/// Mutable on purpose: vscode-textmate stores the compiled rule id in the raw rule (<see cref="Id"/>). The location
/// metadata of upstream (<c>ILocatable</c>, <c>$vscodeTextmateLocation</c>) is not ported.
/// </para>
/// <para>
/// Threading: read-only after loading, so a loaded grammar may be shared between threads; a grammar that compiles rules
/// works on its own <see cref="DeepClone"/> (phase-4/README.md, trap 7).
/// </para>
/// </remarks>
internal sealed class RawRule
{
    /// <summary><c>id</c>: the compiled rule id, set by the rule factory ("not part of the spec, only used internally").</summary>
    public int? Id { get; set; }

    /// <summary><c>include</c>: a scope name, <c>scope#rule</c>, <c>#rule</c>, <c>$self</c> or <c>$base</c>.</summary>
    public string? Include { get; set; }

    /// <summary><c>name</c>: the scope name of the rule.</summary>
    public string? Name { get; set; }

    /// <summary><c>contentName</c>: the scope name of the content between begin and end.</summary>
    public string? ContentName { get; set; }

    /// <summary><c>match</c>.</summary>
    public string? Match { get; set; }

    /// <summary><c>captures</c>.</summary>
    public RawCaptures? Captures { get; set; }

    /// <summary><c>begin</c>.</summary>
    public string? Begin { get; set; }

    /// <summary><c>beginCaptures</c>.</summary>
    public RawCaptures? BeginCaptures { get; set; }

    /// <summary><c>end</c>.</summary>
    public string? End { get; set; }

    /// <summary><c>endCaptures</c>.</summary>
    public RawCaptures? EndCaptures { get; set; }

    /// <summary><c>while</c>.</summary>
    public string? While { get; set; }

    /// <summary><c>whileCaptures</c>.</summary>
    public RawCaptures? WhileCaptures { get; set; }

    /// <summary><c>patterns</c>.</summary>
    public List<RawRule>? Patterns { get; set; }

    /// <summary><c>repository</c>.</summary>
    public RawRepository? Repository { get; set; }

    /// <summary><c>applyEndPatternLast</c> (a JSON <c>true</c> or a number other than 0).</summary>
    public bool ApplyEndPatternLast { get; set; }

    /// <summary><c>clone</c>: a deep copy, including <see cref="Id"/>.</summary>
    public RawRule DeepClone() => new()
    {
        Id = Id,
        Include = Include,
        Name = Name,
        ContentName = ContentName,
        Match = Match,
        Captures = Captures?.DeepClone(),
        Begin = Begin,
        BeginCaptures = BeginCaptures?.DeepClone(),
        End = End,
        EndCaptures = EndCaptures?.DeepClone(),
        While = While,
        WhileCaptures = WhileCaptures?.DeepClone(),
        Patterns = Patterns?.ConvertAll(rule => rule.DeepClone()),
        Repository = Repository?.DeepClone(),
        ApplyEndPatternLast = ApplyEndPatternLast,
    };
}

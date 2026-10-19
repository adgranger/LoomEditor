// Ported from microsoft/vscode-textmate: src/rawGrammar.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Parsing;

/// <summary><c>IRawGrammar</c>: a TextMate grammar as read from its file.</summary>
/// <remarks>
/// <para>
/// Mutable on purpose: <c>initGrammar</c> sets the repository and adds <c>$self</c> / <c>$base</c> to it, and rules
/// get their compiled id. <c>Grammar</c> works on a <see cref="DeepClone"/> (phase-4/README.md, trap 7).
/// </para>
/// <para>Threading: read-only after loading; a loaded grammar may be shared between threads.</para>
/// </remarks>
internal sealed class RawGrammar
{
    /// <summary><c>repository</c>.</summary>
    public RawRepository? Repository { get; set; }

    /// <summary><c>scopeName</c>, for example <c>source.js</c>.</summary>
    public string? ScopeName { get; set; }

    /// <summary><c>patterns</c>.</summary>
    public List<RawRule>? Patterns { get; set; }

    /// <summary><c>injections</c>: rules by injection selector, in document order.</summary>
    public RawRepository? Injections { get; set; }

    /// <summary><c>injectionSelector</c>.</summary>
    public string? InjectionSelector { get; set; }

    /// <summary><c>fileTypes</c>.</summary>
    public List<string>? FileTypes { get; set; }

    /// <summary><c>name</c>.</summary>
    public string? Name { get; set; }

    /// <summary><c>firstLineMatch</c>.</summary>
    public string? FirstLineMatch { get; set; }

    /// <summary><c>clone</c>: a deep copy, including the rule ids.</summary>
    public RawGrammar DeepClone() => new()
    {
        Repository = Repository?.DeepClone(),
        ScopeName = ScopeName,
        Patterns = Patterns?.ConvertAll(rule => rule.DeepClone()),
        Injections = Injections?.DeepClone(),
        InjectionSelector = InjectionSelector,
        FileTypes = FileTypes is null ? null : [.. FileTypes],
        Name = Name,
        FirstLineMatch = FirstLineMatch,
    };
}

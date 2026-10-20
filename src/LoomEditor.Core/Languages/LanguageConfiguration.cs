// Ported from microsoft/vscode: src/vs/editor/common/languages/languageConfiguration.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;

namespace LoomEditor.Core.Languages;

/// <summary>
/// The language configuration interface defines the contract between extensions and various editor features, like
/// automatic bracket insertion, automatic indentation etc. <see langword="null"/> is Monaco's <c>undefined</c>.
/// Monaco's <c>ExplicitLanguageConfiguration</c> is the same type.
/// </summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed record LanguageConfiguration
{
    /// <summary>The language's comment settings.</summary>
    public CommentRule? Comments { get; init; }

    /// <summary>The language's brackets. This configuration implicitly affects pressing Enter around these brackets.</summary>
    public IReadOnlyList<CharacterPair>? Brackets { get; init; }

    /// <summary>The language's word definition.</summary>
    public Regex? WordPattern { get; init; }

    /// <summary>The language's indentation settings.</summary>
    public IndentationRule? IndentationRules { get; init; }

    /// <summary>The language's rules to be evaluated when pressing Enter.</summary>
    public IReadOnlyList<OnEnterRule>? OnEnterRules { get; init; }

    /// <summary>The language's auto closing pairs.</summary>
    public IReadOnlyList<AutoClosingPairConditional>? AutoClosingPairs { get; init; }

    /// <summary>The language's surrounding pairs (the auto closing pairs when not set).</summary>
    public IReadOnlyList<AutoClosingPair>? SurroundingPairs { get; init; }

    /// <summary>The pairs colorized by bracket pair colorization (the brackets when not set).</summary>
    public IReadOnlyList<CharacterPair>? ColorizedBracketPairs { get; init; }

    /// <summary>The characters before which a pair is auto closed (a default set when not set).</summary>
    public string? AutoCloseBefore { get; init; }

    /// <summary>The language's folding rules.</summary>
    public FoldingRules? Folding { get; init; }

    /// <summary>Monaco's <c>__electricCharacterSupport.docComment</c>.</summary>
    public DocComment? DocComment { get; init; }
}

/// <summary>A pair of brackets or delimiters (Monaco's tuple <c>CharacterPair = [string, string]</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct CharacterPair(string Open, string Close);

/// <summary>The line comment token, and whether it is inserted without indentation (Monaco: <c>LineCommentConfig</c>).</summary>
/// <remarks>Monaco's <c>lineComment: string</c> is <c>new LineCommentConfig(text)</c>. Threading: immutable.</remarks>
internal sealed record LineCommentConfig(string Comment, bool? NoIndent = null);

/// <summary>Describes how comments for a language work (Monaco: <c>CommentRule</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed record CommentRule(LineCommentConfig? LineComment = null, CharacterPair? BlockComment = null);

/// <summary>Describes indentation rules for a language (Monaco: <c>IndentationRule</c>).</summary>
/// <param name="DecreaseIndentPattern">If a line matches this pattern, then all the lines after it should be unindented once (until another rule matches).</param>
/// <param name="IncreaseIndentPattern">If a line matches this pattern, then all the lines after it should be indented once (until another rule matches).</param>
/// <param name="IndentNextLinePattern">If a line matches this pattern, then only the next line after it should be indented once.</param>
/// <param name="UnIndentedLinePattern">If a line matches this pattern, then its indentation should not be changed and it should not be evaluated against the other rules.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record IndentationRule(Regex DecreaseIndentPattern, Regex IncreaseIndentPattern, Regex? IndentNextLinePattern = null, Regex? UnIndentedLinePattern = null);

/// <summary>Region markers of a language (Monaco: <c>FoldingMarkers</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed record FoldingMarkers(Regex Start, Regex End);

/// <summary>Describes folding rules for a language (Monaco: <c>FoldingRules</c>).</summary>
/// <param name="OffSide">Whether blocks are defined by their indentation (off-side rule) rather than by brackets.</param>
/// <param name="Markers">Region markers used by the language.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record FoldingRules(bool OffSide = false, FoldingMarkers? Markers = null);

/// <summary>Describes a rule to be evaluated when pressing Enter (Monaco: <c>OnEnterRule</c>).</summary>
/// <param name="BeforeText">This rule will only execute if the text before the cursor matches this regular expression.</param>
/// <param name="Action">What to do.</param>
/// <param name="AfterText">This rule will only execute if the text after the cursor matches this regular expression.</param>
/// <param name="PreviousLineText">This rule will only execute if the text above the current line matches this regular expression.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record OnEnterRule(Regex BeforeText, EnterAction Action, Regex? AfterText = null, Regex? PreviousLineText = null);

/// <summary>Definition of documentation comments, e.g. Javadoc/JSdoc (Monaco: <c>IDocComment</c>).</summary>
/// <param name="Open">The string that starts a doc comment (e.g. <c>/**</c>).</param>
/// <param name="Close">The string that appears on the last line and closes the doc comment (e.g. <c> */</c>).</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record DocComment(string Open, string? Close = null);

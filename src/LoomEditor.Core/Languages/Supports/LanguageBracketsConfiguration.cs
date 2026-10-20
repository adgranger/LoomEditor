// Ported from microsoft/vscode: src/vs/editor/common/languages/supports/languageBracketsConfiguration.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;

namespace LoomEditor.Core.Languages.Supports;

/// <summary>
/// Captures all bracket related configurations for a single language (Monaco: <c>LanguageBracketsConfiguration</c>).
/// </summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed class LanguageBracketsConfiguration
{
    private readonly OrderedDictionary<string, OpeningBracketKind> _openingBrackets = [];
    private readonly OrderedDictionary<string, ClosingBracketKind> _closingBrackets = [];

    public LanguageBracketsConfiguration(string languageId, LanguageConfiguration config)
    {
        LanguageId = languageId;

        List<CharacterPair> bracketPairs = config.Brackets is not null ? FilterValidBrackets(config.Brackets) : [];

        foreach ((string open, string close) in bracketPairs)
        {
            OpeningBracketKind opening = GetOrAddOpening(open);
            ClosingBracketKind closing = GetOrAddClosing(close);

            opening.AddOpenedBracket(closing);
            closing.AddOpeningBracket(opening);
        }

        // Treat colorized brackets as brackets, and mark them as colorized.
        List<CharacterPair> colorizedBracketPairs = config.ColorizedBracketPairs is not null
            ? FilterValidBrackets(config.ColorizedBracketPairs)
            // If not configured: Take all brackets except `<` ... `>`
            // Many languages set < ... > as bracket pair, even though they also use it as comparison operator.
            // This leads to problems when colorizing this bracket, so we exclude it if not explicitly configured otherwise.
            // https://github.com/microsoft/vscode/issues/132476
            : [.. bracketPairs.Where(p => !(p.Open == "<" && p.Close == ">"))];
        foreach ((string open, string close) in colorizedBracketPairs)
        {
            OpeningBracketKind opening = GetOrAddOpening(open);
            ClosingBracketKind closing = GetOrAddClosing(close);

            opening.AddOpenedBracket(closing);
            closing.AddOpeningColorizedBracket(opening);
            closing.AddOpeningBracket(opening);
        }
    }

    public string LanguageId { get; }

    /// <summary>No two brackets have the same bracket text.</summary>
    public IReadOnlyList<OpeningBracketKind> OpeningBrackets => [.. _openingBrackets.Values];

    /// <summary>No two brackets have the same bracket text.</summary>
    public IReadOnlyList<ClosingBracketKind> ClosingBrackets => [.. _closingBrackets.Values];

    public OpeningBracketKind? GetOpeningBracketInfo(string bracketText) => _openingBrackets.GetValueOrDefault(bracketText);

    public ClosingBracketKind? GetClosingBracketInfo(string bracketText) => _closingBrackets.GetValueOrDefault(bracketText);

    public BracketKind? GetBracketInfo(string bracketText) =>
        (BracketKind?)GetOpeningBracketInfo(bracketText) ?? GetClosingBracketInfo(bracketText);

    public Regex GetBracketRegExp() => BracketsUtils.CreateBracketOrRegExp([.. _openingBrackets.Keys, .. _closingBrackets.Keys]);

    private static List<CharacterPair> FilterValidBrackets(IReadOnlyList<CharacterPair> bracketPairs) =>
        [.. bracketPairs.Where(p => p.Open != "" && p.Close != "")];

    // Monaco's CachedFunction: one kind per bracket text, created on first use.
    private OpeningBracketKind GetOrAddOpening(string bracket)
    {
        if (!_openingBrackets.TryGetValue(bracket, out OpeningBracketKind? kind))
        {
            kind = new OpeningBracketKind(this, bracket);
            _openingBrackets.Add(bracket, kind);
        }
        return kind;
    }

    private ClosingBracketKind GetOrAddClosing(string bracket)
    {
        if (!_closingBrackets.TryGetValue(bracket, out ClosingBracketKind? kind))
        {
            kind = new ClosingBracketKind(this, bracket);
            _closingBrackets.Add(bracket, kind);
        }
        return kind;
    }
}

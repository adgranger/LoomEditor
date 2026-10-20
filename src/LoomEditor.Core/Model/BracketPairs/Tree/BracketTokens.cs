// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/brackets.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>The bracket tokens of one language and the regex that finds any of them (Monaco: <c>BracketTokens</c>).</summary>
/// <remarks>
/// <para>
/// Behavior difference: the regex is a .NET regex; <c>\b</c> is replaced by an explicit ASCII word boundary because
/// JavaScript's <c>\w</c> and <c>\b</c> are ASCII only (see <c>docs/PORTING.md</c>).
/// </para>
/// <para>Threading: single-threaded (UI thread), like the tree that owns it.</para>
/// </remarks>
internal sealed class BracketTokens
{
    // JavaScript's \b with an ASCII \w: a boundary between [A-Za-z0-9_] and anything else. .NET's \b is Unicode.
    private const string AsciiWordBoundary = @"(?:(?<=[A-Za-z0-9_])(?![A-Za-z0-9_])|(?<![A-Za-z0-9_])(?=[A-Za-z0-9_]))";

    private readonly OrderedDictionary<string, Token> _map;
    private bool _hasRegExp;
    private Regex? _regExpGlobal;

    /// <summary>Creates the tokens from a map that keeps the insertion order of the brackets.</summary>
    public BracketTokens(OrderedDictionary<string, Token> map)
    {
        ArgumentNullException.ThrowIfNull(map);

        _map = map;
    }

    /// <summary>True when the language has no brackets.</summary>
    public bool IsEmpty => _map.Count == 0;

    /// <summary>
    /// A regex that finds any bracket in a line (Monaco: <c>regExpGlobal</c>, flags <c>gi</c>), or
    /// <see langword="null"/> if there is no such regex (because there are no brackets).
    /// </summary>
    public Regex? RegExpGlobal
    {
        get
        {
            if (!_hasRegExp)
            {
                var regExpStr = GetRegExpStr();
                _regExpGlobal = regExpStr is not null
                    ? new Regex(regExpStr, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                    : null;
                _hasRegExp = true;
            }

            return _regExpGlobal;
        }
    }

    /// <summary>Creates the tokens of a language from its bracket configuration.</summary>
    public static BracketTokens CreateFromLanguage(ResolvedLanguageConfiguration configuration, DenseKeyProvider<string> denseKeyProvider)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(denseKeyProvider);

        int GetId(BracketKind bracketInfo)
        {
            return denseKeyProvider.GetKey($"{bracketInfo.LanguageId}:::{bracketInfo.BracketText}");
        }

        var map = new OrderedDictionary<string, Token>();
        foreach (var openingBracket in configuration.BracketsNew.OpeningBrackets)
        {
            var length = Length.Of(0, openingBracket.BracketText.Length);
            var openingTextId = GetId(openingBracket);
            var bracketIds = SmallImmutableSet<int>.Empty.Add(openingTextId, IdentityKeyProvider.Instance);

            // Like a JavaScript Map, setting an existing key keeps its position.
            map[openingBracket.BracketText] = new Token(
                length,
                TokenKind.OpeningBracket,
                openingTextId,
                bracketIds,
                BracketAstNode.Create(length, openingBracket, bracketIds));
        }

        foreach (var closingBracket in configuration.BracketsNew.ClosingBrackets)
        {
            var length = Length.Of(0, closingBracket.BracketText.Length);

            var bracketIds = SmallImmutableSet<int>.Empty;
            var closingBrackets = closingBracket.GetOpeningBrackets();
            foreach (var bracket in closingBrackets)
            {
                bracketIds = bracketIds.Add(GetId(bracket), IdentityKeyProvider.Instance);
            }

            map[closingBracket.BracketText] = new Token(
                length,
                TokenKind.ClosingBracket,
                GetId(closingBrackets[0]),
                bracketIds,
                BracketAstNode.Create(length, closingBracket, bracketIds));
        }

        return new BracketTokens(map);
    }

    /// <summary>The regex source of <see cref="RegExpGlobal"/>, or <see langword="null"/> without brackets.</summary>
    public string? GetRegExpStr()
    {
        if (IsEmpty)
        {
            return null;
        }

        // JavaScript's default sort compares UTF-16 code units, like string.CompareOrdinal.
        var keys = new List<string>(_map.Keys);
        keys.Sort(string.CompareOrdinal);
        keys.Reverse();
        return string.Join('|', keys.Select(PrepareBracketForRegExp));
    }

    /// <summary>Returns the token of a bracket text (compared in lower case, as Monaco does), or <see langword="null"/>.</summary>
    public Token? GetToken(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return _map.GetValueOrDefault(value.ToLowerInvariant());
    }

    /// <summary>
    /// Returns the first closing bracket text (in insertion order) that closes one of the opening brackets, or
    /// <see langword="null"/>.
    /// </summary>
    public string? FindClosingTokenText(SmallImmutableSet<int> openingBracketIds)
    {
        ArgumentNullException.ThrowIfNull(openingBracketIds);

        foreach (var (closingText, info) in _map)
        {
            if (info.Kind == TokenKind.ClosingBracket && info.BracketIds.Intersects(openingBracketIds))
            {
                return closingText;
            }
        }

        return null;
    }

    private static string PrepareBracketForRegExp(string str)
    {
        var escaped = Strings.EscapeRegExpCharacters(str);

        // These bracket pair delimiters start or end with letters
        // see https://github.com/microsoft/vscode/issues/132162 https://github.com/microsoft/vscode/issues/150440
        // Monaco: /^[\w ]+/ and /[\w ]+$/ (ASCII \w), and \b (ASCII too): see AsciiWordBoundary.
        if (IsAsciiWordOrSpace(str[0]))
        {
            escaped = AsciiWordBoundary + escaped;
        }

        if (IsAsciiWordOrSpace(str[^1]))
        {
            escaped += AsciiWordBoundary;
        }

        return escaped;
    }

    private static bool IsAsciiWordOrSpace(char c)
    {
        return c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_' or ' ';
    }
}

/// <summary>Caches the <see cref="BracketTokens"/> of every language (Monaco: <c>LanguageAgnosticBracketTokens</c>).</summary>
/// <remarks>Threading: single-threaded (UI thread), like the tree that owns it.</remarks>
internal sealed class LanguageAgnosticBracketTokens
{
    private readonly Dictionary<string, BracketTokens> _languageIdToBracketTokens = new(StringComparer.Ordinal);
    private readonly DenseKeyProvider<string> _denseKeyProvider;
    private readonly Func<string, ResolvedLanguageConfiguration> _getLanguageConfiguration;

    /// <summary>Creates the cache.</summary>
    public LanguageAgnosticBracketTokens(
        DenseKeyProvider<string> denseKeyProvider,
        Func<string, ResolvedLanguageConfiguration> getLanguageConfiguration)
    {
        ArgumentNullException.ThrowIfNull(denseKeyProvider);
        ArgumentNullException.ThrowIfNull(getLanguageConfiguration);

        _denseKeyProvider = denseKeyProvider;
        _getLanguageConfiguration = getLanguageConfiguration;
    }

    /// <summary>
    /// Reports a change whenever the language configuration of a used language updates: true when the language
    /// is cached.
    /// </summary>
    public bool DidLanguageChange(string languageId)
    {
        return _languageIdToBracketTokens.ContainsKey(languageId);
    }

    /// <summary>Returns the bracket tokens of a language, creating them on first use.</summary>
    public BracketTokens GetSingleLanguageBracketTokens(string languageId)
    {
        if (!_languageIdToBracketTokens.TryGetValue(languageId, out var singleLanguageBracketTokens))
        {
            singleLanguageBracketTokens = BracketTokens.CreateFromLanguage(_getLanguageConfiguration(languageId), _denseKeyProvider);
            _languageIdToBracketTokens[languageId] = singleLanguageBracketTokens;
        }

        return singleLanguageBracketTokens;
    }

    /// <summary>Returns the token of a bracket text in a language, or <see langword="null"/>.</summary>
    public Token? GetToken(string value, string languageId)
    {
        var singleLanguageBracketTokens = GetSingleLanguageBracketTokens(languageId);
        return singleLanguageBracketTokens.GetToken(value);
    }
}

// Ported from microsoft/vscode-textmate: src/registry.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Oniguruma;
using LoomEditor.TextMate.Parsing;
using LoomEditor.TextMate.Theme;
using TextMateTheme = LoomEditor.TextMate.Theme.Theme;

namespace LoomEditor.TextMate.Grammar;

/// <summary>
/// <c>SyncRegistry</c>: the raw grammars, injections and theme that grammars are created from; it creates and owns the
/// <see cref="Grammar"/> of each scope name.
/// </summary>
/// <remarks>
/// Deliberate difference: upstream awaits the Oniguruma library (<c>Promise&lt;IOnigLib&gt;</c>); here it is given
/// (ADR 0008, decision 3).
/// Threading: single-threaded, like the grammars it owns (ADR 0008, decision 5); <see cref="Dispose"/> disposes them.
/// </remarks>
internal sealed class SyncRegistry : IGrammarRepositoryAndThemeProvider, IDisposable
{
    private readonly Dictionary<string, Grammar> _grammars = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RawGrammar> _rawGrammars = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyList<string>> _injectionGrammars = new(StringComparer.Ordinal);
    private readonly IOnigLib _onigLib;
    private TextMateTheme _theme;

    /// <summary>Creates a registry with a theme.</summary>
    public SyncRegistry(TextMateTheme theme, IOnigLib onigLib)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(onigLib);
        _theme = theme;
        _onigLib = onigLib;
    }

    /// <summary><c>dispose</c>: disposes the grammars.</summary>
    public void Dispose()
    {
        foreach (Grammar grammar in _grammars.Values)
        {
            grammar.Dispose();
        }
    }

    /// <summary><c>setTheme</c>.</summary>
    public void SetTheme(TextMateTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        _theme = theme;
    }

    /// <summary><c>getColorMap</c>.</summary>
    public string?[] GetColorMap() => _theme.GetColorMap();

    /// <summary><c>addGrammar</c>: adds <paramref name="grammar"/> to the registry.</summary>
    /// <param name="grammar">The raw grammar.</param>
    /// <param name="injectionScopeNames">The scope names of the grammars injected into it, if any.</param>
    public void AddGrammar(RawGrammar grammar, IReadOnlyList<string>? injectionScopeNames = null)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        _rawGrammars[grammar.ScopeName!] = grammar;

        if (injectionScopeNames is not null)
        {
            _injectionGrammars[grammar.ScopeName!] = injectionScopeNames;
        }
    }

    /// <summary><c>lookup</c>: a raw grammar.</summary>
    public RawGrammar? Lookup(string scopeName) => _rawGrammars.GetValueOrDefault(scopeName);

    /// <summary><c>injections</c>: the injections for the given grammar (empty when none; upstream returns <c>undefined</c>).</summary>
    public IReadOnlyList<string> Injections(string targetScope) => _injectionGrammars.GetValueOrDefault(targetScope) ?? [];

    /// <summary><c>getDefaults</c>: the default theme settings.</summary>
    public StyleAttributes GetDefaults() => _theme.GetDefaults();

    /// <summary><c>themeMatch</c>: matches a scope in the theme.</summary>
    public StyleAttributes? ThemeMatch(ScopeStack scopePath) => _theme.Match(scopePath);

    /// <summary>
    /// <c>grammarForScopeName</c>: the grammar of <paramref name="scopeName"/>, created on the first call (later calls
    /// return it whatever the other arguments are); <see langword="null"/> when no raw grammar was added for it.
    /// </summary>
    public IGrammar? GrammarForScopeName(
        string scopeName,
        int initialLanguage,
        IReadOnlyDictionary<string, int>? embeddedLanguages,
        IReadOnlyDictionary<string, StandardTokenType>? tokenTypes,
        BalancedBracketSelectors? balancedBracketSelectors)
    {
        if (!_grammars.TryGetValue(scopeName, out Grammar? grammar))
        {
            if (!_rawGrammars.TryGetValue(scopeName, out RawGrammar? rawGrammar))
            {
                return null;
            }

            grammar = Grammar.Create(
                scopeName,
                rawGrammar,
                initialLanguage,
                embeddedLanguages,
                tokenTypes,
                balancedBracketSelectors,
                this,
                _onigLib);
            _grammars[scopeName] = grammar;
        }

        return grammar;
    }
}

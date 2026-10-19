using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Parsing;
using LoomEditor.TextMate.Theme;
using TextMateGrammar = LoomEditor.TextMate.Grammar.Grammar;
using TextMateTheme = LoomEditor.TextMate.Theme.Theme;

namespace LoomEditor.TextMate.Tests.TestHelpers;

/// <summary>Builds grammars from inline JSON for the tests (a stand-in for the registry).</summary>
internal static class TestGrammars
{
    /// <summary>Parses an inline JSON grammar.</summary>
    public static RawGrammar Parse(string json) => RawGrammarReader.ParseRawGrammar(json, "test.tmLanguage.json");

    /// <summary>Creates the grammar of <paramref name="json"/> with <paramref name="repository"/> (a new empty one by default).</summary>
    public static TextMateGrammar Create(
        string json,
        FakeGrammarRepository? repository = null,
        IReadOnlyDictionary<string, int>? embeddedLanguages = null,
        IReadOnlyDictionary<string, StandardTokenType>? tokenTypes = null,
        BalancedBracketSelectors? balancedBracketSelectors = null)
    {
        RawGrammar raw = Parse(json);
        repository ??= new FakeGrammarRepository();
        return TextMateGrammar.Create(
            raw.ScopeName!,
            raw,
            1,
            embeddedLanguages,
            tokenTypes,
            balancedBracketSelectors,
            repository,
            OnigTestLib.GetOniguruma());
    }
}

/// <summary>An <see cref="IGrammarRepositoryAndThemeProvider"/> over dictionaries; counts the lookups.</summary>
internal sealed class FakeGrammarRepository : IGrammarRepositoryAndThemeProvider
{
    private readonly Dictionary<string, RawGrammar> _grammars = [];
    private readonly Dictionary<string, List<string>> _injections = [];
    private readonly TextMateTheme _theme = TextMateTheme.CreateFromRawTheme(new RawTheme([]));

    /// <summary>How many times <see cref="Lookup"/> was called, by scope name.</summary>
    public Dictionary<string, int> LookupCounts { get; } = [];

    /// <summary>Adds a raw grammar to the repository.</summary>
    public FakeGrammarRepository Add(RawGrammar grammar)
    {
        _grammars[grammar.ScopeName!] = grammar;
        return this;
    }

    /// <summary>Adds a grammar from inline JSON.</summary>
    public FakeGrammarRepository Add(string json) => Add(TestGrammars.Parse(json));

    /// <summary>Makes <paramref name="injectionScopeName"/> inject into <paramref name="scopeName"/>.</summary>
    public FakeGrammarRepository Inject(string scopeName, string injectionScopeName)
    {
        if (!_injections.TryGetValue(scopeName, out List<string>? list))
        {
            _injections[scopeName] = list = [];
        }

        list.Add(injectionScopeName);
        return this;
    }

    public RawGrammar? Lookup(string scopeName)
    {
        LookupCounts[scopeName] = LookupCounts.GetValueOrDefault(scopeName) + 1;
        return _grammars.GetValueOrDefault(scopeName);
    }

    public IReadOnlyList<string> Injections(string scopeName) => _injections.GetValueOrDefault(scopeName) ?? [];

    public StyleAttributes? ThemeMatch(ScopeStack scopePath) => _theme.Match(scopePath);

    public StyleAttributes GetDefaults() => _theme.GetDefaults();
}

// Ported from microsoft/vscode-textmate: src/grammar/grammar.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Oniguruma;
using LoomEditor.TextMate.Parsing;

namespace LoomEditor.TextMate.Grammar;

/// <summary>
/// <c>Grammar</c>: a TextMate grammar ready to tokenize. It owns the compiled rules (and so their Oniguruma scanners),
/// the injections, and resolves the external grammars that the rules include.
/// </summary>
/// <remarks>
/// <para>
/// Deliberate differences: no <c>fonts</c> (ADR 0008, decision 3); no location metadata; <c>tokenizeLine</c>,
/// <c>tokenizeLine2</c> and <c>_tokenize</c> come with P4-19 (another <c>partial</c> file).
/// </para>
/// <para>
/// Threading: one thread at a time, like upstream (ADR 0008, decision 5): rules are compiled lazily while tokenizing
/// and the state stack is mutated by <c>reset</c>. <see cref="Dispose"/> frees every rule and its scanner.
/// </para>
/// </remarks>
internal sealed partial class Grammar : IRuleFactoryHelper, IRuleRegistryAndOnigLib, IScopeAttributesContext, IDisposable
{
    private readonly string _rootScopeName;
    private readonly RawGrammar _grammar;
    private readonly List<Rule?> _ruleId2Desc = [null];
    private readonly Dictionary<string, RawGrammar> _includedGrammars = new(StringComparer.Ordinal);
    private readonly IGrammarRepositoryAndThemeProvider _grammarRepository;
    private readonly BasicScopeAttributesProvider _basicScopeAttributesProvider;
    private readonly List<TokenTypeMatcher> _tokenTypeMatchers = [];
    private readonly BalancedBracketSelectors? _balancedBracketSelectors;
    private readonly IOnigLib _onigLib;
    private int _lastRuleId;
    private List<Injection>? _injections;

    /// <summary>
    /// Creates the grammar (upstream's <c>createGrammar</c> and <c>Grammar</c> constructor). <paramref name="grammar"/>
    /// is cloned, never modified.
    /// </summary>
    /// <param name="rootScopeName">The scope name of the grammar.</param>
    /// <param name="grammar">The raw grammar.</param>
    /// <param name="initialLanguage">The language id of tokens that have no embedded language.</param>
    /// <param name="embeddedLanguages">Scope name → language id.</param>
    /// <param name="tokenTypes">Selector → token type.</param>
    /// <param name="balancedBracketSelectors">Decides which tokens contain balanced brackets.</param>
    /// <param name="grammarRepository">Where external grammars, injections and the theme come from.</param>
    /// <param name="onigLib">The regex engine.</param>
    public Grammar(
        string rootScopeName,
        RawGrammar grammar,
        int initialLanguage,
        IReadOnlyDictionary<string, int>? embeddedLanguages,
        IReadOnlyDictionary<string, StandardTokenType>? tokenTypes,
        BalancedBracketSelectors? balancedBracketSelectors,
        IGrammarRepositoryAndThemeProvider grammarRepository,
        IOnigLib onigLib)
    {
        ArgumentNullException.ThrowIfNull(rootScopeName);
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(grammarRepository);
        ArgumentNullException.ThrowIfNull(onigLib);
        _rootScopeName = rootScopeName;
        _basicScopeAttributesProvider = new BasicScopeAttributesProvider(initialLanguage, embeddedLanguages);
        _balancedBracketSelectors = balancedBracketSelectors;
        _grammarRepository = grammarRepository;
        _onigLib = onigLib;
        _grammar = InitGrammar(grammar, null);

        if (tokenTypes is not null)
        {
            foreach (KeyValuePair<string, StandardTokenType> entry in tokenTypes)
            {
                foreach (MatcherWithPriority<string[]> matcher in Matcher.CreateMatchers<string[]>(entry.Key, NameMatcher.Matches))
                {
                    _tokenTypeMatchers.Add(new TokenTypeMatcher(matcher.Matcher, entry.Value));
                }
            }
        }
    }

    /// <summary><c>createGrammar</c>.</summary>
    public static Grammar Create(
        string scopeName,
        RawGrammar grammar,
        int initialLanguage,
        IReadOnlyDictionary<string, int>? embeddedLanguages,
        IReadOnlyDictionary<string, StandardTokenType>? tokenTypes,
        BalancedBracketSelectors? balancedBracketSelectors,
        IGrammarRepositoryAndThemeProvider grammarRepository,
        IOnigLib onigLib) =>
        new(scopeName, grammar, initialLanguage, embeddedLanguages, tokenTypes, balancedBracketSelectors, grammarRepository, onigLib);

    /// <summary><c>themeProvider</c>.</summary>
    public IThemeProvider ThemeProvider => _grammarRepository;

    /// <summary>The initialized clone of the raw grammar (with <c>$self</c> and <c>$base</c> in its repository).</summary>
    internal RawGrammar RootGrammar => _grammar;

    /// <summary><c>dispose</c>: disposes the rules, and so their scanners.</summary>
    public void Dispose()
    {
        foreach (Rule? rule in _ruleId2Desc)
        {
            rule?.Dispose();
        }
    }

    /// <summary><c>createOnigScanner</c>.</summary>
    public IOnigScanner CreateOnigScanner(IReadOnlyList<string> sources) => _onigLib.CreateOnigScanner(sources);

    /// <summary><c>createOnigString</c>.</summary>
    public IOnigString CreateOnigString(string str) => _onigLib.CreateOnigString(str);

    /// <summary><c>getMetadataForScope</c>.</summary>
    public BasicScopeAttributes GetMetadataForScope(string scopeName) =>
        _basicScopeAttributesProvider.GetBasicScopeAttributes(scopeName);

    /// <summary><c>registerRule</c>: gives the factory a new id and stores the rule it returns.</summary>
    public T RegisterRule<T>(Func<RuleId, T> factory)
        where T : Rule
    {
        ArgumentNullException.ThrowIfNull(factory);
        int id = ++_lastRuleId;
        T result = factory(RuleId.FromNumber(id));
        while (_ruleId2Desc.Count <= id)
        {
            _ruleId2Desc.Add(null);
        }

        _ruleId2Desc[id] = result;
        return result;
    }

    /// <summary>
    /// <c>getRule</c>. <see langword="null"/> while the rule is being created (its factory has not returned yet), as
    /// upstream returns <c>undefined</c>.
    /// </summary>
    public Rule? GetRule(RuleId ruleId)
    {
        int id = RuleId.ToNumber(ruleId);
        return id >= 0 && id < _ruleId2Desc.Count ? _ruleId2Desc[id] : null;
    }

    /// <summary>
    /// <c>getExternalGrammar</c>: the grammar of <paramref name="scopeName"/> from the repository, initialized once and
    /// cached; its <c>$base</c> is the one of <paramref name="repository"/> when given.
    /// </summary>
    public RawGrammar? GetExternalGrammar(string scopeName, RawRepository? repository)
    {
        ArgumentNullException.ThrowIfNull(scopeName);
        if (_includedGrammars.TryGetValue(scopeName, out RawGrammar? included))
        {
            return included;
        }

        RawGrammar? rawIncludedGrammar = _grammarRepository.Lookup(scopeName);
        if (rawIncludedGrammar is null)
        {
            return null;
        }

        RawRule? @base = null;
        repository?.TryGetValue("$base", out @base);
        return _includedGrammars[scopeName] = InitGrammar(rawIncludedGrammar, @base);
    }

    /// <summary><c>getExternalGrammar</c> without a repository.</summary>
    public RawGrammar? GetExternalGrammar(string scopeName) => GetExternalGrammar(scopeName, null);

    // initGrammar: works on a clone (phase-4/README.md, trap 7), adds $self and $base to its repository.
    private static RawGrammar InitGrammar(RawGrammar grammar, RawRule? @base)
    {
        RawGrammar clone = grammar.DeepClone();
        clone.Repository ??= new RawRepository();
        var self = new RawRule { Patterns = clone.Patterns, Name = clone.ScopeName };
        clone.Repository["$self"] = self;
        clone.Repository["$base"] = @base ?? self;
        return clone;
    }
}

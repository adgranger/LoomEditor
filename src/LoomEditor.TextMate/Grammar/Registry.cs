// Ported from microsoft/vscode-textmate: src/main.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Parsing;
using LoomEditor.TextMate.Theme;
using TextMateTheme = LoomEditor.TextMate.Theme.Theme;

namespace LoomEditor.TextMate.Grammar;

/// <summary>The registry that holds all grammars: the entry point of the tokenizer.</summary>
/// <remarks>
/// <para>
/// Deliberate difference: synchronous (ADR 0008, decision 3). Loading a grammar calls
/// <see cref="RegistryOptions.LoadGrammar"/> for it and for every grammar it depends on, once per scope name, then
/// creates the grammar. Like upstream, loading a scope name that has no grammar throws
/// (<c>No grammar provided for &lt;scope&gt;</c>).
/// </para>
/// <para>
/// Threading: single-threaded, one registry per thread (ADR 0008, decision 5); <see cref="Dispose"/> disposes its
/// grammars.
/// </para>
/// </remarks>
internal sealed class Registry : IDisposable
{
    private readonly RegistryOptions _options;
    private readonly SyncRegistry _syncRegistry;
    private readonly HashSet<string> _ensureGrammarCache = new(StringComparer.Ordinal);

    /// <summary>Creates a registry.</summary>
    public Registry(RegistryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        _syncRegistry = new SyncRegistry(TextMateTheme.CreateFromRawTheme(options.Theme, options.ColorMap), options.OnigLib);
    }

    /// <summary><c>parseRawGrammar</c> (re-exported by <c>main.ts</c>): parses a JSON or plist grammar.</summary>
    public static RawGrammar ParseRawGrammar(string content, string? filePath = null) =>
        RawGrammarReader.ParseRawGrammar(content, filePath);

    /// <summary><c>dispose</c>.</summary>
    public void Dispose() => _syncRegistry.Dispose();

    /// <summary><c>setTheme</c>. Once called, no previous <c>ruleStack</c> should be used anymore.</summary>
    public void SetTheme(RawTheme theme, IReadOnlyList<string>? colorMap = null) =>
        _syncRegistry.SetTheme(TextMateTheme.CreateFromRawTheme(theme, colorMap));

    /// <summary><c>getColorMap</c>: a lookup array for color ids.</summary>
    public string?[] GetColorMap() => _syncRegistry.GetColorMap();

    /// <summary>
    /// <c>loadGrammarWithEmbeddedLanguages</c>: loads the grammar for <paramref name="initialScopeName"/> and all
    /// referenced included grammars. Please do not use language id 0.
    /// </summary>
    public IGrammar? LoadGrammarWithEmbeddedLanguages(
        string initialScopeName,
        int initialLanguage,
        IReadOnlyDictionary<string, int> embeddedLanguages) =>
        LoadGrammarWithConfiguration(initialScopeName, initialLanguage, new GrammarConfiguration(EmbeddedLanguages: embeddedLanguages));

    /// <summary>
    /// <c>loadGrammarWithConfiguration</c>: loads the grammar for <paramref name="initialScopeName"/> and all
    /// referenced included grammars. Please do not use language id 0.
    /// </summary>
    public IGrammar? LoadGrammarWithConfiguration(string initialScopeName, int initialLanguage, GrammarConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return LoadGrammarCore(
            initialScopeName,
            initialLanguage,
            configuration.EmbeddedLanguages,
            configuration.TokenTypes,
            new BalancedBracketSelectors(configuration.BalancedBracketSelectors ?? [], configuration.UnbalancedBracketSelectors ?? []));
    }

    /// <summary><c>loadGrammar</c>: loads the grammar for <paramref name="initialScopeName"/> and all referenced included grammars.</summary>
    public IGrammar? LoadGrammar(string initialScopeName) => LoadGrammarCore(initialScopeName, 0, null, null, null);

    /// <summary>
    /// <c>addGrammar</c>: adds a raw grammar and returns its grammar. If the grammar of that scope name was already
    /// created it is returned as it is, whatever the other arguments are.
    /// </summary>
    public IGrammar AddGrammar(
        RawGrammar rawGrammar,
        IReadOnlyList<string>? injections = null,
        int initialLanguage = 0,
        IReadOnlyDictionary<string, int>? embeddedLanguages = null)
    {
        ArgumentNullException.ThrowIfNull(rawGrammar);
        _syncRegistry.AddGrammar(rawGrammar, injections ?? []);
        return GrammarForScopeName(rawGrammar.ScopeName!, initialLanguage, embeddedLanguages)!;
    }

    // _loadGrammar: process the dependency queue, then create the grammar.
    private IGrammar? LoadGrammarCore(
        string initialScopeName,
        int initialLanguage,
        IReadOnlyDictionary<string, int>? embeddedLanguages,
        IReadOnlyDictionary<string, StandardTokenType>? tokenTypes,
        BalancedBracketSelectors? balancedBracketSelectors)
    {
        ArgumentNullException.ThrowIfNull(initialScopeName);
        var dependencyProcessor = new ScopeDependencyProcessor(_syncRegistry.Lookup, initialScopeName, _syncRegistry.Injections);
        while (dependencyProcessor.Q.Count > 0)
        {
            foreach (AbsoluteRuleReference request in dependencyProcessor.Q)
            {
                LoadSingleGrammar(request.ScopeName);
            }

            dependencyProcessor.ProcessQueue();
        }

        return GrammarForScopeName(initialScopeName, initialLanguage, embeddedLanguages, tokenTypes, balancedBracketSelectors);
    }

    // _loadSingleGrammar / _doLoadSingleGrammar: the loader is called once per scope name, found or not.
    private void LoadSingleGrammar(string scopeName)
    {
        if (!_ensureGrammarCache.Add(scopeName))
        {
            return;
        }

        RawGrammar? grammar = _options.LoadGrammar(scopeName);
        if (grammar is not null)
        {
            _syncRegistry.AddGrammar(grammar, _options.GetInjections?.Invoke(scopeName));
        }
    }

    // _grammarForScopeName
    private IGrammar? GrammarForScopeName(
        string scopeName,
        int initialLanguage = 0,
        IReadOnlyDictionary<string, int>? embeddedLanguages = null,
        IReadOnlyDictionary<string, StandardTokenType>? tokenTypes = null,
        BalancedBracketSelectors? balancedBracketSelectors = null) =>
        _syncRegistry.GrammarForScopeName(scopeName, initialLanguage, embeddedLanguages, tokenTypes, balancedBracketSelectors);
}

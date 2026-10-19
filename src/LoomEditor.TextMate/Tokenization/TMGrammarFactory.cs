// Ported from microsoft/vscode: src/vs/workbench/services/textMate/common/TMGrammarFactory.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Oniguruma;
using LoomEditor.TextMate.Theme;

namespace LoomEditor.TextMate.Tokenization;

/// <summary>The result of <see cref="TMGrammarFactory.CreateGrammar"/> (VS Code: <c>ICreateGrammarResult</c>).</summary>
/// <param name="LanguageId"><c>languageId</c>.</param>
/// <param name="Grammar"><c>grammar</c>: <see langword="null"/> if the grammar could not be loaded.</param>
/// <param name="InitialState"><c>initialState</c>.</param>
/// <param name="ContainsEmbeddedLanguages"><c>containsEmbeddedLanguages</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record CreateGrammarResult(string LanguageId, IGrammar? Grammar, StateStack InitialState, bool ContainsEmbeddedLanguages);

/// <summary>
/// Creates the grammar of a language from grammar definitions: injections, embedded languages and token types (VS Code:
/// <c>TMGrammarFactory</c>). Synchronous: grammar files are read with a <c>Func</c> (resource name → text).
/// </summary>
/// <remarks>Threading: single-threaded, one factory per thread; the <see cref="RawGrammarCache"/> may be shared.</remarks>
internal sealed class TMGrammarFactory : IDisposable
{
    /// <summary><c>missingTMGrammarErrorMessage</c>.</summary>
    public const string MissingTMGrammarErrorMessage = "No TM Grammar registered for this language.";

    private readonly StateStack _initialState = StateStack.Initial;
    private readonly TMScopeRegistry _scopeRegistry = new();
    private readonly Dictionary<string, List<string>> _injections = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<IReadOnlyDictionary<string, int>>> _injectedEmbeddedLanguages = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _languageToScope = new(StringComparer.Ordinal);
    private readonly Registry _grammarRegistry;

    /// <summary>Creates a factory for <paramref name="grammarDefinitions"/>.</summary>
    /// <param name="grammarDefinitions">The validated definitions (see <see cref="LanguageRegistry.CreateGrammarDefinitions"/>).</param>
    /// <param name="readFile">Reads a grammar file by resource name; <see langword="null"/> when it does not exist.</param>
    /// <param name="rawGrammars">Parses each file once.</param>
    /// <param name="onigLib">The regex engine.</param>
    public TMGrammarFactory(
        IReadOnlyList<ValidGrammarDefinition> grammarDefinitions,
        Func<string, string?> readFile,
        RawGrammarCache rawGrammars,
        IOnigLib onigLib)
    {
        ArgumentNullException.ThrowIfNull(grammarDefinitions);
        ArgumentNullException.ThrowIfNull(readFile);
        ArgumentNullException.ThrowIfNull(rawGrammars);
        ArgumentNullException.ThrowIfNull(onigLib);

        _grammarRegistry = new Registry(new RegistryOptions(
            onigLib,
            scopeName =>
            {
                ValidGrammarDefinition? grammarDefinition = _scopeRegistry.GetGrammarDefinition(scopeName);
                if (grammarDefinition is null)
                {
                    Debug.WriteLine($"No grammar found for scope {scopeName}");
                    return null;
                }

                return rawGrammars.Get(grammarDefinition.Location, readFile);
            },
            GetInjections: scopeName =>
            {
                string[] scopeParts = scopeName.Split('.');
                List<string> injections = [];
                for (int i = 1; i <= scopeParts.Length; i++)
                {
                    string subScopeName = string.Join('.', scopeParts.Take(i));
                    if (_injections.TryGetValue(subScopeName, out List<string>? found))
                    {
                        injections.AddRange(found);
                    }
                }

                return injections;
            }));

        foreach (ValidGrammarDefinition validGrammar in grammarDefinitions)
        {
            _scopeRegistry.Register(validGrammar);

            if (validGrammar.InjectTo is not null)
            {
                foreach (string injectScope in validGrammar.InjectTo)
                {
                    GetOrAdd(_injections, injectScope).Add(validGrammar.ScopeName);
                    GetOrAdd(_injectedEmbeddedLanguages, injectScope).Add(validGrammar.EmbeddedLanguages);
                }
            }

            if (validGrammar.Language is not null)
            {
                _languageToScope[validGrammar.Language] = validGrammar.ScopeName;
            }
        }
    }

    private static List<T> GetOrAdd<T>(Dictionary<string, List<T>> dictionary, string key)
    {
        if (!dictionary.TryGetValue(key, out List<T>? list))
        {
            dictionary[key] = list = [];
        }

        return list;
    }

    /// <summary><c>dispose</c>.</summary>
    public void Dispose() => _grammarRegistry.Dispose();

    /// <summary><c>has</c>: whether a grammar is registered for the language.</summary>
    public bool Has(string languageId) => _languageToScope.ContainsKey(languageId);

    /// <summary><c>setTheme</c>.</summary>
    public void SetTheme(RawTheme theme, IReadOnlyList<string> colorMap) => _grammarRegistry.SetTheme(theme, colorMap);

    /// <summary><c>getColorMap</c>.</summary>
    public string?[] GetColorMap() => _grammarRegistry.GetColorMap();

    /// <summary><c>createGrammar</c>.</summary>
    /// <exception cref="InvalidOperationException">No grammar is registered for the language (<see cref="MissingTMGrammarErrorMessage"/>).</exception>
    public CreateGrammarResult CreateGrammar(string languageId, int encodedLanguageId)
    {
        if (!_languageToScope.TryGetValue(languageId, out string? scopeName)
            || _scopeRegistry.GetGrammarDefinition(scopeName) is not { } grammarDefinition)
        {
            // No TM grammar defined
            throw new InvalidOperationException(MissingTMGrammarErrorMessage);
        }

        // A copy: the definition is shared and must not get the injected languages.
        var embeddedLanguages = new Dictionary<string, int>(grammarDefinition.EmbeddedLanguages, StringComparer.Ordinal);
        if (_injectedEmbeddedLanguages.TryGetValue(scopeName, out List<IReadOnlyDictionary<string, int>>? injectedEmbeddedLanguages))
        {
            foreach (IReadOnlyDictionary<string, int> injected in injectedEmbeddedLanguages)
            {
                foreach ((string scope, int language) in injected)
                {
                    embeddedLanguages[scope] = language;
                }
            }
        }

        IGrammar? grammar = _grammarRegistry.LoadGrammarWithConfiguration(
            scopeName,
            encodedLanguageId,
            new GrammarConfiguration(
                embeddedLanguages,
                grammarDefinition.TokenTypes,
                grammarDefinition.BalancedBracketSelectors,
                grammarDefinition.UnbalancedBracketSelectors));

        return new CreateGrammarResult(languageId, grammar, _initialState, embeddedLanguages.Count > 0);
    }
}

// Ported from microsoft/vscode: src/vs/workbench/services/textMate/browser/textMateTokenizationFeatureImpl.ts (_getOrCreateGrammarFactory, _updateTheme)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Threading;
using LoomEditor.Core.Tokens;
using LoomEditor.TextMate.Grammars;
using LoomEditor.TextMate.Oniguruma;
using LoomEditor.TextMate.Theme;
using LoomEditor.TextMate.Tokenization;
using LoomEditor.TextMate.Tokenization.Worker;
using LoomEditor.Themes;

namespace LoomEditor.View.Highlighting;

/// <summary>
/// Joins everything that highlights the model of one editor: the built-in languages, the editor's
/// <see cref="TokenizationRegistry"/>, the UI-thread grammar factory, the background worker and the theme. Setting a
/// language or a theme re-tokenizes the model. Without a dispatcher there is no worker: the model uses its default
/// background tokenizer with a scheduler that does nothing, so only the visible lines are tokenized (heuristically).
/// </summary>
/// <remarks>Threading: UI thread only; the worker runs on its own thread and delivers through the dispatcher.</remarks>
internal sealed class SyntaxHighlighter : IDisposable
{
    private readonly TextModel _model;
    private readonly TokenizationRegistry _registry;
    private readonly LanguageRegistry _languages;
    private readonly LanguageConfigurationRegistry _configurations;
    private readonly HashSet<string> _configuredLanguages = new(StringComparer.Ordinal);
    private readonly List<IDisposable> _registrations = [];
    private readonly ThreadedBackgroundTokenizerFactory? _threaded;
    private TMGrammarFactory? _grammarFactory;
    private TextMateTokenizationFactory? _tokenization;
    private EditorTheme _theme;
    private bool _workerStopped;

    /// <summary>Creates the highlighter of <paramref name="model"/>, with the Dark Modern theme.</summary>
    /// <param name="model">A model created with <paramref name="registry"/> and the codec of <paramref name="languages"/>.</param>
    /// <param name="registry">The registry of the model.</param>
    /// <param name="languages">The known languages, all registered.</param>
    /// <param name="configurations">The language configuration registry of the model.</param>
    /// <param name="dispatcher">The UI thread, or <see langword="null"/> for no background worker.</param>
    public SyntaxHighlighter(
        TextModel model,
        TokenizationRegistry registry,
        LanguageRegistry languages,
        LanguageConfigurationRegistry configurations,
        IUiDispatcher? dispatcher)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(languages);
        ArgumentNullException.ThrowIfNull(configurations);
        _model = model;
        _registry = registry;
        _languages = languages;
        _configurations = configurations;
        if (dispatcher is not null)
        {
            _threaded = new ThreadedBackgroundTokenizerFactory(
                dispatcher,
                languages.Codec,
                (definitions, host) => new TokenizationWorker(
                    () => new TMGrammarFactory(definitions, BuiltInLanguages.ReadFile, new RawGrammarCache(), new OnigurumaEngine()),
                    host));
            _threaded.SetGrammarDefinitions(languages.CreateGrammarDefinitions());
        }

        _theme = EditorTheme.Dark;
        SetTheme(_theme);
    }

    /// <summary>The color map the tokens refer to (ARGB), or <see langword="null"/> before the first theme.</summary>
    internal IReadOnlyList<uint>? ColorMap => _registry.GetColorMap();

    /// <summary>The number of models the worker currently tokenizes (0 without a worker or once disposed).</summary>
    internal int WorkerControllerCount => _threaded?.ControllerCount ?? 0;

    /// <summary>
    /// Sets the language of the model; a language with no grammar, or an unknown id, is plain text. The tokenization
    /// support of a language is created the first time it is needed, and so is the registration of the bundled language
    /// configurations of the language and of the languages its grammar embeds (VS Code: languageConfigurationExtensionPoint.ts,
    /// priority 50).
    /// </summary>
    public void SetLanguage(string languageId)
    {
        ArgumentNullException.ThrowIfNull(languageId);
        if (!_languages.IsRegisteredLanguageId(languageId))
        {
            languageId = LanguageIdCodec.PlainTextLanguageId;
        }

        if (_registry.Get(languageId) is null && GetTokenizationFactory().CreateTokenizationSupport(languageId) is { } support)
        {
            _registrations.Add(_registry.Register(languageId, support));
        }

        RegisterConfiguration(languageId);
        if (_languages.GetLanguageDefinition(languageId)?.Grammar is { } grammar)
        {
            foreach (string embedded in grammar.EmbeddedLanguages.Values)
            {
                RegisterConfiguration(embedded);
            }
        }

        _model.Tokenization.SetLanguageId(languageId);
    }

    /// <summary>Sets the theme: the grammars and the worker get its token rules, then the registry gets its color map.</summary>
    public void SetTheme(EditorTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        _theme = theme;
        RawTheme rawTheme = ThemeToTextMate.ToRawTheme(theme);
        IReadOnlyList<string> textColorMap = ThemeToTextMate.ToTextColorMap(theme);
        _grammarFactory?.SetTheme(rawTheme, textColorMap);
        _threaded?.AcceptTheme(rawTheme, textColorMap);
        _registry.SetColorMap(ThemeToTextMate.ToColorMap(theme));
    }

    /// <summary>Stops the background worker (the control is unloaded); the visible lines are still tokenized on demand.</summary>
    public void StopWorker()
    {
        if (_threaded is not null)
        {
            _threaded.SetGrammarDefinitions(_languages.CreateGrammarDefinitions());
            _workerStopped = true;
        }
    }

    /// <summary>Starts the worker again after <see cref="StopWorker"/> (the control is loaded again).</summary>
    public void StartWorker()
    {
        if (_workerStopped)
        {
            _workerStopped = false;
            _model.Tokenization.ResetTokenization();
        }
    }

    /// <summary>Stops the worker and disposes the grammar factory.</summary>
    public void Dispose()
    {
        foreach (IDisposable registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();
        _configuredLanguages.Clear();
        _threaded?.Dispose();
        _grammarFactory?.Dispose();
        _grammarFactory = null;
        _tokenization = null;
    }

    // Languages without a bundled configuration (jsx-tags, plaintext) keep the empty configuration.
    private void RegisterConfiguration(string languageId)
    {
        if (_languages.GetLanguageDefinition(languageId)?.ConfigurationLocation is not { } location
            || !_configuredLanguages.Add(languageId)
            || BuiltInLanguages.ReadFile(location) is not { } json)
        {
            return;
        }

        _registrations.Add(_configurations.Register(languageId, LanguageConfigurationReader.Read(json, languageId), 50));
    }

    private TextMateTokenizationFactory GetTokenizationFactory()
    {
        if (_tokenization is null)
        {
            _grammarFactory = new TMGrammarFactory(_languages.CreateGrammarDefinitions(), BuiltInLanguages.ReadFile, new RawGrammarCache(), new OnigurumaEngine());
            _grammarFactory.SetTheme(ThemeToTextMate.ToRawTheme(_theme), ThemeToTextMate.ToTextColorMap(_theme));
            _tokenization = new TextMateTokenizationFactory(_languages, _grammarFactory, _threaded is null ? null : _threaded.CreateBackgroundTokenizer);
        }

        return _tokenization;
    }
}

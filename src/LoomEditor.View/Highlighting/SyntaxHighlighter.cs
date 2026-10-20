// Ported from microsoft/vscode: src/vs/workbench/services/textMate/browser/textMateTokenizationFeatureImpl.ts (_getOrCreateGrammarFactory, _updateTheme)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

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
    private readonly List<IDisposable> _registrations = [];
    private readonly ThreadedBackgroundTokenizerFactory? _threaded;
    private TMGrammarFactory? _grammarFactory;
    private TextMateTokenizationFactory? _tokenization;
    private EditorTheme _theme;

    /// <summary>Creates the highlighter of <paramref name="model"/>, with the Dark Modern theme.</summary>
    /// <param name="model">A model created with <paramref name="registry"/> and the codec of <paramref name="languages"/>.</param>
    /// <param name="registry">The registry of the model.</param>
    /// <param name="languages">The known languages, all registered.</param>
    /// <param name="dispatcher">The UI thread, or <see langword="null"/> for no background worker.</param>
    public SyntaxHighlighter(TextModel model, TokenizationRegistry registry, LanguageRegistry languages, IUiDispatcher? dispatcher)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(languages);
        _model = model;
        _registry = registry;
        _languages = languages;
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
    /// support of a language is created the first time it is needed.
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

    /// <summary>Stops the worker and disposes the grammar factory.</summary>
    public void Dispose()
    {
        foreach (IDisposable registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();
        _threaded?.Dispose();
        _grammarFactory?.Dispose();
        _grammarFactory = null;
        _tokenization = null;
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

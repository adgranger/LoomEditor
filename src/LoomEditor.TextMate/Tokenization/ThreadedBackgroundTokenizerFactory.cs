// Ported from microsoft/vscode: src/vs/workbench/services/textMate/browser/backgroundTokenization/threadedBackgroundTokenizerFactory.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Threading;
using LoomEditor.Core.Tokens;
using LoomEditor.TextMate.Theme;
using LoomEditor.TextMate.Tokenization.Worker;

namespace LoomEditor.TextMate.Tokenization;

/// <summary>
/// Creates the background tokenizers that run on the tokenization worker, and routes the worker's results to the right
/// controller on the UI thread (Monaco: <c>ThreadedBackgroundTokenizerFactory</c>). Differences: the worker is made by
/// a function (the real one starts a <see cref="TokenizationWorker"/> thread, tests pass a stand-in), creation is
/// synchronous (no WASM, no web worker), and there is no telemetry or mismatch notification.
/// </summary>
/// <remarks>Threading: UI thread only, except the host given to the worker, which is called on the worker thread and posts to the <see cref="IUiDispatcher"/>.</remarks>
internal sealed class ThreadedBackgroundTokenizerFactory : IDisposable
{
    private readonly IUiDispatcher _dispatcher;
    private readonly ILanguageIdCodec _languageIdCodec;
    private readonly Func<IReadOnlyList<ValidGrammarDefinition>, ITextMateWorkerHost, ITokenizationWorker> _createWorker;
    private readonly Func<bool> _shouldTokenizeAsync;
    private readonly int _maxTokenizationLineLength;
    private readonly Dictionary<int, TextMateWorkerTokenizerController> _controllers = [];

    private IReadOnlyList<ValidGrammarDefinition> _grammarDefinitions = [];
    private RawTheme? _currentTheme;
    private IReadOnlyList<string>? _currentTokenColorMap;
    private ITokenizationWorker? _worker;

    /// <summary>Creates a factory; no worker exists until the first model needs one.</summary>
    /// <param name="dispatcher">Delivers the results of the worker to the UI thread.</param>
    /// <param name="languageIdCodec">Encodes the language ids sent to the worker.</param>
    /// <param name="createWorker">Starts a worker for some grammar definitions; the host is where it sends its results.</param>
    /// <param name="shouldTokenizeAsync">Whether to use the worker at all (<c>editor.experimental.asyncTokenization</c>); default: always.</param>
    /// <param name="maxTokenizationLineLength">Lines of this length or more are not tokenized.</param>
    public ThreadedBackgroundTokenizerFactory(
        IUiDispatcher dispatcher,
        ILanguageIdCodec languageIdCodec,
        Func<IReadOnlyList<ValidGrammarDefinition>, ITextMateWorkerHost, ITokenizationWorker> createWorker,
        Func<bool>? shouldTokenizeAsync = null,
        int maxTokenizationLineLength = TokenizationSupportWithLineLimit.DefaultMaxTokenizationLineLength)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(languageIdCodec);
        ArgumentNullException.ThrowIfNull(createWorker);
        _dispatcher = dispatcher;
        _languageIdCodec = languageIdCodec;
        _createWorker = createWorker;
        _shouldTokenizeAsync = shouldTokenizeAsync ?? (() => true);
        _maxTokenizationLineLength = maxTokenizationLineLength;
    }

    /// <summary>The number of models that currently have a controller (attached models).</summary>
    internal int ControllerCount => _controllers.Count;

    /// <summary><c>dispose</c>.</summary>
    public void Dispose() => DisposeWorker();

    /// <summary>
    /// <c>createBackgroundTokenizer</c>: returns <see langword="null"/> (the caller falls back to the default background
    /// tokenizer) when async tokenization is off or the model is too large to be synced. The controller exists while the
    /// model is attached to at least one view.
    /// </summary>
    public IBackgroundTokenizer? CreateBackgroundTokenizer(TextModel textModel, IBackgroundTokenizationStore tokenStore)
    {
        ArgumentNullException.ThrowIfNull(textModel);
        ArgumentNullException.ThrowIfNull(tokenStore);
        if (!_shouldTokenizeAsync() || textModel.IsTooLargeForSyncing)
        {
            return null;
        }

        ITokenizationWorker worker = GetWorker();
        return new ThreadedBackgroundTokenizer(this, textModel, tokenStore, worker);
    }

    /// <summary><c>setGrammarDefinitions</c>: the worker is recreated, with the new definitions, for the next model.</summary>
    public void SetGrammarDefinitions(IReadOnlyList<ValidGrammarDefinition> grammarDefinitions)
    {
        ArgumentNullException.ThrowIfNull(grammarDefinitions);
        _grammarDefinitions = grammarDefinitions;
        DisposeWorker();
    }

    /// <summary><c>acceptTheme</c>.</summary>
    public void AcceptTheme(RawTheme theme, IReadOnlyList<string> colorMap)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(colorMap);
        _currentTheme = theme;
        _currentTokenColorMap = colorMap;
        _worker?.Post(new ThemeMessage(theme, colorMap));
    }

    private ITokenizationWorker GetWorker()
    {
        if (_worker is null)
        {
            _worker = _createWorker(_grammarDefinitions, new Host(this));
            if (_currentTheme is not null && _currentTokenColorMap is not null)
            {
                _worker.Post(new ThemeMessage(_currentTheme, _currentTokenColorMap));
            }
        }

        return _worker;
    }

    private void DisposeWorker()
    {
        foreach (TextMateWorkerTokenizerController controller in _controllers.Values)
        {
            controller.Dispose();
        }

        _controllers.Clear();
        _worker?.Dispose();
        _worker = null;
    }

    // $setTokensAndStates: runs on the worker thread, the result is applied on the UI thread.
    private sealed class Host(ThreadedBackgroundTokenizerFactory factory) : ITextMateWorkerHost
    {
        public void SetTokensAndStates(int controllerId, int versionId, IReadOnlyList<ContiguousMultilineTokens> tokens, IReadOnlyList<StateDeltas> stateDeltas) =>
            factory._dispatcher.Post(() =>
            {
                // When a model detaches, it is removed synchronously from the map.
                // However, the worker might still be sending tokens for that model,
                // so we ignore the event when there is no controller.
                if (factory._controllers.TryGetValue(controllerId, out TextMateWorkerTokenizerController? controller))
                {
                    controller.SetTokensAndStates(controllerId, versionId, tokens, stateDeltas);
                }
            });
    }

    // The IBackgroundTokenizer of a model; keepAliveWhenAttached: the controller lives while the model is attached.
    private sealed class ThreadedBackgroundTokenizer : IBackgroundTokenizer
    {
        private readonly ThreadedBackgroundTokenizerFactory _factory;
        private readonly TextModel _model;
        private readonly IBackgroundTokenizationStore _store;
        private readonly ITokenizationWorker _worker;
        private TextMateWorkerTokenizerController? _controller;
        private bool _isDisposed;

        public ThreadedBackgroundTokenizer(ThreadedBackgroundTokenizerFactory factory, TextModel model, IBackgroundTokenizationStore store, ITokenizationWorker worker)
        {
            _factory = factory;
            _model = model;
            _store = store;
            _worker = worker;
            _model.AttachedChanged += OnAttachedChanged;
            CheckAttached();
        }

        public void RequestTokens(int startLineNumber, int endLineNumberExclusive)
        {
            // If there is no controller, the model has been detached in the meantime.
            // Only use the controller if the worker is the same!
            if (_controller is not null && ReferenceEquals(_worker, _factory._worker))
            {
                _controller.RequestTokens(startLineNumber, endLineNumberExclusive);
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _model.AttachedChanged -= OnAttachedChanged;
            DisposeController();
        }

        private void OnAttachedChanged(object? sender, EventArgs e) => CheckAttached();

        private void CheckAttached()
        {
            if (_model.IsAttachedToEditor)
            {
                if (_controller is null && ReferenceEquals(_worker, _factory._worker))
                {
                    _controller = new TextMateWorkerTokenizerController(_model, _worker, _factory._languageIdCodec, _store, _factory._maxTokenizationLineLength);
                    _factory._controllers[_controller.ControllerId] = _controller;
                }
            }
            else
            {
                DisposeController();
            }
        }

        private void DisposeController()
        {
            if (_controller is null)
            {
                return;
            }

            _factory._controllers.Remove(_controller.ControllerId);
            _controller.Dispose();
            _controller = null;
        }
    }
}

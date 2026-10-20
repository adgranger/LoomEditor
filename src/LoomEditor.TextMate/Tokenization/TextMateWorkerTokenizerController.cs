// Ported from microsoft/vscode: src/vs/workbench/services/textMate/browser/backgroundTokenization/textMateWorkerTokenizerController.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Tokens;
using LoomEditor.Core.Tokens;
using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Tokenization.Worker;

namespace LoomEditor.TextMate.Tokenization;

/// <summary>
/// The UI side of the tokenization worker for one model (Monaco: <c>TextMateWorkerTokenizerController</c>): it mirrors
/// the changes of the model to the worker and applies the batches the worker sends back. Differences: the worker is an
/// <see cref="ITokenizationWorker"/> message sink, states arrive as <see cref="StateStack"/>s (no
/// <c>applyStateStackDiff</c>), and there are no font tokens and no logging.
/// </summary>
/// <remarks>Threading: UI thread only (the thread that owns the model); <see cref="SetTokensAndStates"/> runs there too, posted by the host.</remarks>
internal sealed partial class TextMateWorkerTokenizerController : IDisposable
{
    private static int _nextControllerId;

    private readonly TextModel _model;
    private readonly ITokenizationWorker _worker;
    private readonly ILanguageIdCodec _languageIdCodec;
    private readonly IBackgroundTokenizationStore _backgroundTokenizationStore;
    private readonly List<PendingChange> _pendingChanges = [];

    // These states will eventually equal the worker states. _states[i] stores the state at the end of line number i + 1.
    private readonly TokenizationStateStore<StateStack> _states = new();
    private bool _isDisposed;

    /// <summary>Mirrors <paramref name="model"/> to <paramref name="worker"/>: sends the model, then every change.</summary>
    public TextMateWorkerTokenizerController(
        TextModel model,
        ITokenizationWorker worker,
        ILanguageIdCodec languageIdCodec,
        IBackgroundTokenizationStore backgroundTokenizationStore,
        int maxTokenizationLineLength)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(worker);
        ArgumentNullException.ThrowIfNull(languageIdCodec);
        ArgumentNullException.ThrowIfNull(backgroundTokenizationStore);
        _model = model;
        _worker = worker;
        _languageIdCodec = languageIdCodec;
        _backgroundTokenizationStore = backgroundTokenizationStore;
        ControllerId = Interlocked.Increment(ref _nextControllerId);

        _model.ContentChanged += OnContentChanged;
        _model.LanguageChanged += OnLanguageChanged;

        string languageId = _model.Tokenization.LanguageId;
        _worker.Post(new NewModelMessage(
            ControllerId,
            _model.GetLinesContent(),
            _model.Eol,
            _model.VersionId,
            languageId,
            _languageIdCodec.EncodeLanguageId(languageId),
            maxTokenizationLineLength));
    }

    /// <summary><c>controllerId</c>: identifies this model in the messages of the worker.</summary>
    public int ControllerId { get; }

    /// <summary><c>dispose</c>: stops mirroring the model and removes it from the worker.</summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _model.ContentChanged -= OnContentChanged;
        _model.LanguageChanged -= OnLanguageChanged;
        _worker.Post(new RemovedModelMessage(ControllerId));
    }

    /// <summary><c>requestTokens</c>.</summary>
    public void RequestTokens(int startLineNumber, int endLineNumberExclusive)
    {
        if (!_isDisposed)
        {
            _worker.Post(new RetokenizeMessage(ControllerId, startLineNumber, endLineNumberExclusive));
        }
    }

    /// <summary><c>acceptMaxTokenizationLineLength</c>: <c>editor.maxTokenizationLineLength</c> changed.</summary>
    public void AcceptMaxTokenizationLineLength(int value)
    {
        if (!_isDisposed)
        {
            _worker.Post(new MaxTokenizationLineLengthMessage(ControllerId, value));
        }
    }

    /// <summary>
    /// <c>setTokensAndStates</c>: applies a batch computed for version <paramref name="versionId"/> of the model.
    /// Results for another controller, or for a disposed one, are dropped.
    /// </summary>
    public void SetTokensAndStates(int controllerId, int versionId, IReadOnlyList<ContiguousMultilineTokens> rawTokens, IReadOnlyList<StateDeltas> stateDeltas)
    {
        ArgumentNullException.ThrowIfNull(rawTokens);
        ArgumentNullException.ThrowIfNull(stateDeltas);
        if (_isDisposed || ControllerId != controllerId)
        {
            // This event is for an outdated controller (the worker didn't receive the delete/create messages yet), ignore the event.
            return;
        }

        // _states state, change{k}, ..., change{versionId}, state delta base & rawTokens, change{j}, ..., change{m}, current renderer state
        //                ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^                                ^^^^^^^^^^^^^^^^^^^^^^^^^
        //                | past changes                                                   | future states
        IReadOnlyList<ContiguousMultilineTokens> tokens = rawTokens;

        // Apply past changes to _states
        while (_pendingChanges.Count > 0 && _pendingChanges[0].VersionId <= versionId)
        {
            PendingChange change = _pendingChanges[0];
            _pendingChanges.RemoveAt(0);
            _states.AcceptChanges(change.Changes);
        }

        if (_pendingChanges.Count > 0)
        {
            tokens = RebaseTokens(tokens);
        }

        IIndexTransformer curToFutureTransformerStates = CreateCurToFutureTransformer();

        // Apply state deltas to _states and _backgroundTokenizationStore
        foreach (StateDeltas d in stateDeltas)
        {
            for (int i = 0; i < d.States.Count; i++)
            {
                StateStack? delta = d.States[i];
                StateStack state;
                if (delta is not null)
                {
                    state = delta;
                    _states.SetEndState(d.StartLineNumber + i, state);
                }
                else
                {
                    state = _states.GetEndState(d.StartLineNumber + i)!;
                }

                int? offset = curToFutureTransformerStates.Transform(d.StartLineNumber + i - 1);
                if (offset is { } line)
                {
                    // Only set the state if there is no future change in this line,
                    // as this might make consumers believe that the state/tokens are accurate
                    _backgroundTokenizationStore.SetEndState(line + 1, state);
                }

                if (d.StartLineNumber + i >= _model.LineCount - 1)
                {
                    _backgroundTokenizationStore.BackgroundTokenizationFinished();
                }
            }
        }

        // First set states, then tokens, so that events fired from set tokens don't read invalid states
        _backgroundTokenizationStore.SetTokens(tokens);
    }

    private void OnContentChanged(object? sender, ModelContentChangedEventArgs e)
    {
        _worker.Post(new ModelChangedMessage(ControllerId, new MirrorModelChangedEvent(e.Changes, e.Eol, e.VersionId)));
        _pendingChanges.Add(new PendingChange(e.VersionId, e.Changes));
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        string languageId = _model.Tokenization.LanguageId;
        _worker.Post(new LanguageChangedMessage(ControllerId, languageId, _languageIdCodec.EncodeLanguageId(languageId)));
    }

    private readonly record struct PendingChange(int VersionId, IReadOnlyList<ModelContentChange> Changes);
}

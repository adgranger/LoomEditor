// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diagnostics;
using LoomEditor.Core.Filters;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Text;
using LoomEditor.Core.Threading;
using LoomEditor.View.Cursors;
using LoomEditor.View.Events;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Suggest;

/// <summary>
/// One completion session: <see cref="Trigger"/> asks the providers, builds a <see cref="CompletionModel"/> and raises <see cref="DidSuggest"/>;
/// typing, moving the cursor, editing and changing providers refilter, retrigger or cancel the session (Monaco: <c>SuggestModel</c>).
/// Not ported: quick suggest and trigger characters (P8-17), composition (<c>editorIsComposing</c>: an IME composition can refilter),
/// inline completions, clipboard, snippets sort order, word distance, telemetry, <c>onDidChangeModel</c>, configuration changes.
/// </summary>
/// <remarks>Threading: UI thread only; the provider tasks complete anywhere and come back through the dispatcher (ADR 0011 decision 4).</remarks>
internal sealed partial class SuggestModel : IDisposable
{
    private readonly IEditorContext _context;
    private readonly SuggestModelOptions _modelOptions;
    private readonly ViewModel? _viewModel;
    private readonly LanguageFeatureRegistry<ICompletionItemProvider>? _providers;
    private readonly IUiDispatcher _dispatcher;

    private SuggestTriggerOptions? _triggerState;
    private CancellationTokenSource? _requestToken;
    private LineContext? _lineContext;
    private Selection _currentSelection;
    private CompletionModel? _completionModel;
    private bool _refilterPending;

    /// <summary>Starts listening to <paramref name="context"/>; a context that is not an <see cref="EditorContext"/> never triggers.</summary>
    public SuggestModel(IEditorContext context, SuggestModelOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        _context = context;
        _modelOptions = options;
        _viewModel = context.GetViewModel();
        _providers = context.GetLanguageFeatures()?.CompletionProviders;
        _dispatcher = context.GetUiDispatcher();
        _currentSelection = context.Selection;

        if (_viewModel is not null)
        {
            _viewModel.EventDispatcher.OnEvent += OnViewModelEvent;
        }

        context.Model.ContentChanged += OnContentChanged;
        context.Model.LanguageChanged += OnLanguageChanged;
        if (_providers is not null)
        {
            _providers.Changed += OnProvidersChanged;
        }
    }

    /// <summary>Raised when a session ends.</summary>
    public event EventHandler<SuggestCancelEventArgs>? DidCancel;

    /// <summary>Raised when a session starts, before the providers are asked.</summary>
    public event EventHandler<SuggestTriggerEventArgs>? DidTrigger;

    /// <summary>Raised when the list of the session is available or changed.</summary>
    public event EventHandler<SuggestEventArgs>? DidSuggest;

    /// <summary>Idle without a session, Manual or Auto otherwise.</summary>
    public SuggestState State => _triggerState is null ? SuggestState.Idle : _triggerState.Auto ? SuggestState.Auto : SuggestState.Manual;

    /// <summary>Stops listening and ends the session.</summary>
    public void Dispose()
    {
        if (_viewModel is not null)
        {
            _viewModel.EventDispatcher.OnEvent -= OnViewModelEvent;
        }

        _context.Model.ContentChanged -= OnContentChanged;
        _context.Model.LanguageChanged -= OnLanguageChanged;
        if (_providers is not null)
        {
            _providers.Changed -= OnProvidersChanged;
        }

        Cancel();
    }

    /// <summary>Ends the session, if any, and cancels its request. P8-17 adds the cancel of the quick suggest timer at the top.</summary>
    public void Cancel(bool retrigger = false)
    {
        if (_triggerState is not null)
        {
            _requestToken?.Cancel();
            _requestToken = null;
            _triggerState = null;
            _completionModel = null;
            _lineContext = null;
            DidCancel?.Invoke(this, new SuggestCancelEventArgs(retrigger));
        }
    }

    /// <summary>Starts a session: cancels the previous one and asks the providers.</summary>
    public void Trigger(SuggestTriggerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (_viewModel is null || _providers is null)
        {
            return;
        }

        TextModel model = _context.Model;
        Position position = _context.Selection.Position;
        var ctx = new LineContext(model, position, options);

        // Cancel previous requests, change state & update UI
        Cancel(options.Retrigger);
        _triggerState = options;
        DidTrigger?.Invoke(this, new SuggestTriggerEventArgs(options.Auto, options.Shy, position));

        // Capture context when request was sent
        _lineContext = ctx;

        CompletionContext suggestCtx = options.TriggerCharacter is { Length: > 0 } c
            ? new CompletionContext(CompletionTriggerKind.TriggerCharacter, c)
            : new CompletionContext(options.TriggerKind ?? CompletionTriggerKind.Invoke);

        var source = new CancellationTokenSource();
        _requestToken = source;
        CancellationToken token = source.Token;
        Task<IReadOnlyList<SuggestItem>> task = SuggestItems.ProvideAsync(
            _providers,
            model,
            _context.GetLanguageId(),
            position,
            new SuggestItemOptions(true, options.ProviderFilter, options.ProviderItemsToReuse),
            suggestCtx,
            token);

        UiContinuation.Then(
            task,
            _dispatcher,
            items =>
            {
                if (_triggerState is null)
                {
                    return;
                }

                var ctx2 = new LineContext(model, _context.Selection.Position, options);
                var fuzzy = new FuzzyScoreOptions(FirstMatchCanBeWeak: !_modelOptions.MatchOnWordStartOnly, BoostFullMatch: true);
                _completionModel = new CompletionModel(
                    items,
                    _lineContext!.Column,
                    new CompletionLineContext(ctx2.LeadingLineContent, ctx2.Column - _lineContext.Column),
                    _modelOptions,
                    fuzzy);
                OnNewContext(ctx2);
            },
            _ => Cancel(),
            token);
    }

    private void OnViewModelEvent(object? sender, OutgoingViewModelEvent e)
    {
        if (e is CursorStateChangedEvent cursorEvent)
        {
            OnCursorChange(cursorEvent);
        }
    }

    // Difference from Monaco: its editor-level onDidChangeModelContent is raised after the cursor has moved, but TextModel.ContentChanged is
    // raised while the edit is applied, when the cursor still has its old position. The refilter waits for the cursor event that follows an edit
    // made through the cursor (typing, deleting); the dispatcher runs it when no cursor event comes (an edit that does not move the cursor).
    private void OnContentChanged(object? sender, ModelContentChangedEventArgs e)
    {
        if (_triggerState is not null && !_refilterPending)
        {
            _refilterPending = true;
            _dispatcher.Post(RunPendingRefilter);
        }
    }

    private void RunPendingRefilter()
    {
        if (_refilterPending)
        {
            _refilterPending = false;
            if (_triggerState is not null)
            {
                RefilterCompletionItems();
            }
        }
    }

    private void OnLanguageChanged(object? sender, ModelLanguageChangedEventArgs e) => Cancel();

    private void OnProvidersChanged(object? sender, int e) => UpdateActiveSuggestSession();

    private void UpdateActiveSuggestSession()
    {
        if (_triggerState is not null)
        {
            if (_providers is null || !_providers.Has(_context.GetLanguageId()))
            {
                Cancel();
            }
            else
            {
                Trigger(new SuggestTriggerOptions(Auto: _triggerState.Auto, Retrigger: true));
            }
        }
    }

    private void OnCursorChange(CursorStateChangedEvent e)
    {
        RunPendingRefilter();
        Selection prevSelection = _currentSelection;
        _currentSelection = e.Selections[0];

        if (!_currentSelection.IsEmpty
            || (e.Reason is not CursorChangeReason.NotSet and not CursorChangeReason.Explicit)
            || (e.Source is not "keyboard" and not "deleteLeft"))
        {
            // Early exit if nothing needs to be done!
            Cancel();
            return;
        }

        if (_triggerState is null && e.Reason == CursorChangeReason.NotSet)
        {
            if (prevSelection.Range.Contains(_currentSelection.Range) || prevSelection.Range.End.IsBeforeOrEqual(_currentSelection.Position))
            {
                // cursor did move RIGHT due to typing -> trigger quick suggest
                TriggerQuickSuggestIfTyping();
            }
        }
        else if (_triggerState is not null && e.Reason == CursorChangeReason.Explicit)
        {
            // suggest is active and something like cursor keys are used to move the cursor: refilter at the new position
            RefilterCompletionItems();
        }
    }

    private static void TriggerQuickSuggestIfTyping()
    {
        // P8-17: DoTriggerQuickSuggest
    }

    private void RefilterCompletionItems()
    {
        SuggestTriggerOptions state = _triggerState!;
        Guard.Assert(_triggerState is not null);
        var ctx = new LineContext(_context.Model, _context.Selection.Position, state with { Refilter = true });
        OnNewContext(ctx);
    }
}

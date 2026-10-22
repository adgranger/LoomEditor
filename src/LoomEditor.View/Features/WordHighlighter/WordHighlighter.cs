// Ported from microsoft/vscode: src/vs/editor/contrib/wordHighlighter/browser/wordHighlighter.ts (WordHighlighter)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Events;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.WordHighlighter;

/// <summary>Highlights every occurrence of the word under the cursor (Monaco: <c>WordHighlighter</c>).</summary>
/// <remarks>
/// <para>
/// Single model, textual provider only (the semantic providers come with the language features of phase 8): the static
/// <c>query</c> and <c>storedDecorationIDs</c> of Monaco, shared between editors by URI, are instance fields, and the request is
/// synchronous, so there is no worker request to cancel. Not ported: <c>multiFile</c>, notebooks, the other models,
/// <c>occurrencesHighlightFromAllProviders</c>, the <c>hasWordHighlights</c> context key, <c>OccurenceAtPositionRequest.isValid</c>
/// (nothing calls it) and <c>onDidChangeModel</c> (the model of an editor never changes). Behavior differences: the cursor
/// position change is the outgoing <see cref="CursorStateChangedEvent"/> when the primary position changes, and
/// <see cref="Dispose"/> does not run a new query.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class WordHighlighter : IDisposable
{
    private static readonly TimeSpan RunDelay = TimeSpan.FromMilliseconds(50);

    private readonly ViewModel _viewModel;
    private readonly IEditorScheduler _scheduler;
    private readonly EditorDecorationsCollection _decorations;
    private readonly Delayer _runDelayer;
    private EditorWordHighlightOptions _options;
    private int _workerRequestTokenId;
    private bool _workerRequestActive;
    private bool _workerRequestCompleted;
    private List<DocumentHighlight> _workerRequestValue = [];
    private IDisposable? _renderDecorationsTimer;
    private string[]? _storedDecorationIds;
    private Selection? _query;
    private bool _disposed;

    // Monaco: _ignorePositionChangeEvent, true while the navigation moves the cursor.
    private bool _ignorePositionChangeEvent;

    public WordHighlighter(ViewModel viewModel, IEditorScheduler scheduler, EditorWordHighlightOptions options)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(options);

        _viewModel = viewModel;
        _scheduler = scheduler;
        _options = options;
        _runDelayer = new Delayer(scheduler, RunDelay);
        _decorations = new EditorDecorationsCollection(viewModel.Model, viewModel.EditorId);
        viewModel.EventDispatcher.OnEvent += OnViewModelEvent;
        viewModel.Model.ContentChanged += OnModelContentChanged;
    }

    /// <summary>Monaco: <c>hasDecorations</c>.</summary>
    public bool HasDecorations => _decorations.Count > 0;

    /// <summary>The options; changing <c>occurrencesHighlight</c> removes the highlights (Monaco: <c>onDidChangeConfiguration</c>).</summary>
    public EditorWordHighlightOptions Options
    {
        get => _options;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            bool changed = _options.OccurrencesHighlight != value.OccurrencesHighlight;
            _options = value;
            if (changed)
            {
                StopAll();
            }
        }
    }

    private bool IsOff => _options.OccurrencesHighlight == OccurrencesHighlight.Off;

    /// <summary>Monaco: <c>restore</c>: highlights again, rendering after <paramref name="delay"/>.</summary>
    public void Restore(TimeSpan delay)
    {
        if (IsOff)
        {
            return;
        }

        _runDelayer.Cancel();
        _runDelayer.Trigger(() => Run(delay));
    }

    /// <summary>Monaco: <c>trigger</c>: highlights at once.</summary>
    public void Trigger()
    {
        _runDelayer.Cancel();
        Run(TimeSpan.Zero);
    }

    /// <summary>Monaco: <c>stop</c>.</summary>
    public void Stop()
    {
        if (IsOff)
        {
            return;
        }

        StopAll();
    }

    /// <summary>Monaco: <c>moveNext</c>: goes to the occurrence after the one at the cursor, wrapping around.</summary>
    public void MoveNext() => MoveBy(1);

    /// <summary>Monaco: <c>moveBack</c>: goes to the occurrence before the one at the cursor, wrapping around.</summary>
    public void MoveBack() => MoveBy(-1);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _viewModel.EventDispatcher.OnEvent -= OnViewModelEvent;
        _viewModel.Model.ContentChanged -= OnModelContentChanged;
        _runDelayer.Dispose();
        RemoveDecorations();
        StopRequest();
    }

    // Monaco: moveNext and moveBack; the screen reader alert is not ported.
    private void MoveBy(int step)
    {
        List<Range> highlights = [.. _decorations.GetRanges()];
        highlights.Sort(Range.CompareUsingStarts);
        if (highlights.Count == 0)
        {
            return;
        }

        Position position = _viewModel.GetSelection().Position;
        int index = highlights.FindIndex(range => range.Contains(position));
        int newIndex = (((index + step) % highlights.Count) + highlights.Count) % highlights.Count;
        Range dest = highlights[newIndex];
        try
        {
            _ignorePositionChangeEvent = true;
            _viewModel.SetSelections("api", [Selection.Collapsed(dest.Start)]);
            _viewModel.RevealRange(
                "api",
                true,
                _viewModel.CoordinatesConverter.ConvertModelRangeToViewRange(dest),
                VerticalRevealType.CenterIfOutsideViewport,
                ScrollType.Smooth);
        }
        finally
        {
            _ignorePositionChangeEvent = false;
        }
    }

    private void OnViewModelEvent(object? sender, OutgoingViewModelEvent e)
    {
        switch (e)
        {
            case CursorStateChangedEvent cursorEvent:
                if (_ignorePositionChangeEvent)
                {
                    // We are changing the position => ignore this event
                    return;
                }

                // Monaco: onDidChangeCursorPosition fires when the position of the primary cursor changes.
                if (IsOff
                    || (cursorEvent.OldSelections is { Count: > 0 } old && old[0].Position == cursorEvent.Selections[0].Position))
                {
                    return;
                }

                _runDelayer.Trigger(() => OnPositionChanged(cursorEvent));
                break;
            case FocusChangedEvent { HasFocus: true }:
                if (!IsOff && !_workerRequestActive)
                {
                    _runDelayer.Trigger(() => Run(null));
                }

                break;
            case FocusChangedEvent:
                // Monaco: onDidBlurEditorWidget, with no other focused editor.
                StopAll();
                break;
        }
    }

    private void OnModelContentChanged(object? sender, EventArgs e) => StopAll();

    private void OnPositionChanged(CursorStateChangedEvent e)
    {
        if (IsOff)
        {
            StopAll();
            return;
        }

        // ignore typing & other
        if (e.Source != "api" && e.Reason != CursorChangeReason.Explicit)
        {
            StopAll();
            return;
        }

        Run(null);
    }

    // Monaco: _removeSingleDecorations (with one editor, _removeAllDecorations does the same).
    private void RemoveDecorations()
    {
        if (_storedDecorationIds is not { } ids)
        {
            return;
        }

        _viewModel.Model.ChangeDecorations(_viewModel.EditorId, accessor => accessor.DeltaDecorations(ids, []));
        _storedDecorationIds = null;

        if (_decorations.Count > 0)
        {
            _decorations.Clear();
            _workerRequestActive = false;
        }
    }

    private void StopRequest()
    {
        // Cancel any renderDecorationsTimer
        _renderDecorationsTimer?.Dispose();
        _renderDecorationsTimer = null;

        // Cancel any worker request
        _workerRequestActive = false;

        // Invalidate any worker request callback
        if (!_workerRequestCompleted)
        {
            _workerRequestTokenId++;
            _workerRequestCompleted = true;
        }
    }

    // Monaco: _stopAll.
    private void StopAll()
    {
        RemoveDecorations();
        StopRequest();
    }

    // Monaco: _run, with the delay of the render: `null` is `occurrencesHighlightDelay`.
    private void Run(TimeSpan? delay)
    {
        if (!_viewModel.HasFocus)
        {
            // no previous query, nothing to highlight off of
            if (_query is null)
            {
                StopAll();
                return;
            }
        }
        else
        {
            Range selection = _viewModel.GetSelection().Range;

            // ignore multiline selection
            if (selection.StartLineNumber != selection.EndLineNumber)
            {
                _query = null;
                StopAll();
                return;
            }

            // The selection must be inside a word or surround one word at most
            if (_viewModel.Model.GetWordAtPosition(new Position(selection.StartLineNumber, selection.StartColumn)) is not { } word
                || word.StartColumn > selection.StartColumn
                || word.EndColumn < selection.EndColumn)
            {
                _query = null;
                StopAll();
                return;
            }

            _query = _viewModel.GetSelection();
        }

        // check if the new queried word is contained in the range of a stored decoration
        Position position = _viewModel.GetSelection().Position;
        foreach (Range storedRange in _decorations.GetRanges())
        {
            if (storedRange.Contains(position))
            {
                return;
            }
        }

        // stop all previous actions if new word is highlighted
        StopAll();

        int myRequestId = ++_workerRequestTokenId;
        _workerRequestCompleted = false;
        if (_query is not { } query)
        {
            return;
        }

        // The request is synchronous: it completes here (Monaco: the promise of the worker request).
        _workerRequestActive = true;
        List<DocumentHighlight> data = TextualDocumentHighlightProvider.ProvideDocumentHighlights(_viewModel.Model, query.Position);
        if (myRequestId == _workerRequestTokenId)
        {
            _workerRequestCompleted = true;
            _workerRequestValue = data;
            BeginRenderDecorations(delay ?? TimeSpan.FromMilliseconds(_options.OccurrencesHighlightDelay));
        }
    }

    // Monaco: _beginRenderDecorations; the request completes at once, so only the delay is left to wait.
    private void BeginRenderDecorations(TimeSpan delay)
    {
        if (delay <= TimeSpan.Zero)
        {
            _renderDecorationsTimer = null;
            RenderDecorations();
        }
        else
        {
            _renderDecorationsTimer = _scheduler.Schedule(delay, RenderDecorations);
        }
    }

    private void RenderDecorations()
    {
        _renderDecorationsTimer = null;

        var newDecorations = new List<ModelDeltaDecoration>();
        foreach (DocumentHighlight highlight in _workerRequestValue)
        {
            newDecorations.Add(new ModelDeltaDecoration(highlight.Range, HighlightDecorations.GetHighlightDecorationOptions(highlight.Kind)));
        }

        string[] oldDecorationIds = _storedDecorationIds ?? [];
        _storedDecorationIds = _viewModel.Model.ChangeDecorations(
            _viewModel.EditorId, accessor => accessor.DeltaDecorations(oldDecorationIds, newDecorations));

        if (newDecorations.Count > 0)
        {
            _decorations.Set(newDecorations);
        }

        // clear the worker request when decorations are completed
        _workerRequestActive = false;
    }
}

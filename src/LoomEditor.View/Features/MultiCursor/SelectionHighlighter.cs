// Ported from microsoft/vscode: src/vs/editor/contrib/multicursor/browser/multicursor.ts (SelectionHighlighterState, SelectionHighlighter)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Events;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.MultiCursor;

/// <summary>Highlights the other occurrences of the selected text (Monaco: <c>SelectionHighlighter</c>).</summary>
/// <remarks>
/// Behavior differences: the editor API is the <see cref="ViewModel"/>; <c>onDidChangeModel</c> is not ported (one model per editor);
/// <c>hasSemanticHighlights</c> is always <c>false</c> (no document highlight provider before phase 8); the whitespace-only test
/// is a loop, not a regular expression.
/// Threading: UI thread only.
/// </remarks>
internal sealed class SelectionHighlighter : IDisposable
{
    private static readonly TimeSpan UpdateDelay = TimeSpan.FromMilliseconds(300);

    private sealed class State
    {
        private readonly TextModel _model;
        private readonly string _searchText;
        private readonly bool _matchCase;
        private readonly string? _wordSeparators;
        private readonly int _modelVersionId;
        private List<Range>? _cachedFindMatches;

        public State(TextModel model, string searchText, bool matchCase, string? wordSeparators, State? prevState)
        {
            _model = model;
            _searchText = searchText;
            _matchCase = matchCase;
            _wordSeparators = wordSeparators;
            _modelVersionId = model.VersionId;
            if (prevState is not null
                && ReferenceEquals(_model, prevState._model)
                && _searchText == prevState._searchText
                && _matchCase == prevState._matchCase
                && _wordSeparators == prevState._wordSeparators
                && _modelVersionId == prevState._modelVersionId)
            {
                _cachedFindMatches = prevState._cachedFindMatches;
            }
        }

        public List<Range> FindMatches()
        {
            if (_cachedFindMatches is null)
            {
                _cachedFindMatches = [.. _model.FindMatches(_searchText, true, false, _matchCase, _wordSeparators, false).Select(m => m.Range)];
                _cachedFindMatches.Sort(Range.CompareUsingStarts);
            }

            return _cachedFindMatches;
        }
    }

    private readonly ViewModel _viewModel;
    private readonly MultiCursorSelectionController _multiCursor;
    private readonly FindController _findController;
    private readonly EditorDecorationsCollection _decorations;
    private readonly RunOnceScheduler _updateSoon;
    private MultiCursorOptions _options;
    private State? _state;

    public SelectionHighlighter(
        ViewModel viewModel,
        MultiCursorSelectionController multiCursor,
        FindController findController,
        EditorDecorationsCollection decorations,
        IEditorScheduler scheduler,
        MultiCursorOptions options)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(multiCursor);
        ArgumentNullException.ThrowIfNull(findController);
        ArgumentNullException.ThrowIfNull(decorations);
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(options);

        _viewModel = viewModel;
        _multiCursor = multiCursor;
        _findController = findController;
        _decorations = decorations;
        _options = options;
        _updateSoon = new RunOnceScheduler(scheduler, Update, UpdateDelay);

        multiCursor.OptionsChanged += OnOptionsChanged;
        viewModel.EventDispatcher.OnEvent += OnViewModelEvent;
        viewModel.Model.ContentChanged += OnModelContentChanged;
        findController.State.Changed += OnFindStateChanged;

        _updateSoon.Schedule();
    }

    public void Dispose()
    {
        _multiCursor.OptionsChanged -= OnOptionsChanged;
        _viewModel.EventDispatcher.OnEvent -= OnViewModelEvent;
        _viewModel.Model.ContentChanged -= OnModelContentChanged;
        _findController.State.Changed -= OnFindStateChanged;
        SetState(null);
        _updateSoon.Dispose();
    }

    private void OnOptionsChanged(object? sender, (MultiCursorOptions Old, MultiCursorOptions New) e)
    {
        _options = e.New;
        _updateSoon.Schedule(TimeSpan.Zero);
    }

    private void OnViewModelEvent(object? sender, OutgoingViewModelEvent e)
    {
        if (e is not CursorStateChangedEvent cursorEvent || !_options.SelectionHighlight)
        {
            // Early exit if nothing needs to be done!
            return;
        }

        if (cursorEvent.Selections[0].IsEmpty)
        {
            if (cursorEvent.Reason == CursorChangeReason.Explicit)
            {
                if (_state is not null)
                {
                    // no longer valid
                    SetState(null);
                }

                _updateSoon.Schedule();
            }
            else
            {
                SetState(null);
            }
        }
        else
        {
            Update();
        }
    }

    private void OnModelContentChanged(object? sender, ModelContentChangedEventArgs e)
    {
        if (_options.SelectionHighlight)
        {
            _updateSoon.Schedule();
        }
    }

    private void OnFindStateChanged(object? sender, FindReplaceStateChangedEventArgs e) => Update();

    private void Update() => SetState(CreateState(_state));

    private State? CreateState(State? oldState)
    {
        if (!_options.SelectionHighlight)
        {
            return null;
        }

        if (!_options.SelectionHighlightMultiline)
        {
            Selection s = _viewModel.GetSelection();
            if (s.Range.StartLineNumber != s.Range.EndLineNumber)
            {
                // multiline forbidden for perf reasons
                return null;
            }
        }

        MultiCursorSession? r = _multiCursor.GetSession();
        if (r is null)
        {
            List<Selection> allSelections = _viewModel.GetSelections();
            if (allSelections.Count > 1
                && !MultiCursorSelectionController.ModelRangesContainSameText(_viewModel.Model, allSelections, _findController.State.MatchCase))
            {
                return null;
            }

            r = MultiCursorSession.Create(_viewModel, _options, _findController);
        }

        if (r is null)
        {
            return null;
        }

        if (r.CurrentMatch is not null)
        {
            // This is an empty selection
            // Do not interfere with semantic word highlighting in the no selection case
            return null;
        }

        if (IsWhitespaceOnly(r.SearchText))
        {
            return null;
        }

        if (_options.SelectionHighlightMaxLength > 0 && r.SearchText.Length > _options.SelectionHighlightMaxLength)
        {
            // very long selection
            return null;
        }

        // TODO (Monaco): better handling of this case
        FindReplaceState findState = _findController.State;
        bool caseSensitive = findState.MatchCase;

        // Return early if the find widget shows the exact same matches
        if (findState.IsRevealed)
        {
            string findStateSearchString = caseSensitive ? findState.SearchString : findState.SearchString.ToLowerInvariant();
            string mySearchString = caseSensitive ? r.SearchText : r.SearchText.ToLowerInvariant();

            if (findStateSearchString == mySearchString && r.MatchCase == findState.MatchCase && r.WholeWord == findState.WholeWord && !findState.IsRegex)
            {
                return null;
            }
        }

        return new State(_viewModel.Model, r.SearchText, r.MatchCase, r.WholeWord ? _viewModel.CursorConfig.WordSeparators : null, oldState);
    }

    // Monaco: /^[ \t]+$/.test(searchText)
    private static bool IsWhitespaceOnly(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        foreach (char c in text)
        {
            if (c != ' ' && c != '\t')
            {
                return false;
            }
        }

        return true;
    }

    private void SetState(State? newState)
    {
        _state = newState;

        if (_state is null)
        {
            _decorations.Clear();
            return;
        }

        TextModel model = _viewModel.Model;
        if (model.IsTooLargeForTokenization)
        {
            // the file is too large, so searching word under cursor in the whole document would be blocking the UI.
            return;
        }

        List<Range> allMatches = _state.FindMatches();

        List<Selection> selections = _viewModel.GetSelections();
        selections.Sort((a, b) => Range.CompareUsingStarts(a.Range, b.Range));

        // do not overlap with selection (issue #64 and #512)
        List<Range> matches = [];
        for (int i = 0, j = 0; i < allMatches.Count;)
        {
            Range match = allMatches[i];

            if (j >= selections.Count)
            {
                // finished all editor selections
                matches.Add(match);
                i++;
            }
            else
            {
                Range selection = selections[j].Range;
                int cmp = Range.CompareUsingStarts(match, selection);
                if (cmp < 0)
                {
                    // match is before sel
                    if (selection.IsEmpty || !match.Intersects(selection))
                    {
                        matches.Add(match);
                    }

                    i++;
                }
                else if (cmp > 0)
                {
                    // sel is before match
                    j++;
                }
                else
                {
                    // sel is equal to match
                    i++;
                    j++;
                }
            }
        }

        ModelDecorationOptions options = SelectionHighlightDecorations.GetSelectionHighlightDecorationOptions(hasSemanticHighlights: false);
        _decorations.Set([.. matches.Select(r => new ModelDeltaDecoration(r, options))]);
    }
}

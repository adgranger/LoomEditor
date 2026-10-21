// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/findModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Search;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Events;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.Find;

/// <summary>
/// Searches the model when the find state changes, decorates the matches, keeps "n of m" up to date and moves between matches
/// (Monaco: <c>FindModelBoundToEditorModel</c>).
/// </summary>
/// <remarks>
/// Behavior differences: the editor API is the <see cref="ViewModel"/>; <c>RunOnceScheduler</c> and <c>TimeoutTimer</c> run on an
/// <see cref="IEditorScheduler"/>; the model never unfolds regions (the folding feature does).
/// Threading: UI thread only.
/// </remarks>
internal sealed partial class FindModelBoundToEditorModel : IDisposable
{
    private const string Source = "api";

    private readonly ViewModel _viewModel;
    private readonly TextModel _model;
    private readonly FindReplaceState _state;
    private readonly EditorFindOptions _options;
    private readonly FindDecorations _decorations;
    private readonly RunOnceScheduler _updateDecorationsScheduler;
    private readonly RunOnceScheduler _startSearchingScheduler;
    private FindReplaceStateChangedEventArgs? _pendingSearch;
    private bool _ignoreModelContentChanged;
    private bool _isDisposed;

    public FindModelBoundToEditorModel(ViewModel viewModel, FindReplaceState state, IEditorScheduler scheduler, EditorFindOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(scheduler);

        _viewModel = viewModel;
        _model = viewModel.Model;
        _state = state;
        _options = options ?? EditorFindOptions.Default;
        _decorations = new FindDecorations(viewModel);

        _updateDecorationsScheduler = new RunOnceScheduler(scheduler, () => Research(false), TimeSpan.FromMilliseconds(100));
        _startSearchingScheduler = new RunOnceScheduler(scheduler, RunPendingSearch, FindConstants.ResearchDelay);

        _viewModel.EventDispatcher.OnEvent += OnViewModelEvent;
        _model.ContentChanged += OnModelContentChanged;
        _state.Changed += OnStateChanged;

        Research(false, _state.SearchScope);
    }

    public void Dispose()
    {
        _isDisposed = true;
        _startSearchingScheduler.Dispose();
        _updateDecorationsScheduler.Dispose();
        _viewModel.EventDispatcher.OnEvent -= OnViewModelEvent;
        _model.ContentChanged -= OnModelContentChanged;
        _state.Changed -= OnStateChanged;
        _decorations.Dispose();
    }

    private void OnViewModelEvent(object? sender, OutgoingViewModelEvent e)
    {
        if (e is CursorStateChangedEvent { Reason: CursorChangeReason.Explicit or CursorChangeReason.Undo or CursorChangeReason.Redo })
        {
            _decorations.SetStartPosition(_viewModel.GetPosition());
        }
    }

    private void OnModelContentChanged(object? sender, ModelContentChangedEventArgs e)
    {
        if (_ignoreModelContentChanged)
        {
            return;
        }

        if (e.IsFlush)
        {
            // a model.SetValue() was called
            _decorations.Reset();
        }

        _decorations.SetStartPosition(_viewModel.GetPosition());
        _updateDecorationsScheduler.Schedule();
    }

    private void OnStateChanged(object? sender, FindReplaceStateChangedEventArgs e)
    {
        if (_isDisposed)
        {
            // The find model is disposed during a find state changed event
            return;
        }

        if (!(e.SearchString || e.IsReplaceRevealed || e.IsRegex || e.WholeWord || e.MatchCase || e.SearchScope))
        {
            return;
        }

        if (_model.IsTooLargeForSyncing)
        {
            // Monaco: TimeoutTimer.cancel() then setIfNotSet(): the last event wins, the search runs after the delay.
            _startSearchingScheduler.Cancel();
            _pendingSearch = e;
            _startSearchingScheduler.Schedule();
        }
        else
        {
            Research(e);
        }
    }

    private void RunPendingSearch()
    {
        if (_pendingSearch is { } e)
        {
            _pendingSearch = null;
            Research(e);
        }
    }

    private void Research(FindReplaceStateChangedEventArgs e)
    {
        if (e.SearchScope)
        {
            Research(e.MoveCursor, _state.SearchScope);
        }
        else
        {
            Research(e.MoveCursor);
        }
    }

    private static Range GetSearchRange(TextModel model, Range? findScope) => findScope ?? model.FullModelRange;

    private void Research(bool moveCursor) => Research(moveCursor, _decorations.GetFindScopes());

    private void Research(bool moveCursor, IReadOnlyList<Range>? newFindScopes)
    {
        List<Range>? findScopes = newFindScopes?.Select(scope =>
        {
            if (scope.StartLineNumber == scope.EndLineNumber)
            {
                return scope;
            }

            var endLineNumber = scope.EndColumn == 1 ? scope.EndLineNumber - 1 : scope.EndLineNumber;
            return new Range(scope.StartLineNumber, 1, endLineNumber, _model.GetLineMaxColumn(endLineNumber));
        }).ToList();

        List<FindMatch> findMatches = FindMatches(findScopes, false, FindConstants.MatchesLimit);
        _decorations.Set(findMatches, findScopes);

        Range editorSelection = _viewModel.GetSelection().Range;
        var currentMatchesPosition = _decorations.GetCurrentMatchesPosition(editorSelection);
        if (currentMatchesPosition == 0 && findMatches.Count > 0)
        {
            // current selection is not on top of a match
            // try to find its nearest result from the top of the document
            var matchAfterSelection = FirstIndexAtOrAfter(findMatches, editorSelection);
            currentMatchesPosition = matchAfterSelection > 0 ? matchAfterSelection : currentMatchesPosition;
        }

        _state.ChangeMatchInfo(currentMatchesPosition, _decorations.GetCount(), null, false);

        if (moveCursor && _options.CursorMoveOnType)
        {
            MoveToNextMatch(_decorations.GetStartPosition());
        }
    }

    // Monaco: findFirstIdxMonotonousOrArrLen(ranges, range => compareRangesUsingStarts(range, selection) >= 0)
    private static int FirstIndexAtOrAfter(List<FindMatch> findMatches, Range selection)
    {
        int low = 0, high = findMatches.Count;
        while (low < high)
        {
            var mid = low + ((high - low) / 2);
            if (Range.CompareUsingStarts(findMatches[mid].Range, selection) >= 0)
            {
                high = mid;
            }
            else
            {
                low = mid + 1;
            }
        }

        return low;
    }

    private bool HasMatches() => _state.MatchesCount > 0;

    private bool CannotFind()
    {
        if (HasMatches())
        {
            return false;
        }

        if (_decorations.GetFindScope() is { } findScope)
        {
            // Reveal the selection so user is reminded that 'selection find' is on.
            RevealRangeInCenterIfOutsideViewport(findScope);
        }

        return true;
    }

    private void SetCurrentFindMatch(Range match)
    {
        var matchesPosition = _decorations.SetCurrentFindMatch(match);
        _state.ChangeMatchInfo(matchesPosition, _decorations.GetCount(), match, true);

        _viewModel.SetSelections(Source, [Selection.FromRange(match, SelectionDirection.LeftToRight)]);
        RevealRangeInCenterIfOutsideViewport(match);
    }

    private void RevealRangeInCenterIfOutsideViewport(Range range) =>
        _viewModel.RevealRange(
            Source,
            true,
            _viewModel.CoordinatesConverter.ConvertModelRangeToViewRange(range),
            VerticalRevealType.CenterIfOutsideViewport,
            ScrollType.Smooth);

    private List<FindMatch> FindMatches(IReadOnlyList<Range>? findScopes, bool captureMatches, int limitResultCount)
    {
        List<Range> searchRanges = findScopes is null
            ? [GetSearchRange(_model, null)]
            : [.. findScopes.Select(scope => GetSearchRange(_model, scope))];

        return _model.FindMatches(_state.SearchString, searchRanges, _state.IsRegex, _state.MatchCase, WordSeparatorsOrNull(), captureMatches, limitResultCount);
    }

    private string? WordSeparatorsOrNull() => _state.WholeWord ? _viewModel.CursorConfig.WordSeparators : null;

    private ReplacePattern GetReplacePattern() =>
        _state.IsRegex ? ReplacePattern.Parse(_state.ReplaceString) : ReplacePattern.FromStaticValue(_state.ReplaceString);

    private void ExecuteEditorCommand(string source, IEditCommand command)
    {
        try
        {
            _ignoreModelContentChanged = true;
            _model.PushStackElement();
            _viewModel.ExecuteCommand(command, source);
            _model.PushStackElement();
        }
        finally
        {
            _ignoreModelContentChanged = false;
        }
    }
}

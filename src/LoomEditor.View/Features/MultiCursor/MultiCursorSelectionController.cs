// Ported from microsoft/vscode: src/vs/editor/contrib/multicursor/browser/multicursor.ts (MultiCursorSelectionController, MultiCursorSelectionControllerAction)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Search;
using LoomEditor.Core.Text;
using LoomEditor.View.Events;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.MultiCursor;

/// <summary>Ctrl+D and its siblings: adds or moves cursors to the next or previous occurrence of the selection, or selects all of them.</summary>
/// <remarks>
/// Behavior differences: the editor API is the <see cref="ViewModel"/>; the editor events are the outgoing view model events
/// and <see cref="OptionsChanged"/>; <c>announceCursorChange</c> and <c>onDidChangeModel</c> are not ported.
/// Threading: UI thread only.
/// </remarks>
internal sealed class MultiCursorSelectionController : IDisposable
{
    private const string Source = "api";

    private readonly ViewModel _viewModel;
    private MultiCursorOptions _options;
    private bool _ignoreSelectionChange;
    private MultiCursorSession? _session;
    private SessionSubscriptions? _subscriptions;

    public MultiCursorSelectionController(ViewModel viewModel, MultiCursorOptions options)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(options);

        _viewModel = viewModel;
        _options = options;
    }

    /// <summary>Raised with the old and the new options when <c>selectedTextMatchMode</c> changes (Monaco: <c>onDidChangeConfiguration</c>).</summary>
    public event EventHandler<(MultiCursorOptions Old, MultiCursorOptions New)>? OptionsChanged;

    /// <summary>The options this controller reads.</summary>
    public MultiCursorOptions Options => _options;

    /// <summary>Changes the options; a different <c>selectedTextMatchMode</c> raises <see cref="OptionsChanged"/>.</summary>
    public void SetOptions(MultiCursorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        MultiCursorOptions old = _options;
        _options = options;
        if (old.SelectedTextMatchMode != options.SelectedTextMatchMode)
        {
            OptionsChanged?.Invoke(this, (old, options));
        }
    }

    public void Dispose() => EndSession();

    /// <summary>Runs <paramref name="run"/> with the find controller, or with a temporary one when there is none (Monaco: <c>MultiCursorSelectionControllerAction.run</c>).</summary>
    public void Run(FindController? findController, IEditorScheduler scheduler, Action<FindController> run)
    {
        if (findController is not null)
        {
            run(findController);
            return;
        }

        using var temporary = new FindController(_viewModel, scheduler);
        run(temporary);
    }

    /// <summary>The running session, if any (Monaco: <c>getSession</c>).</summary>
    public MultiCursorSession? GetSession() => _session;

    private sealed class SessionSubscriptions(MultiCursorSelectionController owner, MultiCursorSession session, FindController findController) : IDisposable
    {
        private readonly bool _endOnFocus = owner._options.SelectedTextMatchMode != SelectedTextMatchMode.FindOptions;
        private bool _disposed;

        public void Subscribe()
        {
            owner._viewModel.EventDispatcher.OnEvent += OnViewModelEvent;
            owner.OptionsChanged += OnOptionsChanged;
            findController.State.Changed += OnFindStateChanged;
        }

        private void OnViewModelEvent(object? sender, OutgoingViewModelEvent e)
        {
            if (_disposed)
            {
                return;
            }

            if (e is CursorStateChangedEvent)
            {
                if (!owner._ignoreSelectionChange)
                {
                    owner.EndSession();
                }
            }
            else if (e is FocusChangedEvent focus && (!focus.HasFocus || _endOnFocus))
            {
                owner.EndSession();
            }
        }

        private void OnOptionsChanged(object? sender, (MultiCursorOptions Old, MultiCursorOptions New) e)
        {
            if (!_disposed)
            {
                owner.EndSession();
            }
        }

        private void OnFindStateChanged(object? sender, FindReplaceStateChangedEventArgs e)
        {
            if (!_disposed && session.UsesFindOptions && (e.MatchCase || e.WholeWord))
            {
                owner.EndSession();
            }
        }

        public void Dispose()
        {
            _disposed = true;
            owner._viewModel.EventDispatcher.OnEvent -= OnViewModelEvent;
            owner.OptionsChanged -= OnOptionsChanged;
            findController.State.Changed -= OnFindStateChanged;
        }
    }

    private void BeginSessionIfNeeded(FindController findController)
    {
        if (_session is not null)
        {
            return;
        }

        // Create a new session
        MultiCursorSession? session = MultiCursorSession.Create(_viewModel, _options, findController);
        if (session is null)
        {
            return;
        }

        _session = session;

        NewFindReplaceState newState = new() { SearchString = session.SearchText };
        if (session.IsDisconnectedFromFindController)
        {
            newState = newState with
            {
                WholeWordOverride = FindOptionOverride.True,
                MatchCaseOverride = FindOptionOverride.True,
                IsRegexOverride = FindOptionOverride.False,
            };
        }

        findController.State.Change(newState, false);

        _subscriptions = new SessionSubscriptions(this, session, findController);
        _subscriptions.Subscribe();
    }

    private void EndSession()
    {
        _subscriptions?.Dispose();
        _subscriptions = null;
        if (_session is { IsDisconnectedFromFindController: true } session)
        {
            session.FindController.State.Change(
                new NewFindReplaceState
                {
                    WholeWordOverride = FindOptionOverride.NotSet,
                    MatchCaseOverride = FindOptionOverride.NotSet,
                    IsRegexOverride = FindOptionOverride.NotSet,
                },
                false);
        }

        _session = null;
    }

    private void SetSelections(IReadOnlyList<Selection> selections)
    {
        _ignoreSelectionChange = true;
        try
        {
            _viewModel.SetSelections(Source, selections);
        }
        finally
        {
            _ignoreSelectionChange = false;
        }
    }

    private Selection ExpandEmptyToWord(Selection selection)
    {
        if (!selection.IsEmpty)
        {
            return selection;
        }

        WordAtPosition? word = MultiCursorSession.GetConfiguredWordAtPosition(_viewModel, selection.Range.Start);
        if (word is not { } w)
        {
            return selection;
        }

        return new Selection(selection.Range.StartLineNumber, w.StartColumn, selection.Range.StartLineNumber, w.EndColumn);
    }

    private void ApplySessionResult(MultiCursorSessionResult? result)
    {
        if (result is null)
        {
            return;
        }

        SetSelections(result.Selections);
        _viewModel.RevealRange(
            Source,
            true,
            _viewModel.CoordinatesConverter.ConvertModelRangeToViewRange(result.RevealRange),
            VerticalRevealType.CenterIfOutsideViewport,
            result.RevealScrollType);
    }

    public void AddSelectionToNextFindMatch(FindController findController)
    {
        if (_session is null)
        {
            // If there are multiple cursors, handle the case where they do not all select the same text.
            List<Selection> allSelections = _viewModel.GetSelections();
            if (allSelections.Count > 1)
            {
                bool matchCase = MultiCursorSession.IsFindWidgetSearch(_viewModel, findController)
                    ? findController.State.MatchCase
                    : MultiCursorSession.GetSelectionSearchOptions(_options, findController).MatchCase;
                if (!ModelRangesContainSameText(_viewModel.Model, allSelections, matchCase))
                {
                    _viewModel.SetSelections(Source, [.. allSelections.Select(ExpandEmptyToWord)]);
                    return;
                }
            }
        }

        BeginSessionIfNeeded(findController);
        if (_session is not null)
        {
            ApplySessionResult(_session.AddSelectionToNextFindMatch());
        }
    }

    public void AddSelectionToPreviousFindMatch(FindController findController)
    {
        BeginSessionIfNeeded(findController);
        if (_session is not null)
        {
            ApplySessionResult(_session.AddSelectionToPreviousFindMatch());
        }
    }

    public void MoveSelectionToNextFindMatch(FindController findController)
    {
        BeginSessionIfNeeded(findController);
        if (_session is not null)
        {
            ApplySessionResult(_session.MoveSelectionToNextFindMatch());
        }
    }

    public void MoveSelectionToPreviousFindMatch(FindController findController)
    {
        BeginSessionIfNeeded(findController);
        if (_session is not null)
        {
            ApplySessionResult(_session.MoveSelectionToPreviousFindMatch());
        }
    }

    public void SelectAll(FindController findController)
    {
        List<FindMatch> matches;

        FindReplaceState findState = findController.State;
        TextModel model = _viewModel.Model;
        string? wordSeparators = findState.WholeWord ? _viewModel.CursorConfig.WordSeparators : null;

        // Special case: find widget owns entirely what we search for if: focus is not in the editor, the widget is visible,
        // the search string is non-empty and we're searching for a regex.
        bool allowFindRegex = _options.SelectedTextMatchMode == SelectedTextMatchMode.FindOptions || !_viewModel.HasFocus;
        if (allowFindRegex && findState.IsRevealed && findState.SearchString.Length > 0 && findState.IsRegex)
        {
            matches = findState.SearchScope is { } scope
                ? model.FindMatches(findState.SearchString, scope, findState.IsRegex, findState.MatchCase, wordSeparators, false, TextModel.MaxSafeSmallInteger)
                : model.FindMatches(findState.SearchString, true, findState.IsRegex, findState.MatchCase, wordSeparators, false, TextModel.MaxSafeSmallInteger);
        }
        else
        {
            BeginSessionIfNeeded(findController);
            if (_session is null)
            {
                return;
            }

            matches = _session.SelectAll(findState.SearchScope);
        }

        if (matches.Count > 0)
        {
            Selection editorSelection = _viewModel.GetSelection();
            // Have the primary cursor remain the one where the action was invoked
            for (int i = 0; i < matches.Count; i++)
            {
                FindMatch match = matches[i];
                if (match.Range.Intersect(editorSelection.Range) is not null)
                {
                    // bingo!
                    matches[i] = matches[0];
                    matches[0] = match;
                    break;
                }
            }

            SetSelections([.. matches.Select(m => new Selection(m.Range.StartLineNumber, m.Range.StartColumn, m.Range.EndLineNumber, m.Range.EndColumn))]);
        }
    }

    /// <summary>Selects <paramref name="selections"/> without ending the session (Monaco: <c>selectAllUsingSelections</c>).</summary>
    public void SelectAllUsingSelections(IReadOnlyList<Selection> selections)
    {
        if (selections.Count > 0)
        {
            SetSelections(selections);
        }
    }

    internal static bool ModelRangesContainSameText(TextModel model, List<Selection> ranges, bool matchCase)
    {
        string selectedText = GetValueInRange(model, ranges[0].Range, !matchCase);
        for (int i = 1; i < ranges.Count; i++)
        {
            Range range = ranges[i].Range;
            if (range.IsEmpty)
            {
                return false;
            }

            if (selectedText != GetValueInRange(model, range, !matchCase))
            {
                return false;
            }
        }

        return true;
    }

    private static string GetValueInRange(TextModel model, Range range, bool toLowerCase)
    {
        string text = model.GetValueInRange(range);
        return toLowerCase ? text.ToLowerInvariant() : text;
    }
}

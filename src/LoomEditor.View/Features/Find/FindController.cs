// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/findController.ts (CommonFindController, FindController)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Accessibility;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.Find;

/// <summary>Owns the find state of one editor and the find model, which lives while the find widget is revealed.</summary>
/// <remarks>
/// Behavior differences: <c>Start</c> is synchronous (its only <c>await</c> read the macOS find clipboard); no storage of the
/// toggles, no find clipboard, no search history, no "file too large" notification; the widget is reached through
/// <see cref="IFindWidget"/> (<c>null</c> without UI).
/// Threading: UI thread only.
/// </remarks>
internal sealed class FindController : IDisposable
{
    private readonly IEditorScheduler _scheduler;
    private FindModelBoundToEditorModel? _model;

    public FindController(ViewModel viewModel, IEditorScheduler scheduler, EditorFindOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(scheduler);

        ViewModel = viewModel;
        _scheduler = scheduler;
        Options = options ?? EditorFindOptions.Default;
        State.Changed += OnStateChanged;
    }

    /// <summary>The editor of this controller (Monaco: <c>editor</c>).</summary>
    public ViewModel ViewModel { get; }

    /// <summary>The <c>editor.find</c> options.</summary>
    public EditorFindOptions Options { get; }

    /// <summary>The state every find part shares.</summary>
    public FindReplaceState State { get; } = new();

    /// <summary>Where the find results are announced (Monaco: <c>alert</c> in the find widget).</summary>
    internal IEditorAnnouncer Announcer { get; set; } = NullEditorAnnouncer.Instance;

    /// <summary>The find widget, or <c>null</c> without UI.</summary>
    public IFindWidget? Widget { get; set; }

    /// <summary>Whether the find input has the focus.</summary>
    public bool IsFindInputFocused => Widget?.IsFindInputFocused ?? false;

    /// <summary>Whether the replace input was the last focused input of the widget.</summary>
    public bool WasReplaceInputLastFocused => Widget?.WasReplaceInputLastFocused ?? false;

    public void Dispose()
    {
        State.Changed -= OnStateChanged;
        DisposeModel();
    }

    private void DisposeModel()
    {
        _model?.Dispose();
        _model = null;
    }

    private void OnStateChanged(object? sender, FindReplaceStateChangedEventArgs e)
    {
        if (e.IsRevealed && !State.IsRevealed)
        {
            DisposeModel();
        }

        // Monaco announces from the widget, which only exists once revealed.
        if ((e.SearchString || e.MatchesCount || e.MatchesPosition) && State.IsRevealed)
        {
            Announcer.Announce(AccessibilityMessages.FindResult(
                FindMatchesLabel.Format(State.MatchesPosition, State.MatchesCount), State.CurrentMatch, State.SearchString, ViewModel.Model));
        }
    }

    /// <summary>Focuses the last focused element of the find widget.</summary>
    public void FocusLastElement() => Widget?.FocusLastElement();

    /// <summary>Hides the find widget, clears the search scope and focuses the editor.</summary>
    public void CloseFindWidget()
    {
        State.Change(new NewFindReplaceState { IsRevealed = false, SearchScope = null }, false);
        Widget?.FocusEditor();
    }

    public void ToggleCaseSensitive()
    {
        State.Change(new NewFindReplaceState { MatchCase = !State.MatchCase }, false);
        HighlightFindOptionsWhenHidden();
    }

    public void ToggleWholeWords()
    {
        State.Change(new NewFindReplaceState { WholeWord = !State.WholeWord }, false);
        HighlightFindOptionsWhenHidden();
    }

    public void ToggleRegex()
    {
        State.Change(new NewFindReplaceState { IsRegex = !State.IsRegex }, false);
        HighlightFindOptionsWhenHidden();
    }

    public void TogglePreserveCase()
    {
        State.Change(new NewFindReplaceState { PreserveCase = !State.PreserveCase }, false);
        HighlightFindOptionsWhenHidden();
    }

    private void HighlightFindOptionsWhenHidden()
    {
        if (!State.IsRevealed)
        {
            HighlightFindOptions();
        }
    }

    /// <summary>Flashes the find toggles.</summary>
    public void HighlightFindOptions(bool ignoreWhenVisible = false) =>
        Widget?.HighlightFindOptions(State.IsRevealed && !ignoreWhenVisible);

    /// <summary>Turns "find in selection" on (with the non-empty selections) or off.</summary>
    public void ToggleSearchScope()
    {
        if (State.SearchScope is not null)
        {
            State.Change(new NewFindReplaceState { SearchScope = null }, true);
            return;
        }

        List<Range> scopes = [];
        foreach (Selection selection in ViewModel.GetSelections())
        {
            Range range = selection.Range;
            if (range.EndColumn == 1 && range.EndLineNumber > range.StartLineNumber)
            {
                range = new Range(range.Start, new Position(range.EndLineNumber - 1, ViewModel.Model.GetLineMaxColumn(range.EndLineNumber - 1)));
            }

            if (!range.IsEmpty)
            {
                scopes.Add(range);
            }
        }

        if (scopes.Count > 0)
        {
            State.Change(new NewFindReplaceState { SearchScope = scopes }, true);
        }
    }

    /// <summary>Sets the search string, escaping it in regular expression mode.</summary>
    public void SetSearchString(string searchString)
    {
        if (State.IsRegex)
        {
            searchString = Strings.EscapeRegExpCharacters(searchString);
        }

        State.Change(new NewFindReplaceState { SearchString = searchString }, false);
    }

    /// <summary>Reveals the find widget, seeds the search string and creates the find model.</summary>
    public void Start(FindStartOptions opts, NewFindReplaceState? newState = null)
    {
        DisposeModel();

        Selection selection = ViewModel.GetSelection();
        var updateSearchScope = Options.AutoFindInSelection switch
        {
            AutoFindInSelectionMode.Always => true,
            AutoFindInSelectionMode.Multiline => selection.Range.StartLineNumber != selection.Range.EndLineNumber,
            _ => false,
        };
        opts = opts with { UpdateSearchScope = opts.UpdateSearchScope || updateSearchScope };

        NewFindReplaceState stateChanges = (newState ?? new NewFindReplaceState()) with { IsRevealed = true };

        if (opts.SeedSearchStringFromSelection == SeedSearchStringFromSelection.Single)
        {
            var selectionSearchString = SelectionSearchString.Get(ViewModel, opts.SeedSearchStringFromSelection, opts.SeedSearchStringFromNonEmptySelection);
            if (!string.IsNullOrEmpty(selectionSearchString))
            {
                stateChanges = stateChanges with { SearchString = State.IsRegex ? Strings.EscapeRegExpCharacters(selectionSearchString) : selectionSearchString };
            }
        }
        else if (opts.SeedSearchStringFromSelection == SeedSearchStringFromSelection.Multiple && !opts.UpdateSearchScope)
        {
            var selectionSearchString = SelectionSearchString.Get(ViewModel, opts.SeedSearchStringFromSelection);
            if (!string.IsNullOrEmpty(selectionSearchString))
            {
                stateChanges = stateChanges with { SearchString = selectionSearchString };
            }
        }

        // Overwrite isReplaceRevealed
        if (opts.ForceRevealReplace || stateChanges.IsReplaceRevealed == true)
        {
            stateChanges = stateChanges with { IsReplaceRevealed = true };
        }
        else if (!State.IsRevealed)
        {
            stateChanges = stateChanges with { IsReplaceRevealed = false };
        }

        if (opts.UpdateSearchScope)
        {
            IReadOnlyList<Selection> currentSelections = ViewModel.GetSelections();
            if (currentSelections.Any(s => !s.IsEmpty))
            {
                stateChanges = stateChanges with { SearchScope = [.. currentSelections.Select(s => s.Range)] };
            }
        }

        stateChanges = stateChanges with { Loop = opts.Loop };
        State.Change(stateChanges, false);

        _model ??= new FindModelBoundToEditorModel(ViewModel, State, _scheduler, Options);

        Widget?.ApplyStartFocus(opts.ShouldFocus);
    }

    public bool MoveToNextMatch()
    {
        _model?.MoveToNextMatch();
        return _model is not null;
    }

    public bool MoveToPrevMatch()
    {
        _model?.MoveToPrevMatch();
        return _model is not null;
    }

    public bool GoToMatch(int index)
    {
        _model?.MoveToMatch(index);
        return _model is not null;
    }

    public bool Replace()
    {
        _model?.Replace();
        return _model is not null;
    }

    public bool ReplaceAll()
    {
        _model?.ReplaceAll();
        return _model is not null;
    }

    public bool SelectAllMatches()
    {
        if (_model is null)
        {
            return false;
        }

        _model.SelectAllMatches();
        Widget?.FocusEditor();
        return true;
    }
}

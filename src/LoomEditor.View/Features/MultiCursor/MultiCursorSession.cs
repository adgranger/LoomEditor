// Ported from microsoft/vscode: src/vs/editor/contrib/multicursor/browser/multicursor.ts (MultiCursorSession)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Search;
using LoomEditor.Core.Text;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.MultiCursor;

/// <summary>What a session wants the editor to do (Monaco: <c>MultiCursorSessionResult</c>).</summary>
internal sealed record MultiCursorSessionResult(IReadOnlyList<Selection> Selections, Range RevealRange, ScrollType RevealScrollType);

/// <summary>The search a series of Ctrl+D presses shares: what is searched, how, and where the last match was.</summary>
/// <remarks>
/// Behavior differences: <c>editor.hasModel()</c> is always true; <c>editor.hasTextFocus()</c> is <see cref="ViewModel.HasFocus"/>.
/// Threading: UI thread only.
/// </remarks>
internal sealed class MultiCursorSession
{
    private readonly ViewModel _viewModel;
    private readonly string? _wordSeparatorsIfWholeWord;

    private MultiCursorSession(
        ViewModel viewModel,
        FindController findController,
        bool isDisconnectedFromFindController,
        bool usesFindOptions,
        string searchText,
        bool wholeWord,
        bool matchCase,
        Selection? currentMatch)
    {
        _viewModel = viewModel;
        FindController = findController;
        IsDisconnectedFromFindController = isDisconnectedFromFindController;
        UsesFindOptions = usesFindOptions;
        SearchText = searchText;
        WholeWord = wholeWord;
        MatchCase = matchCase;
        CurrentMatch = currentMatch;
        _wordSeparatorsIfWholeWord = wholeWord ? viewModel.CursorConfig.WordSeparators : null;
    }

    public FindController FindController { get; }

    public bool IsDisconnectedFromFindController { get; }

    public bool UsesFindOptions { get; }

    public string SearchText { get; }

    public bool WholeWord { get; }

    public bool MatchCase { get; }

    public Selection? CurrentMatch { get; private set; }

    // Find widget owns entirely what we search for if: focus is not in the editor, the widget is visible and the search string is non-empty.
    public static bool IsFindWidgetSearch(ViewModel viewModel, FindController findController)
    {
        FindReplaceState findState = findController.State;
        return !viewModel.HasFocus && findState.IsRevealed && findState.SearchString.Length > 0;
    }

    public static (bool WholeWord, bool MatchCase, bool UsesFindOptions) GetSelectionSearchOptions(
        MultiCursorOptions options, FindController findController)
    {
        if (options.SelectedTextMatchMode == SelectedTextMatchMode.FindOptions)
        {
            FindReplaceState findState = findController.State;
            return (findState.WholeWord, findState.MatchCase, true);
        }

        return (false, options.SelectedTextMatchMode == SelectedTextMatchMode.CaseSensitive, false);
    }

    public static MultiCursorSession? Create(ViewModel viewModel, MultiCursorOptions options, FindController findController)
    {
        FindReplaceState findState = findController.State;

        if (IsFindWidgetSearch(viewModel, findController))
        {
            // Find widget owns what is searched for
            return new MultiCursorSession(viewModel, findController, false, true, findState.SearchString, findState.WholeWord, findState.MatchCase, null);
        }

        bool isDisconnectedFromFindController = false;
        bool wholeWord;
        bool matchCase;
        bool usesFindOptions;
        List<Selection> selections = viewModel.GetSelections();
        if (selections.Count == 1 && selections[0].IsEmpty)
        {
            isDisconnectedFromFindController = true;
            wholeWord = true;
            matchCase = true;
            usesFindOptions = true;
        }
        else
        {
            (wholeWord, matchCase, usesFindOptions) = GetSelectionSearchOptions(options, findController);
        }

        // Selection owns what is searched for
        Selection s = viewModel.GetSelection();

        string searchText;
        Selection? currentMatch = null;

        if (s.IsEmpty)
        {
            // selection is empty => expand to current word
            WordAtPosition? word = GetConfiguredWordAtPosition(viewModel, s.Range.Start);
            if (word is not { } w)
            {
                return null;
            }

            searchText = w.Word;
            currentMatch = new Selection(s.Range.StartLineNumber, w.StartColumn, s.Range.StartLineNumber, w.EndColumn);
        }
        else
        {
            searchText = viewModel.Model.GetValueInRange(s.Range, EndOfLinePreference.LF);
        }

        return new MultiCursorSession(viewModel, findController, isDisconnectedFromFindController, usesFindOptions, searchText, wholeWord, matchCase, currentMatch);
    }

    // Monaco: editor.getConfiguredWordAtPosition (the word separators option, not the language word pattern)
    public static WordAtPosition? GetConfiguredWordAtPosition(ViewModel viewModel, Position position) =>
        Cursors.WordOperations.GetWordAtPosition(viewModel.Model, viewModel.CursorConfig.WordSeparators, viewModel.CursorConfig.WordSegmenterLocales, position);

    public MultiCursorSessionResult? AddSelectionToNextFindMatch() => Add(GetNextMatch());

    public MultiCursorSessionResult? MoveSelectionToNextFindMatch() => Move(GetNextMatch());

    public MultiCursorSessionResult? AddSelectionToPreviousFindMatch() => Add(GetPreviousMatch());

    public MultiCursorSessionResult? MoveSelectionToPreviousFindMatch() => Move(GetPreviousMatch());

    private MultiCursorSessionResult? Add(Selection? match)
    {
        if (match is not { } m)
        {
            return null;
        }

        return new MultiCursorSessionResult([.. _viewModel.GetSelections(), m], m.Range, ScrollType.Smooth);
    }

    private MultiCursorSessionResult? Move(Selection? match)
    {
        if (match is not { } m)
        {
            return null;
        }

        List<Selection> allSelections = _viewModel.GetSelections();
        allSelections[^1] = m;
        return new MultiCursorSessionResult(allSelections, m.Range, ScrollType.Smooth);
    }

    private Selection? GetNextMatch()
    {
        if (CurrentMatch is { } result)
        {
            CurrentMatch = null;
            return result;
        }

        HighlightFindOptions();

        List<Selection> allSelections = _viewModel.GetSelections();
        Selection lastAddedSelection = allSelections[^1];
        FindMatch? nextMatch = _viewModel.Model.FindNextMatch(SearchText, lastAddedSelection.Range.End, false, MatchCase, _wordSeparatorsIfWholeWord, false);
        return nextMatch is null ? null : new Selection(nextMatch.Range.StartLineNumber, nextMatch.Range.StartColumn, nextMatch.Range.EndLineNumber, nextMatch.Range.EndColumn);
    }

    private Selection? GetPreviousMatch()
    {
        if (CurrentMatch is { } result)
        {
            CurrentMatch = null;
            return result;
        }

        HighlightFindOptions();

        List<Selection> allSelections = _viewModel.GetSelections();
        Selection lastAddedSelection = allSelections[^1];
        FindMatch? previousMatch = _viewModel.Model.FindPreviousMatch(SearchText, lastAddedSelection.Range.Start, false, MatchCase, _wordSeparatorsIfWholeWord, false);
        return previousMatch is null ? null : new Selection(previousMatch.Range.StartLineNumber, previousMatch.Range.StartColumn, previousMatch.Range.EndLineNumber, previousMatch.Range.EndColumn);
    }

    public List<FindMatch> SelectAll(IReadOnlyList<Range>? searchScope)
    {
        HighlightFindOptions();

        TextModel model = _viewModel.Model;
        return searchScope is not null
            ? model.FindMatches(SearchText, searchScope, false, MatchCase, _wordSeparatorsIfWholeWord, false, TextModel.MaxSafeSmallInteger)
            : model.FindMatches(SearchText, true, false, MatchCase, _wordSeparatorsIfWholeWord, false, TextModel.MaxSafeSmallInteger);
    }

    private void HighlightFindOptions()
    {
        if (UsesFindOptions)
        {
            FindController.HighlightFindOptions();
        }
    }
}

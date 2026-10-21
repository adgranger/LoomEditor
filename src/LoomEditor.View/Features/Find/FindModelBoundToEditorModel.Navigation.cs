// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/findModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Search;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Features.Find;

// Moving between matches and selecting all of them.
internal sealed partial class FindModelBoundToEditorModel
{
    private bool IsUsingLineStops() =>
        _state.IsRegex && (_state.SearchString.Contains('^') || _state.SearchString.Contains('$'));

    private Position PrevSearchPosition(Position before)
    {
        var lineNumber = before.LineNumber;
        var column = before.Column;

        if (IsUsingLineStops() || column == 1)
        {
            lineNumber = lineNumber == 1 ? _model.LineCount : lineNumber - 1;
            column = _model.GetLineMaxColumn(lineNumber);
        }
        else
        {
            column--;
        }

        return new Position(lineNumber, column);
    }

    private void MoveToPrevMatch(Position before, bool isRecursed = false)
    {
        if (!_state.CanNavigateBack())
        {
            // we are beyond the first matched find result
            // instead of doing nothing, we should refocus the first item
            if (_decorations.MatchAfterPosition(before) is { } nextMatchRange)
            {
                SetCurrentFindMatch(nextMatchRange);
            }

            return;
        }

        if (_decorations.GetCount() < FindConstants.MatchesLimit)
        {
            Range? prevMatchRange = _decorations.MatchBeforePosition(before);

            if (prevMatchRange is { IsEmpty: true } empty && empty.Start == before)
            {
                before = PrevSearchPosition(before);
                prevMatchRange = _decorations.MatchBeforePosition(before);
            }

            if (prevMatchRange is { } found)
            {
                SetCurrentFindMatch(found);
            }

            return;
        }

        if (CannotFind())
        {
            return;
        }

        Range searchRange = GetSearchRange(_model, _decorations.GetFindScope());

        // ...(----)...|...
        if (searchRange.End.IsBefore(before))
        {
            before = searchRange.End;
        }

        // ...|...(----)...
        if (before.IsBefore(searchRange.Start))
        {
            before = searchRange.End;
        }

        var position = new Position(before.LineNumber, before.Column);

        FindMatch? prevMatch = _model.FindPreviousMatch(_state.SearchString, position, _state.IsRegex, _state.MatchCase, WordSeparatorsOrNull(), false);

        if (prevMatch is { Range.IsEmpty: true } stuck && stuck.Range.Start == position)
        {
            // Looks like we're stuck at this position, unacceptable!
            position = PrevSearchPosition(position);
            prevMatch = _model.FindPreviousMatch(_state.SearchString, position, _state.IsRegex, _state.MatchCase, WordSeparatorsOrNull(), false);
        }

        if (prevMatch is null)
        {
            // there is precisely one match and selection is on top of it
            return;
        }

        if (!isRecursed && !searchRange.Contains(prevMatch.Range))
        {
            MoveToPrevMatch(prevMatch.Range.Start, true);
            return;
        }

        SetCurrentFindMatch(prevMatch.Range);
    }

    /// <summary>Goes to the match before the selection.</summary>
    public void MoveToPrevMatch() => MoveToPrevMatch(_viewModel.GetSelection().Range.Start);

    private Position NextSearchPosition(Position after)
    {
        var lineNumber = after.LineNumber;
        var column = after.Column;

        if (IsUsingLineStops() || column == _model.GetLineMaxColumn(lineNumber))
        {
            lineNumber = lineNumber == _model.LineCount ? 1 : lineNumber + 1;
            column = 1;
        }
        else
        {
            column++;
        }

        return new Position(lineNumber, column);
    }

    private void MoveToNextMatch(Position after)
    {
        if (!_state.CanNavigateForward())
        {
            // we are beyond the last matched find result
            // instead of doing nothing, we should refocus the last item
            if (_decorations.MatchBeforePosition(after) is { } prevMatchRange)
            {
                SetCurrentFindMatch(prevMatchRange);
            }

            return;
        }

        if (_decorations.GetCount() < FindConstants.MatchesLimit)
        {
            Range? nextMatchRange = _decorations.MatchAfterPosition(after);

            if (nextMatchRange is { IsEmpty: true } empty && empty.Start == after)
            {
                // Looks like we're stuck at this position, unacceptable!
                after = NextSearchPosition(after);
                nextMatchRange = _decorations.MatchAfterPosition(after);
            }

            if (nextMatchRange is { } found)
            {
                SetCurrentFindMatch(found);
            }

            return;
        }

        if (GetNextMatch(after, false, true) is { } nextMatch)
        {
            SetCurrentFindMatch(nextMatch.Range);
        }
    }

    private FindMatch? GetNextMatch(Position after, bool captureMatches, bool forceMove, bool isRecursed = false)
    {
        if (CannotFind())
        {
            return null;
        }

        Range searchRange = GetSearchRange(_model, _decorations.GetFindScope());

        // ...(----)...|...
        if (searchRange.End.IsBefore(after))
        {
            after = searchRange.Start;
        }

        // ...|...(----)...
        if (after.IsBefore(searchRange.Start))
        {
            after = searchRange.Start;
        }

        var position = new Position(after.LineNumber, after.Column);

        FindMatch? nextMatch = _model.FindNextMatch(_state.SearchString, position, _state.IsRegex, _state.MatchCase, WordSeparatorsOrNull(), captureMatches);

        if (forceMove && nextMatch is { Range.IsEmpty: true } stuck && stuck.Range.Start == position)
        {
            // Looks like we're stuck at this position, unacceptable!
            position = NextSearchPosition(position);
            nextMatch = _model.FindNextMatch(_state.SearchString, position, _state.IsRegex, _state.MatchCase, WordSeparatorsOrNull(), captureMatches);
        }

        if (nextMatch is null)
        {
            // there is precisely one match and selection is on top of it
            return null;
        }

        if (!isRecursed && !searchRange.Contains(nextMatch.Range))
        {
            return GetNextMatch(nextMatch.Range.End, captureMatches, forceMove, true);
        }

        return nextMatch;
    }

    /// <summary>Goes to the match after the selection.</summary>
    public void MoveToNextMatch() => MoveToNextMatch(_viewModel.GetSelection().Range.End);

    /// <summary>Goes to the match at a 0-based index.</summary>
    public void MoveToMatch(int index)
    {
        if (_decorations.GetDecorationRangeAt(index) is { } decorationRange)
        {
            SetCurrentFindMatch(decorationRange);
        }
    }

    /// <summary>Selects all the matches, keeping the current selection as primary when it is one of them.</summary>
    public void SelectAllMatches()
    {
        if (!HasMatches())
        {
            return;
        }

        // Get all the ranges (even more than the highlighted ones)
        List<FindMatch> matches = FindMatches(_decorations.GetFindScopes(), false, TextModel.MaxSafeSmallInteger);
        List<Selection> selections = [.. matches.Select(m => Selection.FromRange(m.Range, SelectionDirection.LeftToRight))];

        // If one of the ranges is the editor selection, then maintain it as primary
        Selection editorSelection = _viewModel.GetSelection();
        for (var i = 0; i < selections.Count; i++)
        {
            if (selections[i].Range == editorSelection.Range)
            {
                selections.RemoveAt(i);
                selections.Insert(0, editorSelection);
                break;
            }
        }

        _viewModel.SetSelections(Source, selections);
    }
}

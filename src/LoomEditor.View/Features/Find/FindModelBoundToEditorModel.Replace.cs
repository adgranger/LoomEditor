// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/findModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;
using System.Text.RegularExpressions;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Search;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Features.Find;

// Replace and replace all.
internal sealed partial class FindModelBoundToEditorModel
{
    /// <summary>Replaces the current match and goes to the next one; if the selection is not on a match, goes to the next match.</summary>
    public void Replace()
    {
        if (!HasMatches())
        {
            return;
        }

        ReplacePattern replacePattern = GetReplacePattern();
        Selection selection = _viewModel.GetSelection();
        FindMatch? nextMatch = GetNextMatch(selection.Range.Start, true, false);
        if (nextMatch is null)
        {
            return;
        }

        if (selection.Range == nextMatch.Range)
        {
            // selection sits on a find match => replace it!
            var replaceString = replacePattern.BuildReplaceString(nextMatch.Matches, _state.PreserveCase);

            ExecuteEditorCommand("replace", new ReplaceCommand(selection.Range, replaceString));

            _decorations.SetStartPosition(new Position(selection.Range.StartLineNumber, selection.Range.StartColumn + replaceString.Length));
            Research(true);
        }
        else
        {
            _decorations.SetStartPosition(_viewModel.GetPosition());
            SetCurrentFindMatch(nextMatch.Range);
        }
    }

    /// <summary>Replaces every match in one undo step.</summary>
    public void ReplaceAll()
    {
        if (!HasMatches())
        {
            return;
        }

        List<Range>? findScopes = _decorations.GetFindScopes();

        if (findScopes is null && _state.MatchesCount >= FindConstants.MatchesLimit)
        {
            // Doing a replace on the entire file that is over MatchesLimit matches
            LargeReplaceAll();
        }
        else
        {
            RegularReplaceAll(findScopes);
        }

        Research(false);
    }

    /// <summary>
    /// Replaces in the whole text at once. Behavior differences: no <c>String.prototype.replace</c>: the matches are walked with
    /// <see cref="Regex.Match(string, int)"/>, and after an empty match the search moves one code point further, like JavaScript with
    /// the <c>u</c> flag. Like Monaco, word separators are ignored here (a Monaco quirk, kept on purpose).
    /// </summary>
    private void LargeReplaceAll()
    {
        var searchParams = new SearchParams(_state.SearchString, _state.IsRegex, _state.MatchCase, WordSeparatorsOrNull());
        SearchData? searchData = searchParams.ParseSearchRequest();
        if (searchData is null)
        {
            return;
        }

        Regex searchRegex = searchData.Regex;
        if (!searchData.IsMultiline)
        {
            searchRegex = new Regex(searchRegex.ToString(), searchRegex.Options | RegexOptions.Multiline);
        }

        var modelText = _model.GetValue(EndOfLinePreference.LF);
        Range fullModelRange = _model.FullModelRange;

        ReplacePattern replacePattern = GetReplacePattern();
        var preserveCase = _state.PreserveCase;
        var needsGroups = replacePattern.HasReplacementPatterns || preserveCase;
        var staticReplacement = needsGroups ? null : replacePattern.BuildReplaceString(null, preserveCase);

        var result = new StringBuilder(modelText.Length);
        var copied = 0;
        var index = 0;
        while (index <= modelText.Length)
        {
            Match m = searchRegex.Match(modelText, index);
            if (!m.Success)
            {
                break;
            }

            result.Append(modelText, copied, m.Index - copied);
            result.Append(staticReplacement ?? replacePattern.BuildReplaceString(Groups(m), preserveCase));
            copied = m.Index + m.Length;

            // an empty match does not advance: step over a whole surrogate pair (JavaScript AdvanceStringIndex, `u` flag)
            index = m.Length > 0
                ? copied
                : m.Index + (m.Index < modelText.Length && Strings.GetNextCodePoint(modelText, modelText.Length, m.Index) > 0xFFFF ? 2 : 1);
        }

        result.Append(modelText, copied, modelText.Length - copied);

        ExecuteEditorCommand("replaceAll", new ReplaceCommandThatPreservesSelection(fullModelRange, result.ToString(), _viewModel.GetSelection()));
    }

    private static string?[] Groups(Match m) => [.. m.Groups.Cast<Group>().Select(g => g.Success ? g.Value : null)];

    private void RegularReplaceAll(List<Range>? findScopes)
    {
        ReplacePattern replacePattern = GetReplacePattern();
        // Get all the ranges (even more than the highlighted ones)
        List<FindMatch> matches = FindMatches(findScopes, replacePattern.HasReplacementPatterns || _state.PreserveCase, TextModel.MaxSafeSmallInteger);

        List<string> replaceStrings = [.. matches.Select(m => replacePattern.BuildReplaceString(m.Matches, _state.PreserveCase))];

        ExecuteEditorCommand("replaceAll", new ReplaceAllCommand(_viewModel.GetSelection(), [.. matches.Select(m => m.Range)], replaceStrings));
    }
}

// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetSession.ts (SnippetSession)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diagnostics;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Snippets;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Features.Snippet;

/// <summary>
/// Inserts a snippet template at every cursor and keeps one <see cref="OneSnippet"/> per cursor: it moves between the tab stops and
/// merges nested snippets (Monaco: <c>SnippetSession</c>).
/// </summary>
/// <remarks>
/// <para>
/// Behavior differences: only a template at the cursors (no snippet edits: <c>createEditsAndSnippetsFromEdits</c>); no
/// <c>OvertypingCapturer</c>; <c>$CLIPBOARD</c> falls back to the variable host when the options carry no clipboard text; the
/// session does not scroll (<c>revealRange</c>, <c>revealPositionInCenterIfOutsideViewport</c>): the controller does.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed partial class SnippetSession : IDisposable
{
    private readonly EditorContext _editor;
    private readonly string _template;
    private readonly SnippetSessionInsertOptions _options;
    private readonly ISnippetVariableHost _host;
    private readonly TimeProvider _time;
    private readonly Random _random;
    private readonly List<(int NestingLevel, int PlaceholderGroupsIdx, string Template)> _templateMerges = [];
    private List<OneSnippet> _snippets = [];

    /// <summary>Creates the session; call <see cref="Insert"/> to insert the template.</summary>
    /// <param name="editor">The editor to insert into.</param>
    /// <param name="template">The snippet template (TextMate syntax).</param>
    /// <param name="options">How to insert it, or <see langword="null"/> for the defaults.</param>
    /// <param name="host">Supplies the file, workspace and clipboard variables.</param>
    /// <param name="time">Supplies the time variables.</param>
    /// <param name="random">Supplies the random variables.</param>
    public SnippetSession(
        EditorContext editor,
        string template,
        SnippetSessionInsertOptions? options,
        ISnippetVariableHost host,
        TimeProvider time,
        Random random)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(random);

        _editor = editor;
        _template = template;
        _options = options ?? SnippetSessionInsertOptions.Default;
        _host = host;
        _time = time;
        _random = random;
    }

    /// <summary>Removes the decorations of every snippet.</summary>
    public void Dispose()
    {
        foreach (OneSnippet snippet in _snippets)
        {
            snippet.Dispose();
        }
    }

    /// <summary>True at the first tab stop (of the first snippet).</summary>
    public bool IsAtFirstPlaceholder => _snippets[0].IsAtFirstPlaceholder;

    /// <summary>True at the last tab stop (of the first snippet).</summary>
    public bool IsAtLastPlaceholder => _snippets[0].IsAtLastPlaceholder;

    /// <summary>True when the snippet has at least one placeholder.</summary>
    public bool HasPlaceholder => _snippets[0].HasPlaceholder;

    /// <summary>True when the snippet contains a choice.</summary>
    public bool HasChoice => _snippets[0].HasChoice;

    /// <summary>The choice of the active placeholder and its range, or <see langword="null"/>.</summary>
    public (Choice Choice, Range Range)? ActiveChoice => _snippets[0].ActiveChoice;

    /// <summary>The merged templates as <c>(nesting level, placeholder group, template)</c>, oldest first (Monaco: <c>_templateMerges</c>).</summary>
    internal IReadOnlyList<(int NestingLevel, int PlaceholderGroupsIdx, string Template)> TemplateMerges => _templateMerges;

    /// <summary>Inserts the template at every cursor, in one undo step, and selects the first tab stop.</summary>
    public void Insert()
    {
        Guard.Assert(!_editor.IsReadOnly);
        if (_editor.IsReadOnly)
        {
            return;
        }

        (IdentifiedEditOperation[] edits, OneSnippet[] snippets) = CreateEditsAndSnippetsFromSelections(_template, _options, false);
        _snippets = [.. snippets];

        _editor.ViewModel.ExecuteEdits("snippet", edits, allUndoEdits =>
        {
            // The text buffer may remove automatic whitespace when doing any edits: look only at our undo edits, which have an identifier.
            List<ValidEditOperation> undoEdits = allUndoEdits.Where(edit => edit.Identifier is not null).ToList();
            for (int idx = 0; idx < snippets.Length; idx++)
            {
                snippets[idx].Initialize(undoEdits[idx].TextChange);
            }

            return _snippets[0].HasPlaceholder
                ? Move(true)
                : undoEdits.Select(edit => Selection.Collapsed(edit.Range.End)).ToList();
        });
    }

    /// <summary>Inserts a nested snippet at every cursor: inside the active placeholders it is merged into the outer snippets.</summary>
    public void Merge(string template, SnippetSessionInsertOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(template);

        Guard.Assert(!_editor.IsReadOnly);
        if (_editor.IsReadOnly)
        {
            return;
        }

        options ??= SnippetSessionInsertOptions.Default;
        _templateMerges.Add((_snippets[0].NestingLevel, _snippets[0].PlaceholderGroupsIdx, template));
        (IdentifiedEditOperation[] edits, OneSnippet[] createdSnippets) = CreateEditsAndSnippetsFromSelections(template, options, true);
        List<OneSnippet> snippets = [.. createdSnippets];

        _editor.ViewModel.ExecuteEdits("snippet", edits, allUndoEdits =>
        {
            List<ValidEditOperation> undoEdits = allUndoEdits.Where(edit => edit.Identifier is not null).ToList();
            for (int idx = 0; idx < snippets.Count; idx++)
            {
                snippets[idx].Initialize(undoEdits[idx].TextChange);
            }

            // Trivial snippets have no placeholder or are just the final placeholder: they are plain text insertions and are not merged.
            bool isTrivialSnippet = snippets[0].IsTrivialSnippet;

            // Only merge when each active placeholder occurrence has a matching nested snippet: cursor normalization or external
            // selection changes can collapse selections, leaving fewer nested snippets than placeholder occurrences.
            bool canMergeSnippets = snippets.Count == _snippets.Sum(snippet => snippet.ActivePlaceholderCount);
            if (!isTrivialSnippet && canMergeSnippets)
            {
                foreach (OneSnippet snippet in _snippets)
                {
                    int count = snippet.ActivePlaceholderCount;
                    snippet.Merge(snippets.GetRange(0, count));
                    snippets.RemoveRange(0, count);
                }

                Guard.Assert(snippets.Count == 0);
            }

            return _snippets[0].HasPlaceholder && !isTrivialSnippet && canMergeSnippets
                ? Move(null)
                : undoEdits.Select(edit => Selection.Collapsed(edit.Range.End)).ToList();
        });
    }

    /// <summary>Moves every cursor to the next tab stop.</summary>
    public void Next() => MoveAndSelect(true);

    /// <summary>Moves every cursor to the previous tab stop.</summary>
    public void Prev() => MoveAndSelect(false);

    /// <summary>
    /// True when the selections are exactly in the placeholders of one tab stop (every snippet, the same tab stop, the same number
    /// of selections).
    /// </summary>
    public bool IsSelectionWithinPlaceholders()
    {
        if (!HasPlaceholder)
        {
            return false;
        }

        List<Selection> selections = [.. _editor.Selections];
        if (selections.Count < _snippets.Count)
        {
            // We started snippet mode with N selections and have M < N: one snippet is without selection, cancel.
            return false;
        }

        var allPossibleSelections = new Dictionary<double, List<Range>>();
        foreach (OneSnippet snippet in _snippets)
        {
            Dictionary<double, List<Range>> possibleSelections = snippet.ComputePossibleSelections();

            // For the first snippet find the placeholders (and their ranges) that contain at least one selection; for all the
            // remaining snippets the same placeholders (and their ranges) must be used.
            if (allPossibleSelections.Count == 0)
            {
                foreach ((double index, List<Range> ranges) in possibleSelections)
                {
                    ranges.Sort(Range.CompareUsingStarts);
                    if (selections.Any(selection => ranges[0].Contains(selection.Range)))
                    {
                        allPossibleSelections[index] = [];
                    }
                }
            }

            if (allPossibleSelections.Count == 0)
            {
                // No selection could be associated with this (the first) snippet.
                return false;
            }

            // Add the selections of this snippet so that we know all the ranges of each tab stop.
            foreach ((double index, List<Range> array) in allPossibleSelections)
            {
                array.AddRange(possibleSelections[index]);
            }
        }

        // Sort the selections (and later the placeholder ranges), then make sure each range contains the corresponding selection.
        selections.Sort((a, b) => Range.CompareUsingStarts(a.Range, b.Range));

        foreach ((double index, List<Range> ranges) in allPossibleSelections.ToList())
        {
            if (ranges.Count != selections.Count)
            {
                allPossibleSelections.Remove(index);
                continue;
            }

            ranges.Sort(Range.CompareUsingStarts);
            for (int i = 0; i < ranges.Count; i++)
            {
                if (!ranges[i].Contains(selections[i].Range))
                {
                    allPossibleSelections.Remove(index);
                    break;
                }
            }
        }

        // Without a tab stop left that matches the selections, there is no selection in a placeholder any more.
        return allPossibleSelections.Count > 0;
    }

    /// <summary>The range that contains every placeholder of every snippet.</summary>
    public Range? GetEnclosingRange()
    {
        Range? result = null;
        foreach (OneSnippet snippet in _snippets)
        {
            Range? snippetRange = snippet.GetEnclosingRange();
            if (result is null)
            {
                result = snippetRange;
            }
            else if (snippetRange is { } range)
            {
                result = result.Value.Plus(range);
            }
        }

        return result;
    }

    private void MoveAndSelect(bool forward)
    {
        List<Selection> newSelections = Move(forward);
        if (newSelections.Count > 0)
        {
            _editor.SetSelections(newSelections);
        }
    }

    private List<Selection> Move(bool? forward)
    {
        var selections = new List<Selection>();
        foreach (OneSnippet snippet in _snippets)
        {
            selections.AddRange(snippet.Move(forward));
        }

        return selections;
    }
}

// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/components/accessibleDiffViewer.ts (ViewModel, computeViewElementGroups)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diff;
using LoomEditor.Core.Text;

namespace LoomEditor.View.DiffEditor;

/// <summary>
/// The model of the accessible diff viewer: the changes cut into groups of rows, and the current group and row (Monaco: <c>ViewModel</c>
/// of <c>accessibleDiffViewer.ts</c>).
/// </summary>
/// <remarks>
/// <para>
/// Behavior differences: the audio cues are not ported; the groups are built with the original range first (Monaco passes the modified
/// range first, so its header announces the modified line numbers as the original ones, and its initial group is picked on the original
/// side); the selection of the modified line happens in this model, after every change of the current row.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class AccessibleDiffViewerModel
{
    private const int GroupLineMargin = 3;

    private readonly IAccessibleDiffViewerHost _host;
    private readonly bool _canClose;
    private List<ViewElementGroup> _groups = [];
    private int _currentGroupIndex;
    private int _currentElementIndex;

    /// <summary>Creates a model with no group.</summary>
    /// <param name="host">The editors.</param>
    /// <param name="canClose">Whether the viewer can be closed (Monaco: <c>canClose</c>); when false, <see cref="Close"/> and <see cref="RevealCurrentElementInEditor"/> do nothing.</param>
    public AccessibleDiffViewerModel(IAccessibleDiffViewerHost host, bool canClose)
    {
        ArgumentNullException.ThrowIfNull(host);

        _host = host;
        _canClose = canClose;
    }

    /// <summary>Raised when the groups, the current group or the current row changed.</summary>
    public event EventHandler? Changed;

    /// <summary>The groups of changes.</summary>
    public IReadOnlyList<ViewElementGroup> Groups => _groups;

    /// <summary>The index of the current group.</summary>
    public int CurrentGroupIndex => _currentGroupIndex;

    /// <summary>The index of the current row of the current group.</summary>
    public int CurrentElementIndex => _currentElementIndex;

    /// <summary>The current group; <c>null</c> when there is none.</summary>
    public ViewElementGroup? CurrentGroup => _currentGroupIndex >= 0 && _currentGroupIndex < _groups.Count ? _groups[_currentGroupIndex] : null;

    /// <summary>The current row; <c>null</c> when there is none.</summary>
    public ViewElement? CurrentElement =>
        CurrentGroup is { } group && _currentElementIndex >= 0 && _currentElementIndex < group.Lines.Count ? group.Lines[_currentElementIndex] : null;

    /// <summary>
    /// Sets the changes (<c>null</c>: no group), and picks the first group that ends after the cursor of the modified editor as the current
    /// one; when there is none the current group index is kept.
    /// </summary>
    public void SetDiffs(IReadOnlyList<DetailedLineRangeMapping>? diffs) => Update(() =>
    {
        if (diffs is null)
        {
            _groups = [];
            return;
        }

        List<ViewElementGroup> groups = ComputeViewElementGroups(diffs, _host.OriginalLineCount, _host.ModifiedLineCount);
        if (_host.ModifiedCursorLine is { } cursorLine)
        {
            int next = groups.FindIndex(g => cursorLine < g.Range.Modified.EndLineNumberExclusive);
            if (next != -1)
            {
                _currentGroupIndex = next;
            }
        }

        _groups = groups;
    });

    /// <summary>Goes to the first row of the next group, wrapping around; does nothing with fewer than two groups.</summary>
    public void NextGroup() => GoToGroupDelta(1);

    /// <summary>Goes to the first row of the previous group, wrapping around; does nothing with fewer than two groups.</summary>
    public void PreviousGroup() => GoToGroupDelta(-1);

    /// <summary>Goes to the next row of the group, stopping at the last one.</summary>
    public void GoToNextLine() => GoToLineDelta(1);

    /// <summary>Goes to the previous row of the group, stopping at the first one.</summary>
    public void GoToPreviousLine() => GoToLineDelta(-1);

    /// <summary>Goes to a row of the current group, compared by reference; does nothing when it is not in the group.</summary>
    public void GoToLine(ViewElement line)
    {
        ArgumentNullException.ThrowIfNull(line);

        if (CurrentGroup is not { } group)
        {
            return;
        }

        for (int i = 0; i < group.Lines.Count; i++)
        {
            if (ReferenceEquals(group.Lines[i], line))
            {
                Update(() => _currentElementIndex = i);
                return;
            }
        }
    }

    /// <summary>Closes the viewer and shows the current row in its editor.</summary>
    public void RevealCurrentElementInEditor()
    {
        if (!_canClose)
        {
            return;
        }

        _host.SetViewerVisible(false);
        switch (CurrentElement)
        {
            case DeletedLineViewElement deleted:
                _host.OriginalReveal(Range.Collapsed(new Position(deleted.OriginalLineNumber, 1)));
                break;
            case HeaderViewElement:
                _host.ModifiedReveal(null);
                break;
            case { } element:
                _host.ModifiedReveal(Range.Collapsed(new Position(element.GetModifiedLineNumber()!.Value, 1)));
                break;
        }
    }

    /// <summary>Closes the viewer and focuses the modified editor.</summary>
    public void Close()
    {
        if (!_canClose)
        {
            return;
        }

        _host.SetViewerVisible(false);
        _host.ModifiedFocus();
    }

    /// <summary>Monaco: <c>computeViewElementGroups</c>. Changes less than two margins apart share a group.</summary>
    internal static List<ViewElementGroup> ComputeViewElementGroups(IReadOnlyList<DetailedLineRangeMapping> diffs, int originalLineCount, int modifiedLineCount)
    {
        List<ViewElementGroup> result = [];

        foreach (List<DetailedLineRangeMapping> g in ArrayHelpers.GroupAdjacentBy(
            diffs, (a, b) => b.Modified.StartLineNumber - a.Modified.EndLineNumberExclusive < 2 * GroupLineMargin))
        {
            List<ViewElement> viewElements = [new HeaderViewElement()];

            var origFullRange = new LineRange(
                Math.Max(1, g[0].Original.StartLineNumber - GroupLineMargin),
                Math.Min(g[^1].Original.EndLineNumberExclusive + GroupLineMargin, originalLineCount + 1));
            var modifiedFullRange = new LineRange(
                Math.Max(1, g[0].Modified.StartLineNumber - GroupLineMargin),
                Math.Min(g[^1].Modified.EndLineNumberExclusive + GroupLineMargin, modifiedLineCount + 1));

            ArrayHelpers.ForEachAdjacent(g, (a, b) =>
            {
                var origRange = new LineRange(
                    a is not null ? a.Original.EndLineNumberExclusive : origFullRange.StartLineNumber,
                    b is not null ? b.Original.StartLineNumber : origFullRange.EndLineNumberExclusive);
                var modifiedRange = new LineRange(
                    a is not null ? a.Modified.EndLineNumberExclusive : modifiedFullRange.StartLineNumber,
                    b is not null ? b.Modified.StartLineNumber : modifiedFullRange.EndLineNumberExclusive);

                origRange.ForEach(origLineNumber =>
                    viewElements.Add(new UnchangedLineViewElement(origLineNumber, modifiedRange.StartLineNumber + (origLineNumber - origRange.StartLineNumber))));

                if (b is not null)
                {
                    b.Original.ForEach(origLineNumber => viewElements.Add(new DeletedLineViewElement(b, origLineNumber)));
                    b.Modified.ForEach(modifiedLineNumber => viewElements.Add(new AddedLineViewElement(b, modifiedLineNumber)));
                }
            });

            result.Add(new ViewElementGroup(
                new LineRangeMapping(g[0].Original.Join(g[^1].Original), g[0].Modified.Join(g[^1].Modified)),
                viewElements));
        }

        return result;
    }

    private void GoToGroupDelta(int delta)
    {
        if (_groups.Count <= 1)
        {
            return;
        }

        Update(() =>
        {
            _currentGroupIndex = OffsetRange.OfLength(_groups.Count).ClipCyclic(_currentGroupIndex + delta);
            _currentElementIndex = 0;
        });
    }

    private void GoToLineDelta(int delta)
    {
        if (CurrentGroup is not { } group || group.Lines.Count <= 1)
        {
            return;
        }

        Update(() => _currentElementIndex = OffsetRange.OfLength(group.Lines.Count).Clip(_currentElementIndex + delta));
    }

    // Runs a change, then tells the host about the new current row (Monaco: the "select lines in editor" autorun) and raises Changed.
    private void Update(Action change)
    {
        ViewElement? before = CurrentElement;
        List<ViewElementGroup> groupsBefore = _groups;
        int groupBefore = _currentGroupIndex;
        int elementBefore = _currentElementIndex;

        change();

        if (!ReferenceEquals(before, CurrentElement) && CurrentElement is { } current and not HeaderViewElement)
        {
            // Monaco: this keeps the editor commands (revert, stage) working on the current line.
            int line = current.GetModifiedLineNumber() ?? ((DeletedLineViewElement)current).Diff.Modified.StartLineNumber;
            _host.ModifiedSetSelection(Range.Collapsed(new Position(line, 1)));
        }

        if (!ReferenceEquals(groupsBefore, _groups) || groupBefore != _currentGroupIndex || elementBefore != _currentElementIndex)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}

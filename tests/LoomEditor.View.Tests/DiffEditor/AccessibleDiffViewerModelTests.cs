using LoomEditor.Core.Diff;
using LoomEditor.Core.Text;
using LoomEditor.View.Accessibility;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.View.Tests.DiffEditor;

// New tests: Monaco has no test of the accessible diff viewer (DOM). Diffs are built by hand on 20-line files.
public class AccessibleDiffViewerModelTests
{
    private sealed class FakeHost : IAccessibleDiffViewerHost
    {
        public int OriginalLineCount { get; set; } = 20;

        public int ModifiedLineCount { get; set; } = 20;

        public int? ModifiedCursorLine { get; set; }

        public List<string> Calls { get; } = [];

        public List<Range> Selections { get; } = [];

        public string GetOriginalLine(int lineNumber) => $"o{lineNumber}";

        public string GetModifiedLine(int lineNumber) => $"m{lineNumber}";

        public void OriginalReveal(Range range) => Calls.Add($"originalReveal {range.StartLineNumber}");

        public void ModifiedReveal(Range? range) => Calls.Add(range is { } r ? $"modifiedReveal {r.StartLineNumber}" : "modifiedReveal null");

        public void ModifiedSetSelection(Range range) => Selections.Add(range);

        public void ModifiedFocus() => Calls.Add("modifiedFocus");

        public void SetViewerVisible(bool visible) => Calls.Add($"visible {visible}");
    }

    private static DetailedLineRangeMapping Diff(int os, int oe, int ms, int me) => new(new LineRange(os, oe), new LineRange(ms, me), null);

    // orig [5,7) -> mod [5,6): two deleted lines and one added line.
    private static readonly DetailedLineRangeMapping A = Diff(5, 7, 5, 6);

    // Far from A (modified gap of 8 lines).
    private static readonly DetailedLineRangeMapping B = Diff(15, 16, 14, 15);

    // Close to A (modified gap of 2 lines).
    private static readonly DetailedLineRangeMapping C = Diff(9, 10, 8, 9);

    private static (AccessibleDiffViewerModel Model, FakeHost Host) Create(IReadOnlyList<DetailedLineRangeMapping> diffs, int? cursorLine = null, bool canClose = true)
    {
        var host = new FakeHost { ModifiedCursorLine = cursorLine };
        var model = new AccessibleDiffViewerModel(host, canClose);
        model.SetDiffs(diffs);
        return (model, host);
    }

    private static LineType[] Types(ViewElementGroup group) => [.. group.Lines.Select(l => l.Type)];

    [Fact]
    public void Groups_TwoFarApartChanges_TwoGroups() => Assert.Equal(2, Create([A, B]).Model.Groups.Count);

    [Fact]
    public void Groups_TwoCloseChanges_OneGroup() => Assert.Single(Create([A, C]).Model.Groups);

    [Fact]
    public void Groups_ContextLinesLimitedToFileBounds()
    {
        var atStart = AccessibleDiffViewerModel.ComputeViewElementGroups([Diff(1, 2, 1, 2)], 20, 20);
        var atEnd = AccessibleDiffViewerModel.ComputeViewElementGroups([Diff(20, 21, 20, 21)], 20, 20);

        // Header, deleted, added, then three lines of context after; header, three lines before, deleted, added.
        Assert.Equal(6, atStart[0].Lines.Count);
        Assert.Equal(6, atEnd[0].Lines.Count);
    }

    [Fact]
    public void Group_Rows_HeaderThenUnchangedDeletedAddedInOrder()
    {
        ViewElementGroup group = Create([A]).Model.Groups[0];

        Assert.Equal(
            [LineType.Header, LineType.Unchanged, LineType.Unchanged, LineType.Unchanged, LineType.Deleted, LineType.Deleted, LineType.Added, LineType.Unchanged, LineType.Unchanged, LineType.Unchanged],
            Types(group));
        Assert.Equal(new UnchangedLineViewElement(2, 2), group.Lines[1]);
        Assert.Equal(5, ((DeletedLineViewElement)group.Lines[4]).OriginalLineNumber);
        Assert.Equal(5, ((AddedLineViewElement)group.Lines[6]).ModifiedLineNumber);
        // Context after the change: original 7..9 against modified 6..8.
        Assert.Equal(new UnchangedLineViewElement(7, 6), group.Lines[7]);
        // The range is the original side first (Monaco swaps the two).
        Assert.Equal((5, 7), (group.Range.Original.StartLineNumber, group.Range.Original.EndLineNumberExclusive));
        Assert.Equal((5, 6), (group.Range.Modified.StartLineNumber, group.Range.Modified.EndLineNumberExclusive));
    }

    [Fact]
    public void Group_Rows_UnchangedBetweenTwoChangesInGroup()
    {
        ViewElementGroup group = AccessibleDiffViewerModel.ComputeViewElementGroups([Diff(5, 6, 5, 6), Diff(8, 9, 8, 9)], 20, 20)[0];

        // After the first change (deleted 5, added 5): original 6..7 are unchanged, then the second change.
        Assert.Equal(new UnchangedLineViewElement(6, 6), group.Lines[^7]);
        Assert.Equal(new UnchangedLineViewElement(7, 7), group.Lines[^6]);
        Assert.Equal(LineType.Deleted, group.Lines[^5].Type);
    }

    [Fact]
    public void NextGroup_Wraps()
    {
        var (model, _) = Create([A, B]);

        model.NextGroup();
        Assert.Equal(1, model.CurrentGroupIndex);
        model.NextGroup();
        Assert.Equal(0, model.CurrentGroupIndex);
    }

    [Fact]
    public void PreviousGroup_Wraps()
    {
        var (model, _) = Create([A, B]);

        model.PreviousGroup();

        Assert.Equal(1, model.CurrentGroupIndex);
    }

    [Fact]
    public void NextGroup_SingleGroup_Stays()
    {
        var (model, _) = Create([A]);
        int changes = 0;
        model.Changed += (_, _) => changes++;

        model.NextGroup();
        model.PreviousGroup();

        Assert.Equal(0, model.CurrentGroupIndex);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void GoToNextLine_ClipsAtEnd()
    {
        var (model, _) = Create([A]);
        int count = model.CurrentGroup!.Lines.Count;

        for (int i = 0; i < count + 2; i++)
        {
            model.GoToNextLine();
        }

        Assert.Equal(count - 1, model.CurrentElementIndex);
    }

    [Fact]
    public void GoToPreviousLine_ClipsAtStart()
    {
        var (model, _) = Create([A]);
        model.GoToNextLine();

        model.GoToPreviousLine();
        model.GoToPreviousLine();

        Assert.Equal(0, model.CurrentElementIndex);
    }

    [Fact]
    public void InitialGroup_FirstGroupEndingAfterCursor()
    {
        Assert.Equal(1, Create([A, B], cursorLine: 10).Model.CurrentGroupIndex);
        Assert.Equal(0, Create([A, B], cursorLine: 3).Model.CurrentGroupIndex);
    }

    [Fact]
    public void InitialGroup_CursorAfterAll_StaysFirst() => Assert.Equal(0, Create([A, B], cursorLine: 18).Model.CurrentGroupIndex);

    [Fact]
    public void CurrentElementChange_SelectsModifiedLine()
    {
        var (model, host) = Create([A]);

        model.GoToNextLine();

        // The first context line of the group: original 2, modified 2.
        Assert.Equal(Range.Collapsed(new Position(2, 1)), Assert.Single(host.Selections));
    }

    [Fact]
    public void CurrentElementChange_Deleted_SelectsDiffModifiedStart()
    {
        // A pure deletion: original [6,8) is gone, the modified side is empty at line 5.
        var (model, host) = Create([Diff(6, 8, 5, 5)]);

        for (int i = 0; i < 4; i++)
        {
            model.GoToNextLine();
        }

        Assert.Equal(LineType.Deleted, model.CurrentElement!.Type);
        Assert.Equal(Range.Collapsed(new Position(5, 1)), host.Selections[^1]);
    }

    [Fact]
    public void CurrentElementChange_Header_NoSelection()
    {
        var (model, host) = Create([A, B]);

        model.NextGroup();

        Assert.IsType<HeaderViewElement>(model.CurrentElement);
        Assert.Empty(host.Selections);
    }

    [Fact]
    public void Reveal_Deleted_CallsOriginalReveal()
    {
        var (model, host) = Create([A]);
        model.GoToLine(model.CurrentGroup!.Lines[4]);

        model.RevealCurrentElementInEditor();

        Assert.Equal(["visible False", "originalReveal 5"], host.Calls);
    }

    [Fact]
    public void Reveal_Added_CallsModifiedReveal()
    {
        var (model, host) = Create([A]);
        model.GoToLine(model.CurrentGroup!.Lines[6]);

        model.RevealCurrentElementInEditor();

        Assert.Equal(["visible False", "modifiedReveal 5"], host.Calls);
    }

    [Fact]
    public void Reveal_Header_ModifiedRevealWithNull()
    {
        var (model, host) = Create([A]);

        model.RevealCurrentElementInEditor();

        Assert.Equal(["visible False", "modifiedReveal null"], host.Calls);
    }

    [Fact]
    public void Close_CanCloseFalse_DoesNothing()
    {
        var (model, host) = Create([A], canClose: false);

        model.Close();
        model.RevealCurrentElementInEditor();

        Assert.Empty(host.Calls);
    }

    [Fact]
    public void Close_HidesTheViewerAndFocusesTheModifiedEditor()
    {
        var (model, host) = Create([A]);

        model.Close();

        Assert.Equal(["visible False", "modifiedFocus"], host.Calls);
    }

    [Fact]
    public void Labels_Header_OriginalAndModifiedCounts()
    {
        Assert.Equal(
            "Difference 1 of 2: original line 3, no lines changed, modified line 3, 1 line changed",
            AccessibilityMessages.DiffHeaderLabel(1, 2, 3, 0, 3, 1));
        Assert.Equal(
            "Difference 2 of 2: original line 7, 5 lines changed, modified line 9, 2 lines changed",
            AccessibilityMessages.DiffHeaderLabel(2, 2, 7, 5, 9, 2));
    }

    [Fact]
    public void Labels_HeaderText_Format() => Assert.Equal("1/2: @@ -3,2 +3,4 @@", AccessibilityMessages.DiffHeaderText(1, 2, 3, 2, 3, 4));

    [Fact]
    public void Labels_Rows_UnchangedSameNumber() =>
        Assert.Equal("let x unchanged line 4", AccessibilityMessages.DiffRowLabel(LineType.Unchanged, "let x", 4, 4));

    [Fact]
    public void Labels_Rows_UnchangedDifferentNumbers() =>
        Assert.Equal("let x original line 7 modified line 6", AccessibilityMessages.DiffRowLabel(LineType.Unchanged, "let x", 7, 6));

    [Fact]
    public void Labels_Rows_AddedDeleted()
    {
        Assert.Equal("+ new modified line 5", AccessibilityMessages.DiffRowLabel(LineType.Added, "new", null, 5));
        Assert.Equal("- old original line 6", AccessibilityMessages.DiffRowLabel(LineType.Deleted, "old", 6, null));
    }

    [Fact]
    public void Labels_Rows_BlankLine() =>
        Assert.Equal("+ blank modified line 5", AccessibilityMessages.DiffRowLabel(LineType.Added, "", null, 5));

    [Fact]
    public void Labels_ListLabel() =>
        Assert.Equal("Accessible Diff Viewer. Use arrow up and down to navigate.", AccessibilityMessages.DiffViewerListLabel);

    [Fact]
    public void Changed_RaisedOnNavigation()
    {
        var (model, _) = Create([A, B]);
        int changes = 0;
        model.Changed += (_, _) => changes++;

        model.GoToNextLine();
        model.NextGroup();
        model.SetDiffs([A]);

        Assert.Equal(3, changes);
    }
}

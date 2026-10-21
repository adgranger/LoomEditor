// Ported from microsoft/vscode: src/vs/editor/test/browser/viewModel/viewModelImpl.test.ts (suite 'ViewModel': the hidden areas tests)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Model.Tokens;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Layout.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

public class ViewModelHiddenAreasTests
{
    private const int LineHeight = 20;

    private readonly FakeTimeProvider _clock = new();
    private readonly FakeFrameScheduler _frames = new();

    private ViewModel CreateViewModel(string text)
    {
        var fontInfo = new FontInfo(1, "Consolas", "normal", 14, LineHeight, 0, true, 8.4, 16.8, true, 8.4, 8, 8, 8.4);
        var viewModel = new ViewModel(new TextModel(text), fontInfo, new EditorLayoutOptions(), new ViewLayoutOptions(LineHeight, ScrollBeyondLastLine: false), _frames.Schedule, _clock);
        viewModel.SetViewportSize(400, 100); // 5 lines
        return viewModel;
    }

    private static string Lines(int count) => string.Join('\n', Enumerable.Range(1, count).Select(i => $"line{i}"));

    private static void Test(string[] text, Action<ViewModel, TextModel> callback) =>
        TestCodeEditor.WithTestCodeEditor(text, editor => callback(editor.ViewModel, editor.Model));

    private static void AssertAtLeastOneViewLine(ViewModel viewModel) =>
        Assert.True(viewModel.GetLineCount() >= 1, $"expected at least 1 view line but got {viewModel.GetLineCount()}");

    // Monaco: 'issue #21073: SplitLinesCollection: attempt to access a \'newer\' model'
    [Fact]
    public void ModelEdit_InsertingLines_AddsViewLines() => Test([string.Empty], (viewModel, model) =>
    {
        Assert.Equal(1, viewModel.GetLineCount());

        model.ApplyEdits([TextEdit.Insert(new Position(1, 1), "line01\nline02\nline03\nline04\nline05\nline06\nline07\nline08\nline09\nline10")]);

        Assert.Equal(10, viewModel.GetLineCount());
    });

    // Monaco: 'issue #44805: No visible lines via API call'
    [Fact]
    public void SetHiddenAreas_HidingEveryLine_KeepsVisibleRanges() => Test(["line1", "line2", "line3"], (viewModel, _) =>
    {
        Assert.Equal(3, viewModel.GetLineCount());
        viewModel.SetHiddenAreas([new Range(1, 1, 3, 1)]);
        Assert.NotNull(viewModel.GetVisibleRanges());
    });

    // Monaco: 'issue #44805: No visible lines via undoing'
    [Fact]
    public void Undo_AfterHidingLines_KeepsVisibleRanges() => Test([string.Empty], (viewModel, model) =>
    {
        Assert.Equal(1, viewModel.GetLineCount());

        model.PushEditOperations(null, [TextEdit.Insert(new Position(1, 1), "line1\nline2\nline3")], null);

        viewModel.SetHiddenAreas([new Range(1, 1, 1, 1)]);
        Assert.Equal(2, viewModel.GetLineCount());

        model.Undo();
        Assert.NotNull(viewModel.GetVisibleRanges());
    });

    // Monaco: 'issue #40926: Incorrect spacing when inserting new line after multiple folded blocks of code'
    [Fact]
    public void ModelEdit_AfterMultipleFoldedBlocks_KeepsTheViewLineCount() => Test(
        [
            "foo = {",
            "    foobar: function() {",
            "        this.foobar();",
            "    },",
            "    foobar: function() {",
            "        this.foobar();",
            "    },",
            "    foobar: function() {",
            "        this.foobar();",
            "    },",
            "}",
        ],
        (viewModel, model) =>
        {
            viewModel.SetHiddenAreas([new Range(3, 1, 3, 1), new Range(6, 1, 6, 1), new Range(9, 1, 9, 1)]);

            model.ApplyEdits(
            [
                TextEdit.Insert(new Position(4, 7), "\n    "),
                TextEdit.Insert(new Position(7, 7), "\n    "),
                TextEdit.Insert(new Position(10, 7), "\n    "),
            ]);

            Assert.Equal(11, viewModel.GetLineCount());
        });

    // Monaco: suite 'hidden areas must always leave at least one visible line'
    // Monaco: 'replacing the only visible line content does not make it hidden'
    [Fact]
    public void ReplacingTheOnlyVisibleLineContent_DoesNotMakeItHidden() => Test(["line1", "line2", "line3"], (viewModel, model) =>
    {
        // Hide lines 1 and 3, leaving only line 2 visible
        viewModel.SetHiddenAreas([new Range(1, 1, 1, 1), new Range(3, 1, 3, 1)]);
        Assert.Equal(1, viewModel.GetLineCount());

        model.ApplyEdits([TextEdit.Replace(new Range(2, 1, 2, 6), "new content")]);

        AssertAtLeastOneViewLine(viewModel);
    });

    // Monaco: 'deleting the only visible line when it is the last line'
    [Fact]
    public void DeletingTheOnlyVisibleLine_WhenItIsTheLastLine_KeepsAViewLine() => Test(["line1", "line2", "line3"], (viewModel, model) =>
    {
        // Hide lines 1-2, leaving only line 3 visible
        viewModel.SetHiddenAreas([new Range(1, 1, 2, 1)]);
        Assert.Equal(1, viewModel.GetLineCount());

        // Delete line 3 by merging it into line 2
        model.ApplyEdits([TextEdit.Delete(new Range(2, 6, 3, 6))]);

        AssertAtLeastOneViewLine(viewModel);
    });

    // Monaco: 'deleting the only visible line when it is in the middle'
    [Fact]
    public void DeletingTheOnlyVisibleLine_WhenItIsInTheMiddle_KeepsAViewLine() => Test(["line1", "line2", "line3", "line4", "line5"], (viewModel, model) =>
    {
        // Hide lines 1-2 and 4-5, leaving only line 3 visible
        viewModel.SetHiddenAreas([new Range(1, 1, 2, 1), new Range(4, 1, 5, 1)]);
        Assert.Equal(1, viewModel.GetLineCount());

        // Delete line 3 by merging adjacent lines
        model.ApplyEdits([TextEdit.Delete(new Range(2, 6, 4, 1))]);

        AssertAtLeastOneViewLine(viewModel);
    });

    // Monaco: 'undo that removes the only visible line'
    [Fact]
    public void Undo_ThatRemovesTheOnlyVisibleLine_KeepsAViewLine() => Test(["line1"], (viewModel, model) =>
    {
        Assert.Equal(1, viewModel.GetLineCount());

        model.PushEditOperations(null, [TextEdit.Insert(new Position(1, 6), "\nline2\nline3\nline4\nline5")], null);
        Assert.Equal(5, viewModel.GetLineCount());

        // Hide lines 1-2 and 4-5, leaving only line 3 visible
        viewModel.SetHiddenAreas([new Range(1, 1, 2, 1), new Range(4, 1, 5, 1)]);
        Assert.Equal(1, viewModel.GetLineCount());

        // Undo collapses back to 1 line, but hidden area decorations may grow
        model.Undo();

        AssertAtLeastOneViewLine(viewModel);
    });

    // Monaco: 'deleting the only visible line between two hidden areas leaves all lines hidden'
    [Fact]
    public void DeletingTheOnlyVisibleLineBetweenTwoHiddenAreas_KeepsAViewLine() =>
        Test(["line1", "line2", "line3", "line4", "line5", "line6", "line7", "line8"], (viewModel, model) =>
        {
            Assert.Equal(8, viewModel.GetLineCount());

            // Hide lines 1-5 and 7-8, leaving only line 6 visible
            viewModel.SetHiddenAreas([new Range(1, 1, 5, 1), new Range(7, 1, 8, 1)]);
            Assert.Equal(1, viewModel.GetLineCount());

            // Delete lines 6, 7, 8: the only visible line plus some hidden ones
            model.ApplyEdits([TextEdit.Delete(new Range(6, 1, 8, 5))]);

            AssertAtLeastOneViewLine(viewModel);
        });

    // Monaco: 'multiple visible lines deleted leaving only hidden lines'
    [Fact]
    public void DeletingMultipleVisibleLines_LeavingOnlyHiddenLines_KeepsAViewLine() =>
        Test(["hidden1", "hidden2", "visible1", "visible2", "hidden3", "hidden4"], (viewModel, model) =>
        {
            viewModel.SetHiddenAreas([new Range(1, 1, 2, 1), new Range(5, 1, 6, 1)]);
            Assert.Equal(2, viewModel.GetLineCount());

            // Delete visible lines 3 and 4
            model.ApplyEdits([TextEdit.Delete(new Range(2, 8, 5, 1))]);

            AssertAtLeastOneViewLine(viewModel);
        });

    // Monaco: 'hidden areas from multiple sources that overlap produce valid merged result'
    [Fact]
    public void SetHiddenAreas_FromOverlappingSources_ProducesAValidMergedResult() =>
        Test([.. Enumerable.Range(1, 10).Select(i => $"line{i}")], (viewModel, _) =>
        {
            // MergeLineRangeArray advances both indexes after a merge (a bug of Monaco, ported): the hidden areas
            // are normalized by the view lines, so lines 1-9 are hidden and line 10 stays visible.
            viewModel.SetHiddenAreas([new Range(1, 1, 8, 1)], "sourceA");
            viewModel.SetHiddenAreas([new Range(2, 1, 3, 1), new Range(5, 1, 6, 1), new Range(8, 1, 9, 1)], "sourceB");

            Assert.Equal(1, viewModel.GetLineCount());

            IReadOnlyList<Range> hiddenAreas = viewModel.GetHiddenAreas();
            for (int i = 1; i < hiddenAreas.Count; i++)
            {
                Assert.True(
                    hiddenAreas[i].StartLineNumber > hiddenAreas[i - 1].EndLineNumber,
                    $"hidden areas should not overlap: [{hiddenAreas[i - 1].StartLineNumber}-{hiddenAreas[i - 1].EndLineNumber}] and [{hiddenAreas[i].StartLineNumber}-{hiddenAreas[i].EndLineNumber}]");
            }
        });

    // Monaco: 'tab size change with drifted hidden area decorations must not leave 0 visible lines'
    [Fact]
    public void TabSizeChange_WithDriftedHiddenAreaDecorations_KeepsAViewLine() => Test(["line1", "line2", "line3"], (viewModel, model) =>
    {
        // Hide lines 1-2, leaving only line 3 visible.
        viewModel.SetHiddenAreas([new Range(1, 1, 2, 1)]);
        Assert.Equal(1, viewModel.GetLineCount());

        // Inserting at (2,1), the end edge of the hidden area decoration, makes the decoration grow over the visible line.
        model.ApplyEdits([TextEdit.Insert(new Position(2, 1), "x\n")]);
        model.ApplyEdits([TextEdit.Insert(new Position(3, 1), "y\n")]);

        // Delete lines 4-5 to collapse back, making the decoration cover everything.
        model.ApplyEdits([TextEdit.Delete(new Range(4, 1, 5, 6))]);

        // The tab size change builds the lines again from the decoration ranges.
        model.UpdateOptions(new TextModelUpdateOptions(TabSize: 8));

        AssertAtLeastOneViewLine(viewModel);
    });

    [Fact]
    public void SetHiddenAreas_UpdatesTheLayoutLineCount()
    {
        using ViewModel viewModel = CreateViewModel(Lines(5));
        Assert.Equal(5 * LineHeight, viewModel.ViewLayout.GetContentHeight());

        viewModel.SetHiddenAreas([new Range(2, 1, 3, 1)]);

        Assert.Equal(3, viewModel.GetLineCount());
        Assert.Equal(3 * LineHeight, viewModel.ViewLayout.GetContentHeight());
    }

    [Fact]
    public void SetHiddenAreas_CursorInHiddenArea_MovesToTheLineAbove() => Test(["line1", "line2", "line3", "line4", "line5"], (viewModel, _) =>
    {
        viewModel.SetSelections("test", [Selection.Collapsed(new Position(3, 2))]);

        viewModel.SetHiddenAreas([new Range(3, 1, 4, 1)]);

        CursorState state = viewModel.GetPrimaryCursorState();
        Assert.Equal(2, state.ViewState.Position.LineNumber);
        Assert.Equal(new Position(3, 2), state.ModelState.Position);
        Assert.Equal(Selection.Collapsed(new Position(3, 2)), viewModel.GetSelection());
    });

    [Fact]
    public void SetHiddenAreas_SameRanges_RaisesNoEvent()
    {
        using ViewModel viewModel = CreateViewModel(Lines(5));
        viewModel.SetHiddenAreas([new Range(2, 1, 3, 1)]);
        var events = new List<OutgoingViewModelEvent>();
        viewModel.EventDispatcher.OnEvent += (_, e) => events.Add(e);

        viewModel.SetHiddenAreas([new Range(2, 1, 3, 1)]);

        Assert.Empty(events);
    }

    [Fact]
    public void SetHiddenAreas_RaisesHiddenAreasChangedEvent()
    {
        using ViewModel viewModel = CreateViewModel(Lines(5));
        var events = new List<OutgoingViewModelEvent>();
        viewModel.EventDispatcher.OnEvent += (_, e) => events.Add(e);

        viewModel.SetHiddenAreas([new Range(2, 1, 3, 1)]);

        Assert.Single(events, e => e is HiddenAreasChangedEvent);
    }

    [Fact]
    public void ModelTokensChanged_HiddenLines_RaisesViewLineRanges()
    {
        using ViewModel viewModel = CreateViewModel(Lines(10));
        var handler = new RecordingHandler();
        viewModel.EventDispatcher.AddViewEventHandler(handler);
        viewModel.SetHiddenAreas([new Range(3, 1, 4, 1)]);
        handler.Events.Clear();

        viewModel.Model.RaiseTokensChanged(new ModelTokensChangedEventArgs([new(2, 5)]));

        ViewTokensChangedEvent e = Assert.IsType<ViewTokensChangedEvent>(Assert.Single(handler.Events));
        Assert.Equal([new ViewLineRange(2, 3)], e.Ranges);
    }

    [Fact]
    public void VisibleLines_WithHiddenArea_ReportsModelRangesAroundIt()
    {
        using ViewModel viewModel = CreateViewModel(Lines(20));
        var states = new List<AttachedViewState>();
        viewModel.Model.AttachedViews.VisibleRangesChanged += (_, state) => states.Add(state!);

        viewModel.SetHiddenAreas([new Range(3, 1, 4, 1)]);

        // view lines 1-5 are the model lines 1, 2, 5, 6, 7
        AttachedViewState last = states[^1];
        Assert.Equal([(1, 3), (5, 8)], last.VisibleLineRanges.Select(r => (r.Start, r.EndExclusive)));
    }

    private sealed class RecordingHandler : IViewEventHandler
    {
        public List<ViewEvent> Events { get; } = [];

        public void HandleEvents(IReadOnlyList<ViewEvent> events) => Events.AddRange(events);
    }
}

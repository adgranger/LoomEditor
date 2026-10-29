using LoomEditor.Core.Text;
using LoomEditor.Core.Comments;
using LoomEditor.View.Comments;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Comments;

// Not a port: Monaco has no comment widget. All tests are new.
public class CommentThreadManagerTests
{
    private const string Text = "l1\nl2\nl3\nl4\nl5";

    private static readonly TestCodeEditorOptions WrapOptions =
        new(Wrapping: new EditorWrappingOptions { WordWrap = WordWrap.WordWrapColumn, WordWrapColumn = 10 });

    private static void With(Action<TestCodeEditor, CommentThreadManager> test, string text = Text, TestCodeEditorOptions? options = null) =>
        TestCodeEditor.WithTestCodeEditor(text, editor =>
        {
            using var manager = new CommentThreadManager(editor.Model, editor.ViewModel.ViewZones, 7);
            test(editor, manager);
        }, options);

    private static void Edit(TestCodeEditor editor, Range range, string text) =>
        editor.Model.ApplyEdits([new TextEdit(range, text)]);

    private static ViewZone Zone(TestCodeEditor editor, CommentThreadManager manager, CommentThread thread) =>
        editor.ViewModel.ViewZones.GetZone(manager.GetZoneId(thread)!)!;

    private static IEditorWhitespace Whitespace(TestCodeEditor editor, string zoneId) =>
        editor.ViewModel.ViewLayout.GetWhitespaces().Single(w => w.Id == zoneId);

    [Fact]
    public void Add_CreatesWidgetZoneAfterLine() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2, "title");

        ViewZone zone = Zone(editor, manager, thread);
        Assert.Equal(ViewZoneKind.Widget, zone.Kind);
        Assert.Equal(2, zone.AfterLineNumber);
        Assert.Equal(CommentThreadManager.DefaultHeight, zone.HeightInPx);
        Assert.True(zone.SuppressMouseDown);
        Assert.Same(thread, zone.Data);
        Assert.Equal("title", thread.Title);
        Assert.Equal(2, thread.LineNumber);
        Assert.Equal(CommentThreadManager.DefaultHeight, Whitespace(editor, zone.Id).Height);
        Assert.Same(thread, manager.Find(thread.Id));
    });

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Add_LineOutOfRange_Throws(int line) => With((_, manager) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.Add(line)));

    [Fact]
    public void Add_SameLineTwice_TwoZonesInOrder() => With((editor, manager) =>
    {
        CommentThread first = manager.Add(2);
        CommentThread second = manager.Add(2);

        string[] order = editor.ViewModel.ViewLayout.GetWhitespaces().Select(w => w.Id).ToArray();
        Assert.Equal([manager.GetZoneId(first), manager.GetZoneId(second)], order);
    });

    [Fact]
    public void InsertLineAbove_ThreadMovesDown() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(3);

        Edit(editor, new Range(1, 1, 1, 1), "new\n");

        Assert.Equal(4, thread.LineNumber);
    });

    [Fact]
    public void DeleteLineAbove_ThreadMovesUp() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(3);

        Edit(editor, new Range(1, 1, 2, 1), "");

        Assert.Equal(2, thread.LineNumber);
    });

    [Fact]
    public void TypeAtEndOfLine_ThreadStaysOnLine() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2);

        Edit(editor, new Range(2, 3, 2, 3), "xyz");
        Edit(editor, new Range(2, 1, 2, 1), "abc");

        Assert.Equal(2, thread.LineNumber);
    });

    [Fact]
    public void DeleteThreadLine_ThreadStaysAtReplacementLine() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(3);

        Edit(editor, new Range(3, 1, 4, 1), "");

        Assert.Equal(3, thread.LineNumber);
        Assert.Equal(3, Zone(editor, manager, thread).AfterLineNumber);
    });

    [Fact]
    public void ReplaceWholeDocument_ClampsToLineCount() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(5);

        editor.Model.SetValue("a\nb");

        Assert.Equal(2, thread.LineNumber);
        Assert.Equal(2, Zone(editor, manager, thread).AfterLineNumber);
        Assert.Single(manager.Threads);
    });

    [Fact]
    public void Moved_UpdatesZoneAfterLineNumber() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(3);

        Edit(editor, new Range(1, 1, 1, 1), "new\n");

        Assert.Equal(4, Zone(editor, manager, thread).AfterLineNumber);
        Assert.Equal(4, Whitespace(editor, manager.GetZoneId(thread)!).AfterLineNumber);
    });

    [Fact]
    public void Moved_RaisesThreadMovedOnce() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(3);
        List<CommentThread> moved = [];
        manager.ThreadMoved += (_, t) => moved.Add(t);

        Edit(editor, new Range(1, 1, 1, 1), "new\n");

        Assert.Equal([thread], moved);
    });

    [Fact]
    public void Moved_ThenRecompute_ZoneStaysUnderThread() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(3);
        Edit(editor, new Range(1, 1, 1, 1), "new\n");

        editor.ViewModel.ViewZones.RecomputeWhitespaceProps();

        Assert.Equal(4, Whitespace(editor, manager.GetZoneId(thread)!).AfterLineNumber);
    });

    [Fact]
    public void Moved_ThenWrapChange_ZoneStaysUnderThread() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2);
        Edit(editor, new Range(1, 1, 1, 1), "new\n");
        Assert.Equal(3, thread.LineNumber);

        // Line 1 ("aaaa bbbb cccc") wraps in two view lines: model line 3 is view line 4.
        Assert.Equal(4, Whitespace(editor, manager.GetZoneId(thread)!).AfterLineNumber);

        // Wrapping off: the zone is re-derived from AfterLineNumber and stays under model line 3.
        editor.ViewModel.SetOptions(new EditorLayoutOptions(), editor.ViewModel.ViewLayoutOptions);

        Assert.Equal(3, Zone(editor, manager, thread).AfterLineNumber);
        Assert.Equal(3, Whitespace(editor, manager.GetZoneId(thread)!).AfterLineNumber);
    }, "aaaa bbbb cccc\nl2\nl3", WrapOptions);

    [Fact]
    public void SetHeight_UpdatesZoneAndLayout() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2);

        manager.SetHeight(thread, 200);

        Assert.Equal(200, manager.GetHeight(thread));
        Assert.Equal(200, Whitespace(editor, manager.GetZoneId(thread)!).Height);
    });

    [Fact]
    public void SetHeight_Same_NoLayoutCall() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2);
        int changes = 0;
        editor.ViewModel.ViewZones.ZonesChanged += (_, _) => changes++;

        manager.SetHeight(thread, CommentThreadManager.DefaultHeight);

        Assert.Equal(0, changes);
    });

    [Fact]
    public void SetHeight_Negative_ClampsToZero() => With((_, manager) =>
    {
        CommentThread thread = manager.Add(2);

        manager.SetHeight(thread, -5);

        Assert.Equal(0, manager.GetHeight(thread));
    });

    [Fact]
    public void Remove_RemovesZoneAndDecoration() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2);
        int raised = 0;
        manager.ThreadsChanged += (_, _) => raised++;

        Assert.True(manager.Remove(thread));

        Assert.Empty(editor.ViewModel.ViewLayout.GetWhitespaces());
        Assert.Empty(editor.ViewModel.ViewZones.Zones);
        Assert.DoesNotContain(editor.Model.GetAllDecorations(7), d => d.OwnerId == 7);
        Assert.Empty(manager.Threads);
        Assert.Equal(1, raised);
    });

    [Fact]
    public void Remove_Unknown_False() => With((_, manager) =>
    {
        var other = new CommentThread("x", 1);

        Assert.False(manager.Remove(other));
    });

    [Fact]
    public void LineTextChanged_RaisedWhenContentOfThreadLineChanges() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2);
        List<CommentThread> raised = [];
        manager.ThreadLineTextChanged += (_, t) => raised.Add(t);

        Edit(editor, new Range(2, 1, 2, 1), "x");

        Assert.Equal([thread], raised);
    });

    [Fact]
    public void LineTextChanged_NotRaisedForOtherLines() => With((editor, manager) =>
    {
        manager.Add(2);
        int raised = 0;
        manager.ThreadLineTextChanged += (_, _) => raised++;

        Edit(editor, new Range(4, 1, 4, 1), "x");
        Edit(editor, new Range(1, 1, 1, 1), "new\n");

        Assert.Equal(0, raised);
    });

    [Fact]
    public void Dispose_RemovesEverything() => With((editor, manager) =>
    {
        manager.Add(2);
        manager.Add(3);

        manager.Dispose();

        Assert.Empty(editor.ViewModel.ViewZones.Zones);
        Assert.DoesNotContain(editor.Model.GetAllDecorations(7), d => d.OwnerId == 7);
        Assert.Empty(manager.Threads);
    });

    [Fact]
    public void Threads_KeepCreationOrder() => With((_, manager) =>
    {
        CommentThread a = manager.Add(4);
        CommentThread b = manager.Add(1);
        CommentThread c = manager.Add(3);

        Assert.Equal([a, b, c], manager.Threads);
    });

    private static void AssertRange(CommentThread thread, int start, int end)
    {
        Assert.Equal(start, thread.StartLineNumber);
        Assert.Equal(end, thread.LineNumber);
    }

    private static bool Highlighted(TestCodeEditor editor, CommentThreadManager manager, CommentThread thread) =>
        editor.Model.GetDecorationOptions(manager.GetDecorationId(thread)!)?.ClassName == "comment-thread-range";

    [Fact]
    public void Highlight_MultiLineOpen_UsesRangeClass() => With((editor, manager) =>
        Assert.True(Highlighted(editor, manager, manager.Add(2, 3))));

    [Fact]
    public void Highlight_SingleLine_NoClass() => With((editor, manager) =>
        Assert.False(Highlighted(editor, manager, manager.Add(2))));

    [Fact]
    public void Highlight_Resolved_RemovedThenReopened_Restored() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2, 3);

        thread.State = CommentThreadState.Resolved;
        Assert.False(Highlighted(editor, manager, thread));

        thread.State = CommentThreadState.Open;
        Assert.True(Highlighted(editor, manager, thread));
    });

    [Fact]
    public void Highlight_RangeCollapsesToOneLine_Removed() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2, 4);

        Edit(editor, new Range(2, 1, 5, 1), "");

        AssertRange(thread, 2, 2);
        Assert.False(Highlighted(editor, manager, thread));
    });

    [Fact]
    public void Highlight_SingleLineGrowsToRange_Added() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(3, 3);

        Edit(editor, new Range(3, 2, 3, 2), "x\n");

        AssertRange(thread, 3, 4);
        Assert.True(Highlighted(editor, manager, thread));
    });

    [Fact]
    public void Highlight_SwitchOptions_KeepsTrackedRange() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2, 4);
        string id = manager.GetDecorationId(thread)!;
        Range? before = editor.Model.GetDecorationRange(id);

        thread.State = CommentThreadState.Resolved;

        Assert.Equal(before, editor.Model.GetDecorationRange(id));
    });

    [Fact]
    public void Remove_UnsubscribesFromThreadChanged() => With((_, manager) =>
    {
        CommentThread thread = manager.Add(2, 3);
        manager.Remove(thread);

        thread.State = CommentThreadState.Resolved;

        Assert.Empty(manager.Threads);
    });

    [Fact]
    public void AddRange_CreatesZoneAfterLastLine() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2, 4);

        AssertRange(thread, 2, 4);
        Assert.Equal(4, Zone(editor, manager, thread).AfterLineNumber);
        Assert.Equal(4, Whitespace(editor, manager.GetZoneId(thread)!).AfterLineNumber);
    });

    [Fact]
    public void AddRange_EndBeforeStart_Throws() => With((_, manager) =>
        Assert.Throws<ArgumentOutOfRangeException>("endLineNumber", () => manager.Add(3, 2)));

    [Fact]
    public void AddRange_EndPastLastLine_Throws() => With((_, manager) =>
        Assert.Throws<ArgumentOutOfRangeException>("endLineNumber", () => manager.Add(3, 6)));

    [Fact]
    public void AddRange_StartEqualsEnd_SameAsSingleLine() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2, 2);

        AssertRange(thread, 2, 2);
        Assert.Equal(2, Zone(editor, manager, thread).AfterLineNumber);
    });

    [Fact]
    public void Range_InsertLineAbove_BothEndsMoveDown() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2, 3);

        Edit(editor, new Range(1, 1, 1, 1), "new\n");

        AssertRange(thread, 3, 4);
        Assert.Equal(4, Zone(editor, manager, thread).AfterLineNumber);
    });

    [Fact]
    public void Range_InsertLineInside_GrowsAndZoneMoves() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2, 4);

        Edit(editor, new Range(3, 1, 3, 1), "new\n");

        AssertRange(thread, 2, 5);
        Assert.Equal(5, Zone(editor, manager, thread).AfterLineNumber);
        Assert.Equal(5, Whitespace(editor, manager.GetZoneId(thread)!).AfterLineNumber);
    });

    [Fact]
    public void Range_DeleteLineInside_ShrinksAndZoneMoves() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2, 4);

        Edit(editor, new Range(3, 1, 4, 1), "");

        AssertRange(thread, 2, 3);
        Assert.Equal(3, Zone(editor, manager, thread).AfterLineNumber);
    });

    [Fact]
    public void Range_EnterAtStartOfFirstLine_MovesWholeRange() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2, 3);

        Edit(editor, new Range(2, 1, 2, 1), "\n");

        AssertRange(thread, 3, 4);
    });

    [Fact]
    public void Range_EnterAtEndOfLastLine_DoesNotGrow() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2, 3);

        Edit(editor, new Range(3, 3, 3, 3), "\n");

        AssertRange(thread, 2, 3);
    });

    [Fact]
    public void Range_TypeAtEdges_DoesNotGrow() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2, 3);

        Edit(editor, new Range(2, 1, 2, 1), "abc");
        Edit(editor, new Range(3, 6, 3, 6), "xyz");

        AssertRange(thread, 2, 3);
    });

    [Fact]
    public void Range_DeleteWholeRange_CollapsesToOneLine() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2, 4);

        Edit(editor, new Range(2, 1, 5, 1), "");

        AssertRange(thread, 2, 2);
        Assert.Equal(2, Zone(editor, manager, thread).AfterLineNumber);
    });

    [Fact]
    public void Range_ReplaceWholeDocument_ClampsBothEnds() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(4, 5);

        editor.Model.SetValue("a\nb");

        AssertRange(thread, 2, 2);
        Assert.Equal(2, Zone(editor, manager, thread).AfterLineNumber);
        Assert.Single(manager.Threads);
    });

    [Fact]
    public void Range_DeleteFirstLineOfRange_ShrinksFromTop() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(3, 5);
        List<CommentThread> moved = [];
        manager.ThreadMoved += (_, t) => moved.Add(t);

        Edit(editor, new Range(3, 1, 4, 1), "");

        AssertRange(thread, 3, 4);
        Assert.Equal([thread], moved);
    });

    [Fact]
    public void Range_Moved_ThenRecompute_ZoneStaysUnderLastLine() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2, 3);
        Edit(editor, new Range(1, 1, 1, 1), "new\n");

        editor.ViewModel.ViewZones.RecomputeWhitespaceProps();

        Assert.Equal(4, Whitespace(editor, manager.GetZoneId(thread)!).AfterLineNumber);
    });

    [Fact]
    public void RangeTextChanged_EditInsideRange_Raised() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(2, 4);
        List<CommentThread> raised = [];
        manager.ThreadLineTextChanged += (_, t) => raised.Add(t);

        Edit(editor, new Range(3, 1, 3, 1), "x");

        Assert.Equal([thread], raised);
    });

    [Fact]
    public void RangeTextChanged_EditOutsideRange_NotRaised() => With((editor, manager) =>
    {
        manager.Add(2, 4);
        int raised = 0;
        manager.ThreadLineTextChanged += (_, _) => raised++;

        Edit(editor, new Range(5, 1, 5, 1), "x");
        Edit(editor, new Range(1, 1, 1, 1), "new\n");

        Assert.Equal(0, raised);
    });

    private const string TextModel = "let value = 1;\nsecond line\nthird";

    private static readonly Range Value = new(1, 5, 1, 10);

    private static void AssertColumns(CommentThread thread, int startColumn, int endColumn)
    {
        Assert.Equal(startColumn, thread.StartColumn);
        Assert.Equal(endColumn, thread.EndColumn);
    }

    [Fact]
    public void AddText_DecorationIsTheExactRange() => With((editor, manager) =>
    {
        CommentThread t = manager.Add(Value);

        Assert.Equal(Value, editor.Model.GetDecorationRange(manager.GetDecorationId(t)!));
        AssertColumns(t, 5, 10);
    }, TextModel);

    [Fact]
    public void AddText_ZoneAfterLastLine() => With((editor, manager) =>
    {
        CommentThread t = manager.Add(new Range(1, 5, 2, 3));

        Assert.Equal(2, Zone(editor, manager, t).AfterLineNumber);
    }, TextModel);

    [Fact]
    public void AddText_IsNotWholeLines() => With((_, manager) =>
        Assert.False(manager.Add(Value).IsWholeLines), TextModel);

    [Theory]
    [InlineData(0, 4)]
    [InlineData(4, 4)]
    public void AddText_LineOutOfDocument_Throws(int line, int endLine) => With((_, manager) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.Add(new Range(line, 1, endLine, 1))), TextModel);

    [Fact]
    public void AddText_ColumnPastLineEnd_IsClamped() => With((_, manager) =>
    {
        CommentThread t = manager.Add(new Range(1, 5, 1, 99));

        AssertColumns(t, 5, 15);
    }, TextModel);

    [Fact]
    public void AddLines_EndColumnIsMaxColumnOfLastLine() => With((_, manager) =>
    {
        CommentThread t = manager.Add(1, 2);

        Assert.True(t.IsWholeLines);
        AssertColumns(t, 1, 12);
    }, TextModel);

    [Fact]
    public void Text_TypeBeforeOnSameLine_ColumnsShift_NoEvents() => With((editor, manager) =>
    {
        CommentThread t = manager.Add(Value);
        int events = 0;
        t.Changed += (_, _) => events++;
        manager.ThreadMoved += (_, _) => events++;

        Edit(editor, new Range(1, 1, 1, 1), "x");

        AssertColumns(t, 6, 11);
        Assert.Equal(0, events);
    }, TextModel);

    [Fact]
    public void Text_TypeAtStartEdge_DoesNotGrow() => With((editor, manager) =>
    {
        CommentThread t = manager.Add(Value);

        Edit(editor, new Range(1, 5, 1, 5), "x");

        AssertColumns(t, 6, 11);
    }, TextModel);

    [Fact]
    public void Text_TypeAtEndEdge_DoesNotGrow() => With((editor, manager) =>
    {
        CommentThread t = manager.Add(Value);

        Edit(editor, new Range(1, 10, 1, 10), "x");

        AssertColumns(t, 5, 10);
    }, TextModel);

    [Fact]
    public void Text_TypeInside_Grows() => With((editor, manager) =>
    {
        CommentThread t = manager.Add(Value);

        Edit(editor, new Range(1, 7, 1, 7), "x");

        AssertColumns(t, 5, 11);
    }, TextModel);

    [Fact]
    public void Text_DeleteAllItsText_BecomesEmpty_ThreadKept() => With((editor, manager) =>
    {
        CommentThread t = manager.Add(Value);

        Edit(editor, Value, "");

        AssertColumns(t, 5, 5);
        Assert.Contains(t, manager.Threads);
    }, TextModel);

    [Fact]
    public void Text_EnterInside_BecomesMultiLine_ZoneMoves() => With((editor, manager) =>
    {
        CommentThread t = manager.Add(Value);
        int moved = 0;
        manager.ThreadMoved += (_, _) => moved++;

        Edit(editor, new Range(1, 7, 1, 7), "\n");

        Assert.Equal(1, t.StartLineNumber);
        Assert.Equal(2, t.LineNumber);
        AssertColumns(t, 5, 4);
        Assert.Equal(2, Zone(editor, manager, t).AfterLineNumber);
        Assert.Equal(1, moved);
    }, TextModel);

    [Fact]
    public void Text_EnterAtStartOfLine_MovesDown() => With((editor, manager) =>
    {
        CommentThread t = manager.Add(Value);

        Edit(editor, new Range(1, 1, 1, 1), "\n");

        Assert.Equal(2, t.StartLineNumber);
        Assert.Equal(2, t.LineNumber);
        AssertColumns(t, 5, 10);
    }, TextModel);

    [Fact]
    public void Text_ReplaceWholeDocument_ClampsLinesAndColumns() => With((editor, manager) =>
    {
        CommentThread t = manager.Add(Value);

        editor.Model.SetValue("ab");

        Assert.Equal(1, t.StartLineNumber);
        Assert.Equal(1, t.LineNumber);
        Assert.InRange(t.StartColumn, 1, 3);
        Assert.InRange(t.EndColumn, 1, 3);
        Assert.True(t.StartColumn <= t.EndColumn);
    }, TextModel);

    [Fact]
    public void Lines_TypeAtEndOfLastLine_EndColumnFollows_NoEvents() => With((editor, manager) =>
    {
        CommentThread t = manager.Add(2);
        int events = 0;
        t.Changed += (_, _) => events++;

        Edit(editor, new Range(2, 12, 2, 12), "xy");

        AssertColumns(t, 1, 14);
        Assert.Equal(0, events);
    }, TextModel);

    [Fact]
    public void RangeText_TextThread_EditOutsideTheTextOnItsLine_RaisesLineTextChanged() => With((editor, manager) =>
    {
        CommentThread t = manager.Add(Value);
        List<CommentThread> raised = [];
        manager.ThreadLineTextChanged += (_, x) => raised.Add(x);

        Edit(editor, new Range(1, 13, 1, 13), "z");

        Assert.Equal([t], raised);
    }, TextModel);

    private static bool TextHighlighted(TestCodeEditor editor, CommentThreadManager manager, CommentThread thread) =>
        editor.Model.GetDecorationOptions(manager.GetDecorationId(thread)!)?.ClassName == "comment-thread-text";

    [Fact]
    public void HighlightText_Open_UsesTextClass() => With((editor, manager) =>
        Assert.True(TextHighlighted(editor, manager, manager.Add(new Range(1, 5, 2, 3)))), TextModel);

    [Fact]
    public void HighlightText_SingleLine_IsHighlighted() => With((editor, manager) =>
        Assert.True(TextHighlighted(editor, manager, manager.Add(Value))), TextModel);

    [Fact]
    public void HighlightText_Resolved_Removed_ThenReopened_Restored() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(Value);

        thread.State = CommentThreadState.Resolved;
        Assert.False(TextHighlighted(editor, manager, thread));

        thread.State = CommentThreadState.Open;
        Assert.True(TextHighlighted(editor, manager, thread));
    }, TextModel);

    [Fact]
    public void HighlightText_AllTextDeleted_Removed() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(Value);

        Edit(editor, Value, "");

        Assert.False(TextHighlighted(editor, manager, thread));
    }, TextModel);

    [Fact]
    public void HighlightText_SwitchOptions_KeepsTrackedRange() => With((editor, manager) =>
    {
        CommentThread thread = manager.Add(Value);
        string id = manager.GetDecorationId(thread)!;

        thread.State = CommentThreadState.Resolved;

        Assert.Equal(Value, editor.Model.GetDecorationRange(id));
    }, TextModel);

    [Fact]
    public void HighlightLines_Unchanged() => With((editor, manager) =>
    {
        Assert.Null(editor.Model.GetDecorationOptions(manager.GetDecorationId(manager.Add(1))!)?.ClassName);
        Assert.True(Highlighted(editor, manager, manager.Add(1, 2)));
    }, TextModel);
}

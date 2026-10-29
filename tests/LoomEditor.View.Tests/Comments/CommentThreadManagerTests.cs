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
}

using LoomEditor.Core.Comments;
using LoomEditor.Core.Text;
using LoomEditor.View.Comments;
using LoomEditor.View.Features;
using LoomEditor.View.Tests.Accessibility.TestHelpers;

namespace LoomEditor.View.Tests.Comments;

public class CommentThreadsControllerTests
{
    private static readonly CommentAuthor Author = new("Ada");

    private static (EditorContext Context, CommentThreadsController Controller, RecordingAnnouncer Announcer) Create(string text = "one\ntwo\nthree")
    {
        var context = new EditorContext();
        context.SetText(text);
        var announcer = new RecordingAnnouncer();
        context.Announcer = announcer;
        return (context, new CommentThreadsController(context), announcer);
    }

    private static Comment Suggest(string suggestion) => Comment.Create(Author, "```suggestion\n" + suggestion + "\n```");

    [Fact]
    public void Add_ReturnsThreadOnLine()
    {
        (_, CommentThreadsController controller, _) = Create();

        CommentThread thread = controller.Add(2, "t", false);

        Assert.Equal(2, thread.LineNumber);
        Assert.Same(thread, Assert.Single(controller.Threads));
    }

    [Fact]
    public void Add_StartComposing_ViewModelIsComposing()
    {
        (_, CommentThreadsController controller, _) = Create();

        CommentThread thread = controller.Add(1, null, true);

        Assert.True(controller.GetViewModel(thread).IsComposing);
    }

    [Fact]
    public void Add_RaisesThreadAdded()
    {
        (_, CommentThreadsController controller, _) = Create();
        CommentThread? raised = null;
        controller.ThreadAdded += (_, t) => raised = t;

        CommentThread thread = controller.Add(1, null, false);

        Assert.Same(thread, raised);
    }

    [Fact]
    public void Add_StartComposing_Announces()
    {
        (_, CommentThreadsController controller, RecordingAnnouncer announcer) = Create();

        controller.Add(3, null, true);

        Assert.Equal([CommentStrings.ThreadAdded(3)], announcer.Messages);
    }

    [Fact]
    public void Add_Silent_DoesNotAnnounce()
    {
        (_, CommentThreadsController controller, RecordingAnnouncer announcer) = Create();

        controller.Add(3, null, false);

        Assert.Empty(announcer.Messages);
    }

    [Fact]
    public void Remove_RaisesThreadRemovingBeforeRemoval()
    {
        (_, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(1, null, false);
        int countDuringEvent = -1;
        controller.ThreadRemoving += (_, _) => countDuringEvent = controller.Threads.Count;

        Assert.True(controller.Remove(thread));

        Assert.Equal(1, countDuringEvent);
        Assert.Empty(controller.Threads);
    }

    [Fact]
    public void Remove_DisposesViewModel()
    {
        (_, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(1, null, false);
        CommentThreadViewModel viewModel = controller.GetViewModel(thread);
        int changes = 0;
        viewModel.Changed += (_, _) => changes++;

        controller.Remove(thread);
        thread.Title = "after";

        Assert.Equal(0, changes);
        Assert.Throws<ArgumentException>(() => controller.GetViewModel(thread));
    }

    [Fact]
    public void Submit_RaisesCommentSubmittedWithText()
    {
        (_, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(1, null, true);
        (CommentThread Thread, string Text)? raised = null;
        controller.CommentSubmitted += (_, e) => raised = e;
        CommentThreadViewModel viewModel = controller.GetViewModel(thread);
        viewModel.SetDraft("hello  ");

        viewModel.Submit();

        Assert.Equal((thread, "hello"), raised);
    }

    [Fact]
    public void Cancel_NoComments_RemovesThread()
    {
        (_, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(1, null, true);
        CommentThread? cancelled = null;
        controller.CommentCancelled += (_, t) => cancelled = t;

        controller.GetViewModel(thread).Cancel();

        Assert.Same(thread, cancelled);
        Assert.Empty(controller.Threads);
    }

    [Fact]
    public void Cancel_WithComments_KeepsThread()
    {
        (_, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(1, null, false);
        thread.AddComment(Comment.Create(Author, "hi"));
        controller.GetViewModel(thread).StartComposing();

        controller.GetViewModel(thread).Cancel();

        Assert.Single(controller.Threads);
    }

    [Fact]
    public void Cancel_HandlerRemovesThread_NoException()
    {
        (_, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(1, null, true);
        controller.CommentCancelled += (_, t) => controller.Remove(t);

        controller.GetViewModel(thread).Cancel();

        Assert.Empty(controller.Threads);
    }

    [Fact]
    public void CommentAdded_WhileComposing_AnnouncesPosted()
    {
        (_, CommentThreadsController controller, RecordingAnnouncer announcer) = Create();
        CommentThread thread = controller.Add(1, null, true);
        announcer.Messages.Clear();

        thread.AddComment(Comment.Create(Author, "hi"));
        thread.AddComment(Comment.Create(Author, "again"));

        Assert.Equal([CommentStrings.CommentPosted], announcer.Messages);
    }

    [Fact]
    public void RequestAdd_RaisesWithLine()
    {
        (_, CommentThreadsController controller, _) = Create();
        CommentRange? line = null;
        controller.AddCommentRequested += (_, l) => line = l;

        controller.RequestAdd(2);

        Assert.Equal((2, 2), (line!.Value.StartLineNumber, line.Value.LineNumber));
        Assert.Null(line.Value.TextRange);
        Assert.Empty(controller.Threads);
    }

    [Fact]
    public void ApplySuggestion_ReplacesLineAndRaisesEvent()
    {
        (EditorContext context, CommentThreadsController controller, RecordingAnnouncer announcer) = Create();
        CommentThread thread = controller.Add(2, null, false);
        Comment comment = Suggest("TWO");
        thread.AddComment(comment);
        (CommentThread Thread, Comment Comment, int StartLineNumber, int LineNumber)? raised = null;
        controller.SuggestionApplied += (_, e) => raised = e;

        ApplySuggestionResult result = controller.ApplySuggestion(thread, comment.Id, 0, "two");

        Assert.Equal(ApplySuggestionResult.Applied, result);
        Assert.Equal("one\nTWO\nthree", context.Model.GetValue());
        Assert.Equal((thread, comment, 2, 2), raised);
        Assert.Contains(CommentStrings.SuggestionApplied, announcer.Messages);
    }

    [Fact]
    public void ApplySuggestion_OneUndoRestoresText()
    {
        (EditorContext context, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(2, null, false);
        Comment comment = Suggest("a\nb");
        thread.AddComment(comment);

        controller.ApplySuggestion(thread, comment.Id, 0, null);
        context.Model.Undo();

        Assert.Equal("one\ntwo\nthree", context.Model.GetValue());
    }

    [Fact]
    public void ApplySuggestion_PutsCursorAtLineStart()
    {
        (EditorContext context, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(2, null, false);
        Comment comment = Suggest("TWO");
        thread.AddComment(comment);

        controller.ApplySuggestion(thread, comment.Id, 0, null);

        Assert.Equal(Selection.Collapsed(new Position(2, 1)), context.Selection);
    }

    [Fact]
    public void ApplySuggestion_ReadOnly_ReturnsReadOnly()
    {
        (EditorContext context, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(2, null, false);
        Comment comment = Suggest("TWO");
        thread.AddComment(comment);
        context.IsReadOnly = true;

        Assert.Equal(ApplySuggestionResult.ReadOnly, controller.ApplySuggestion(thread, comment.Id, 0, "two"));
        Assert.Equal("one\ntwo\nthree", context.Model.GetValue());
    }

    [Fact]
    public void ApplySuggestion_StaleLine_ReturnsStale()
    {
        (_, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(2, null, false);
        Comment comment = Suggest("TWO");
        thread.AddComment(comment);

        Assert.Equal(ApplySuggestionResult.Stale, controller.ApplySuggestion(thread, comment.Id, 0, "other"));
    }

    [Fact]
    public void ApplySuggestion_NoOp_ReturnsNoOp()
    {
        (_, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(2, null, false);
        Comment comment = Suggest("two");
        thread.AddComment(comment);

        Assert.Equal(ApplySuggestionResult.NoOp, controller.ApplySuggestion(thread, comment.Id, 0, "two"));
    }

    [Fact]
    public void ApplySuggestion_UnknownComment_ReturnsNotFound()
    {
        (_, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(2, null, false);

        Assert.Equal(ApplySuggestionResult.NotFound, controller.ApplySuggestion(thread, "nope", 0, null));
    }

    [Fact]
    public void ApplySuggestion_BadIndex_ReturnsNotFound()
    {
        (_, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(2, null, false);
        Comment comment = Suggest("TWO");
        thread.AddComment(comment);

        Assert.Equal(ApplySuggestionResult.NotFound, controller.ApplySuggestion(thread, comment.Id, 1, null));
        Assert.Equal(ApplySuggestionResult.NotFound, controller.ApplySuggestion(thread, comment.Id, -1, null));
    }

    [Fact]
    public void ApplySuggestion_Deletion_RemovesLine()
    {
        (EditorContext context, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(2, null, false);
        Comment comment = Comment.Create(Author, "```suggestion\n```");
        thread.AddComment(comment);

        Assert.Equal(ApplySuggestionResult.Applied, controller.ApplySuggestion(thread, comment.Id, 0, "two"));
        Assert.Equal("one\nthree", context.Model.GetValue());
    }

    [Fact]
    public void ThreadsFollowEdits_ThroughController()
    {
        (EditorContext context, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(2, null, false);

        context.Model.ApplyEdits([new TextEdit(new Range(1, 1, 1, 1), "zero\n")]);

        Assert.Equal(3, thread.LineNumber);
    }

    [Fact]
    public void AddRange_ViewModelSeedIsRangeText()
    {
        (_, CommentThreadsController controller, _) = Create();

        CommentThread thread = controller.Add(1, 2, null, false);

        Assert.Equal("one\ntwo", controller.GetViewModel(thread).SuggestionSeed);
    }

    [Fact]
    public void AddRange_StartComposing_AnnouncesRange()
    {
        (_, CommentThreadsController controller, RecordingAnnouncer announcer) = Create();

        controller.Add(1, 3, null, true);

        Assert.Contains(CommentStrings.ThreadAdded(1, 3), announcer.Messages);
    }

    [Fact]
    public void AddRange_TitleSaysLines()
    {
        (_, CommentThreadsController controller, _) = Create();

        CommentThread thread = controller.Add(1, 3, null, false);

        Assert.Equal(CommentStrings.ThreadTitle(1, 3), controller.GetViewModel(thread).Title);
    }

    [Fact]
    public void ApplySuggestion_Range_ReplacesAllLinesInOneUndoStep()
    {
        (EditorContext context, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(1, 2, null, false);
        Comment comment = Suggest("X");
        thread.AddComment(comment);

        Assert.Equal(ApplySuggestionResult.Applied, controller.ApplySuggestion(thread, comment.Id, 0, "one\ntwo"));
        Assert.Equal("X\nthree", context.Model.GetValue());
        context.Model.Undo();

        Assert.Equal("one\ntwo\nthree", context.Model.GetValue());
    }

    [Fact]
    public void ApplySuggestion_Range_CaretAtStart()
    {
        (EditorContext context, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(2, 3, null, false);
        Comment comment = Suggest("X");
        thread.AddComment(comment);

        controller.ApplySuggestion(thread, comment.Id, 0, null);

        Assert.Equal(Selection.Collapsed(new Position(2, 1)), context.Selection);
    }

    [Fact]
    public void ApplySuggestion_Range_StaleWhenALineInsideChanged()
    {
        (_, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(1, 3, null, false);
        Comment comment = Suggest("X");
        thread.AddComment(comment);

        Assert.Equal(ApplySuggestionResult.Stale, controller.ApplySuggestion(thread, comment.Id, 0, "one\nCHANGED\nthree"));
    }

    [Fact]
    public void ApplySuggestion_Range_NoOp()
    {
        (_, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(1, 2, null, false);
        Comment comment = Suggest("one\ntwo");
        thread.AddComment(comment);

        Assert.Equal(ApplySuggestionResult.NoOp, controller.ApplySuggestion(thread, comment.Id, 0, "one\ntwo"));
    }

    [Fact]
    public void ApplySuggestion_Range_Deletion_RemovesLines()
    {
        (EditorContext context, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(1, 2, null, false);
        Comment comment = Comment.Create(Author, "```suggestion\n```");
        thread.AddComment(comment);

        Assert.Equal(ApplySuggestionResult.Applied, controller.ApplySuggestion(thread, comment.Id, 0, "one\ntwo"));
        Assert.Equal("three", context.Model.GetValue());
    }

    [Fact]
    public void ApplySuggestion_Range_RaisesWithStartAndEndBeforeEdit()
    {
        (_, CommentThreadsController controller, _) = Create();
        CommentThread thread = controller.Add(1, 3, null, false);
        Comment comment = Suggest("X");
        thread.AddComment(comment);
        (CommentThread Thread, Comment Comment, int StartLineNumber, int LineNumber)? raised = null;
        controller.SuggestionApplied += (_, e) => raised = e;

        controller.ApplySuggestion(thread, comment.Id, 0, null);

        Assert.Equal((thread, comment, 1, 3), raised);
    }

    [Fact]
    public void RequestAddRange_RaisesBothLines()
    {
        (_, CommentThreadsController controller, _) = Create();
        CommentRange? lines = null;
        controller.AddCommentRequested += (_, l) => lines = l;

        controller.RequestAdd(1, 3);

        Assert.Equal((1, 3), (lines!.Value.StartLineNumber, lines.Value.LineNumber));
        Assert.Null(lines.Value.TextRange);
        Assert.Empty(controller.Threads);
    }

    private const string Code = "let value = 1;";

    private static CommentRange Word(EditorContext context) => CommentRange.FromRange(context.Model, new Range(1, 5, 1, 10));

    // The model, not the context, validates: FromRange needs the model.
    [Fact]
    public void AddText_ManagerHasExactDecoration()
    {
        (EditorContext context, CommentThreadsController controller, _) = Create(Code);

        CommentThread thread = controller.Add(Word(context), null, false);

        Assert.False(thread.IsWholeLines);
        Assert.Equal((5, 10), (thread.StartColumn, thread.EndColumn));
    }

    [Fact]
    public void AddText_StartComposing_AnnouncesSelectedText()
    {
        (EditorContext context, CommentThreadsController controller, RecordingAnnouncer announcer) = Create(Code);

        controller.Add(Word(context), null, true);

        Assert.Equal(["Comment thread added on the selected text on line 1"], announcer.Messages);
    }

    [Fact]
    public void AddText_TitleSaysLine()
    {
        (EditorContext context, CommentThreadsController controller, _) = Create(Code);

        CommentThread thread = controller.Add(Word(context), null, false);

        Assert.Equal("Comment on line 1", controller.GetViewModel(thread).Title);
    }

    [Fact]
    public void GetCommentedText_Text_ReturnsTheText()
    {
        (EditorContext context, CommentThreadsController controller, _) = Create(Code);

        CommentThread thread = controller.Add(Word(context), null, false);

        Assert.Equal("value", controller.GetCommentedText(thread));
    }

    [Fact]
    public void GetCommentedText_MultiLine_JoinsWithLf()
    {
        (EditorContext context, CommentThreadsController controller, _) = Create("abc\r\ndef\r\nghi");

        CommentThread thread = controller.Add(CommentRange.FromRange(context.Model, new Range(1, 2, 2, 3)), null, false);

        Assert.Equal("bc\nde", controller.GetCommentedText(thread));
    }

    [Fact]
    public void GetCommentedText_WholeLines_Empty()
    {
        (_, CommentThreadsController controller, _) = Create();

        Assert.Equal("", controller.GetCommentedText(controller.Add(1, null, false)));
    }

    [Fact]
    public void ApplySuggestion_TextThread_ReplacesTheCoveringLine()
    {
        (EditorContext context, CommentThreadsController controller, _) = Create(Code);
        CommentThread thread = controller.Add(Word(context), null, false);
        Comment comment = Suggest("let total = 2;");
        thread.AddComment(comment);

        ApplySuggestionResult result = controller.ApplySuggestion(thread, comment.Id, 0, Code);

        Assert.Equal(ApplySuggestionResult.Applied, result);
        Assert.Equal("let total = 2;", context.Model.GetValue());
        context.Model.Undo();
        Assert.Equal(Code, context.Model.GetValue());
    }

    [Fact]
    public void ApplySuggestion_TextThread_StaleWhenTheLineChangedOutsideTheText()
    {
        (EditorContext context, CommentThreadsController controller, _) = Create(Code);
        CommentThread thread = controller.Add(Word(context), null, false);
        Comment comment = Suggest("let total = 2;");
        thread.AddComment(comment);
        context.Model.ApplyEdits([new TextEdit(new Range(1, 14, 1, 15), ";;")]);

        ApplySuggestionResult result = controller.ApplySuggestion(thread, comment.Id, 0, Code);

        Assert.Equal(ApplySuggestionResult.Stale, result);
    }

    [Fact]
    public void RequestAdd_Text_RaisesTheExactRange()
    {
        (EditorContext context, CommentThreadsController controller, _) = Create(Code);
        CommentRange? raised = null;
        controller.AddCommentRequested += (_, r) => raised = r;

        controller.RequestAdd(Word(context));

        Assert.Equal(new Range(1, 5, 1, 10), raised!.Value.TextRange);
    }
}

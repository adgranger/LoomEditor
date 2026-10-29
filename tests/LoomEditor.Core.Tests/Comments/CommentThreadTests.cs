using LoomEditor.Core.Comments;

namespace LoomEditor.Core.Tests.Comments;

public class CommentThreadTests
{
    private static readonly CommentAuthor Alice = new("Alice");

    private static CommentThread NewThread(out List<CommentThreadChangedEventArgs> events)
    {
        var thread = new CommentThread("t1", 3);
        var list = new List<CommentThreadChangedEventArgs>();
        thread.Changed += (_, e) => list.Add(e);
        events = list;
        return thread;
    }

    [Fact]
    public void Comment_BlankId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Comment(" ", Alice, "x", DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentNullException>(() => new Comment("a", null!, "x", DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentNullException>(() => new Comment("a", Alice, null!, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Comment_Create_AssignsIdAndTimestamp()
    {
        Comment a = Comment.Create(Alice, "");
        Comment b = Comment.Create(Alice, "x", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Assert.NotEqual(a.Id, b.Id);
        Assert.False(string.IsNullOrWhiteSpace(a.Id));
        Assert.Equal(2020, b.CreatedAt.Year);
        Assert.True(a.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void Author_BlankName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CommentAuthor(""));
        Assert.Throws<ArgumentNullException>(() => new CommentAuthor(null!));
    }

    [Fact]
    public void AddComment_Duplicate_Throws()
    {
        CommentThread t = NewThread(out _);
        Comment c = Comment.Create(Alice, "x");
        t.AddComment(c);
        Assert.Throws<ArgumentException>(() => t.AddComment(c));
        Assert.Throws<ArgumentNullException>(() => t.AddComment(null!));
    }

    [Fact]
    public void AddComment_RaisesCommentAdded()
    {
        CommentThread t = NewThread(out var events);
        Comment c = Comment.Create(Alice, "x");
        t.AddComment(c);
        CommentThreadChangedEventArgs e = Assert.Single(events);
        Assert.Equal(CommentThreadChangeKind.CommentAdded, e.Kind);
        Assert.Same(c, e.Comment);
        Assert.Single(t.Comments);
    }

    [Fact]
    public void RemoveComment_Unknown_ReturnsFalse()
    {
        CommentThread t = NewThread(out var events);
        Assert.False(t.RemoveComment("nope"));
        Assert.Empty(events);
    }

    [Fact]
    public void RemoveComment_RaisesCommentRemoved()
    {
        CommentThread t = NewThread(out var events);
        Comment c = Comment.Create(Alice, "x");
        t.AddComment(c);
        events.Clear();
        Assert.True(t.RemoveComment(c.Id));
        Assert.Equal(CommentThreadChangeKind.CommentRemoved, Assert.Single(events).Kind);
        Assert.Same(c, events[0].Comment);
        Assert.Empty(t.Comments);
    }

    [Fact]
    public void ReplaceComment_KeepsOrder()
    {
        CommentThread t = NewThread(out var events);
        Comment a = Comment.Create(Alice, "a");
        Comment b = Comment.Create(Alice, "b");
        t.AddComment(a);
        t.AddComment(b);
        events.Clear();
        Comment a2 = a with { Body = "a2" };
        Assert.True(t.ReplaceComment(a2));
        Assert.Same(a2, t.Comments[0]);
        Assert.Same(b, t.Comments[1]);
        Assert.Equal(CommentThreadChangeKind.CommentReplaced, Assert.Single(events).Kind);
        Assert.Same(a2, events[0].Comment);
    }

    [Fact]
    public void ReplaceComment_Unknown_ReturnsFalse()
    {
        CommentThread t = NewThread(out var events);
        Assert.False(t.ReplaceComment(Comment.Create(Alice, "x")));
        Assert.Empty(events);
    }

    [Fact]
    public void State_SameValue_RaisesNothing()
    {
        CommentThread t = NewThread(out var events);
        t.State = CommentThreadState.Open;
        Assert.Empty(events);
    }

    [Fact]
    public void State_Changed_RaisesStateChanged()
    {
        CommentThread t = NewThread(out var events);
        t.State = CommentThreadState.Resolved;
        Assert.Equal(CommentThreadChangeKind.StateChanged, Assert.Single(events).Kind);
        Assert.Null(events[0].Comment);
    }

    [Fact]
    public void Title_Changed_RaisesTitleChanged()
    {
        CommentThread t = NewThread(out var events);
        t.Title = "Hello";
        t.Title = "Hello";
        Assert.Equal(CommentThreadChangeKind.TitleChanged, Assert.Single(events).Kind);
        Assert.Equal("Hello", t.Title);
    }

    [Fact]
    public void LineNumber_Changed_RaisesLineChanged()
    {
        CommentThread t = NewThread(out var events);
        t.LineNumber = 3;
        Assert.Empty(events);
        t.LineNumber = 5;
        Assert.Equal(CommentThreadChangeKind.LineChanged, Assert.Single(events).Kind);
        Assert.Equal(5, t.LineNumber);
    }

    [Fact]
    public void Constructor_LineZero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CommentThread("t", 0));
    }

    [Fact]
    public void Comments_IsNotModifiableByCaller()
    {
        CommentThread t = NewThread(out _);
        t.AddComment(Comment.Create(Alice, "x"));
        Assert.IsNotType<List<Comment>>(t.Comments);
        Assert.True(((System.Collections.IList)t.Comments).IsReadOnly);
        Assert.Same(t.Comments, t.Comments);
    }
}

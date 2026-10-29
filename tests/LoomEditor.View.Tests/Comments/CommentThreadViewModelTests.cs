using LoomEditor.Core.Comments;
using LoomEditor.View.Comments;

namespace LoomEditor.View.Tests.Comments;

public class CommentThreadViewModelTests
{
    private static readonly CommentAuthor Me = new("Me");

    private static (CommentThread Thread, CommentThreadViewModel Vm) Make(string? title = null, int line = 3, string text = "code")
    {
        var t = new CommentThread("t", line, title);
        return (t, new CommentThreadViewModel(t, () => text));
    }

    private static Comment C(string id = "c") => new(id, Me, "hi", DateTimeOffset.UnixEpoch);

    [Fact]
    public void Title_NoTitle_UsesLine() => Assert.Equal("Comment on line 3", Make().Vm.Title);

    [Fact]
    public void Title_Set_UsesIt() => Assert.Equal("T", Make("T").Vm.Title);

    [Fact]
    public void Title_LineMoved_RaisesChanged()
    {
        var (t, vm) = Make();
        int n = 0;
        vm.Changed += (_, _) => n++;
        t.LineNumber = 7;
        Assert.Equal(1, n);
        Assert.Equal("Comment on line 7", vm.Title);
    }

    [Fact]
    public void CanSubmit_BlankDraft_False()
    {
        var (_, vm) = Make();
        vm.StartComposing();
        vm.SetDraft("  \n ");
        Assert.False(vm.CanSubmit);
    }

    [Fact]
    public void CanSubmit_NotComposing_False()
    {
        var (_, vm) = Make();
        vm.SetDraft("x");
        Assert.False(vm.CanSubmit);
    }

    [Fact]
    public void SetDraft_NormalizesCr()
    {
        var (_, vm) = Make();
        vm.SetDraft("a\r\nb\rc");
        Assert.Equal("a\nb\nc", vm.Draft);
    }

    [Fact]
    public void SetDraft_Same_RaisesNothing()
    {
        var (_, vm) = Make();
        vm.SetDraft("a");
        int n = 0;
        vm.Changed += (_, _) => n++;
        vm.SetDraft("a");
        Assert.Equal(0, n);
    }

    [Fact]
    public void Submit_Blank_ReturnsNullAndRaisesNothing()
    {
        var (_, vm) = Make();
        vm.StartComposing();
        int n = 0;
        vm.SubmitRequested += (_, _) => n++;
        Assert.Null(vm.Submit());
        Assert.Equal(0, n);
    }

    [Fact]
    public void Submit_KeepsDraft()
    {
        var (_, vm) = Make();
        vm.StartComposing();
        vm.SetDraft("x");
        string? got = null;
        vm.SubmitRequested += (_, s) => got = s;
        Assert.Equal("x", vm.Submit());
        Assert.Equal("x", got);
        Assert.Equal("x", vm.Draft);
        Assert.True(vm.IsComposing);
    }

    [Fact]
    public void Submit_TrimsTrailingWhitespace()
    {
        var (_, vm) = Make();
        vm.StartComposing();
        vm.SetDraft("  x y \n\n");
        Assert.Equal("  x y", vm.Submit());
    }

    [Fact]
    public void CommentAdded_WhileComposing_ClearsDraftAndCloses()
    {
        var (t, vm) = Make();
        vm.StartComposing();
        vm.SetDraft("x");
        vm.TogglePreview();
        t.AddComment(C());
        Assert.Equal("", vm.Draft);
        Assert.False(vm.IsComposing);
        Assert.False(vm.IsPreview);
    }

    [Fact]
    public void CommentAdded_NotComposing_KeepsState()
    {
        var (t, vm) = Make();
        vm.SetDraft("x");
        t.AddComment(C());
        Assert.Equal("x", vm.Draft);
    }

    [Fact]
    public void Cancel_NoComments_RemoveThread()
    {
        var (_, vm) = Make();
        vm.StartComposing();
        CancelOutcome? raised = null;
        vm.CancelRequested += (_, o) => raised = o;
        Assert.Equal(CancelOutcome.RemoveThread, vm.Cancel());
        Assert.Equal(CancelOutcome.RemoveThread, raised);
    }

    [Fact]
    public void Cancel_WithComments_Closed()
    {
        var (t, vm) = Make();
        t.AddComment(C());
        vm.StartComposing();
        Assert.Equal(CancelOutcome.Closed, vm.Cancel());
    }

    [Fact]
    public void Cancel_DiscardsDraftAndPreview()
    {
        var (_, vm) = Make();
        vm.StartComposing();
        vm.SetDraft("x");
        vm.TogglePreview();
        vm.Cancel();
        Assert.Equal("", vm.Draft);
        Assert.False(vm.IsComposing);
        Assert.False(vm.IsPreview);
    }

    [Fact]
    public void TryEscape_EmptyDraft_Cancels()
    {
        var (_, vm) = Make();
        vm.StartComposing();
        Assert.True(vm.TryEscape());
        Assert.False(vm.IsComposing);
    }

    [Fact]
    public void TryEscape_WhitespaceDraft_Cancels()
    {
        var (_, vm) = Make();
        vm.StartComposing();
        vm.SetDraft(" \n");
        Assert.True(vm.TryEscape());
        Assert.Equal("", vm.Draft);
    }

    [Fact]
    public void TryEscape_NonBlankDraft_DoesNothing()
    {
        var (_, vm) = Make();
        vm.StartComposing();
        vm.SetDraft("x");
        Assert.False(vm.TryEscape());
        Assert.True(vm.IsComposing);
        Assert.Equal("x", vm.Draft);
    }

    [Fact]
    public void TryEscape_NotComposing_False() => Assert.False(Make().Vm.TryEscape());

    [Fact]
    public void Resolved_IsCollapsed()
    {
        var (t, vm) = Make();
        t.State = CommentThreadState.Resolved;
        Assert.True(vm.IsCollapsed);
    }

    [Fact]
    public void ToggleCollapsed_Resolved_Expands()
    {
        var (t, vm) = Make();
        t.State = CommentThreadState.Resolved;
        vm.ToggleCollapsed();
        Assert.False(vm.IsCollapsed);
        vm.ToggleCollapsed();
        Assert.True(vm.IsCollapsed);
    }

    [Fact]
    public void ToggleCollapsed_Open_Ignored()
    {
        var (_, vm) = Make();
        vm.ToggleCollapsed();
        Assert.False(vm.IsCollapsed);
    }

    [Fact]
    public void StateChange_ResetsExpanded()
    {
        var (t, vm) = Make();
        t.State = CommentThreadState.Resolved;
        vm.ToggleCollapsed();
        t.State = CommentThreadState.Open;
        t.State = CommentThreadState.Resolved;
        Assert.True(vm.IsCollapsed);
    }

    [Fact]
    public void StartComposing_ExpandsCollapsed()
    {
        var (t, vm) = Make();
        t.State = CommentThreadState.Resolved;
        vm.StartComposing();
        Assert.False(vm.IsCollapsed);
    }

    [Fact]
    public void TogglePreview_NotComposing_Ignored()
    {
        var (_, vm) = Make();
        vm.TogglePreview();
        Assert.False(vm.IsPreview);
    }

    [Fact]
    public void ReplyVisible_ResolvedThread_False()
    {
        var (t, vm) = Make();
        Assert.True(vm.ReplyVisible);
        t.State = CommentThreadState.Resolved;
        Assert.False(vm.ReplyVisible);
    }

    [Fact]
    public void SuggestionSeed_ReadsCurrentLine()
    {
        string line = "a";
        var vm = new CommentThreadViewModel(new CommentThread("t", 1), () => line);
        line = "b";
        Assert.Equal("b", vm.SuggestionSeed);
    }

    [Fact]
    public void Dispose_Unsubscribes()
    {
        var (t, vm) = Make();
        int n = 0;
        vm.Changed += (_, _) => n++;
        vm.Dispose();
        vm.Dispose();
        t.LineNumber = 9;
        Assert.Equal(0, n);
    }
}

using LoomEditor.Core.Text;
using LoomEditor.View.Comments;
using LoomEditor.View.Features;
using LoomEditor.View.Features.CommentThreads;

namespace LoomEditor.View.Tests.Features.CommentThreads;

public class CommentThreadsFeatureTests
{
    private static EditorContext Attached(CommentThreadsFeature feature)
    {
        var context = new EditorContext();
        context.SetText("one\ntwo\nthree");
        feature.Attach(context);
        return context;
    }

    [Fact]
    public void AddCommand_RaisesAddRequestedForCursorLine()
    {
        using var feature = new CommentThreadsFeature();
        EditorContext context = Attached(feature);
        context.CommentThreads = new CommentThreadsController(context);
        context.Selection = Selection.Collapsed(new Position(3, 2));
        int? line = null;
        context.CommentThreads.AddCommentRequested += (_, l) => line = l.LineNumber;

        Assert.True(context.Commands.Execute(CommentThreadsCommands.Add));

        Assert.Equal(3, line);
    }

    [Fact]
    public void AddCommand_MultiLineSelection_RequestsRange()
    {
        using var feature = new CommentThreadsFeature();
        EditorContext context = Attached(feature);
        context.CommentThreads = new CommentThreadsController(context);
        context.Selection = new Selection(1, 2, 3, 2);
        (int Start, int End)? lines = null;
        context.CommentThreads.AddCommentRequested += (_, l) => lines = (l.StartLineNumber, l.LineNumber);

        context.Commands.Execute(CommentThreadsCommands.Add);

        Assert.Equal((1, 3), lines);
    }

    [Fact]
    public void AddCommand_EmptySelection_RequestsCursorLine()
    {
        using var feature = new CommentThreadsFeature();
        EditorContext context = Attached(feature);
        context.CommentThreads = new CommentThreadsController(context);
        context.Selection = Selection.Collapsed(new Position(2, 1));
        (int Start, int End)? lines = null;
        context.CommentThreads.AddCommentRequested += (_, l) => lines = (l.StartLineNumber, l.LineNumber);

        context.Commands.Execute(CommentThreadsCommands.Add);

        Assert.Equal((2, 2), lines);
    }

    [Fact]
    public void AddCommand_NoController_DoesNothing()
    {
        using var feature = new CommentThreadsFeature();
        EditorContext context = Attached(feature);

        Assert.True(context.Commands.Execute(CommentThreadsCommands.Add));
    }

    [Fact]
    public void AddCommand_ReadOnly_StillRaises()
    {
        using var feature = new CommentThreadsFeature();
        EditorContext context = Attached(feature);
        context.CommentThreads = new CommentThreadsController(context);
        context.IsReadOnly = true;
        int? line = null;
        context.CommentThreads.AddCommentRequested += (_, l) => line = l.LineNumber;

        context.Commands.Execute(CommentThreadsCommands.Add);

        Assert.Equal(1, line);
    }
}

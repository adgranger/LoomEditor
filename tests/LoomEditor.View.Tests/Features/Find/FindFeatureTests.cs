using LoomEditor.View.Features;
using LoomEditor.View.Features.Find;

namespace LoomEditor.View.Tests.Features.Find;

public class FindFeatureTests
{
    [Fact]
    public void Attach_RegistersTheFindCommands_DisposeRemovesThemAndTheController()
    {
        var context = new EditorContext();
        string[] ids =
        [
            FindCommands.StartFindAction, FindCommands.StartFindWithSelection, FindCommands.NextMatchFindAction,
            FindCommands.PreviousMatchFindAction, FindCommands.NextSelectionMatchFindAction,
            FindCommands.PreviousSelectionMatchFindAction, FindCommands.StartFindReplaceAction, FindCommands.CloseFindWidgetCommand,
            FindCommands.ToggleCaseSensitiveCommand, FindCommands.ToggleWholeWordCommand, FindCommands.ToggleRegexCommand,
            FindCommands.ToggleSearchScopeCommand, FindCommands.TogglePreserveCaseCommand, FindCommands.ReplaceOneAction,
            FindCommands.ReplaceAllAction, FindCommands.SelectAllMatchesAction,
        ];

        var feature = new FindFeature();
        feature.Attach(context);

        Assert.NotNull(context.FindController);
        Assert.All(ids, id => Assert.True(context.Commands.Contains(id), id));

        feature.Dispose();

        Assert.Null(context.FindController);
        Assert.All(ids, id => Assert.False(context.Commands.Contains(id), id));
    }

    [Fact]
    public void StartFind_ThroughCommandRegistry_RevealsTheState()
    {
        var context = new EditorContext();
        using var host = new EditorFeatureHost(context, [() => new FindFeature()]);

        Assert.True(context.Commands.Execute(FindCommands.StartFindAction));

        Assert.True(context.FindController!.State.IsRevealed);
    }

    [Fact]
    public void CloseFindWidget_WidgetHidden_CannotExecute()
    {
        var context = new EditorContext();
        using var host = new EditorFeatureHost(context, [() => new FindFeature()]);

        Assert.False(context.Commands.Execute(FindCommands.CloseFindWidgetCommand));

        context.Commands.Execute(FindCommands.StartFindAction);

        Assert.True(context.Commands.Execute(FindCommands.CloseFindWidgetCommand));
        Assert.False(context.FindController!.State.IsRevealed);
    }
}

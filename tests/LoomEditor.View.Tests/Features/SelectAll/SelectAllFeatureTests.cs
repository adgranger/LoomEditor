using LoomEditor.Core.Text;
using LoomEditor.View.Features;
using LoomEditor.View.Features.SelectAll;

namespace LoomEditor.View.Tests.Features.SelectAll;

public class SelectAllFeatureTests
{
    [Fact]
    public void ComputeSelection_SelectsFromStartToEndOfLastLine()
    {
        Selection selection = SelectAllFeature.ComputeSelection(["first", "second", "end"]);

        Assert.Equal(new Selection(1, 1, 3, 4), selection);
    }

    [Fact]
    public void ComputeSelection_EmptyDocument_ReturnsCaretAtStart()
    {
        Assert.Equal(Selection.Collapsed(new Position(1, 1)), SelectAllFeature.ComputeSelection([""]));
    }

    [Fact]
    public void Attach_RegistersCommand_ThatSelectsEverything()
    {
        var context = new EditorContext();
        context.SetText("hello\nworld!");
        using var feature = new SelectAllFeature();

        feature.Attach(context);
        bool executed = context.Commands.Execute(SelectAllCommands.SelectAll);

        Assert.True(executed);
        Assert.Equal(new Selection(1, 1, 2, 7), context.Selection);
    }

    [Fact]
    public void Dispose_UnregistersCommand()
    {
        var context = new EditorContext();
        var feature = new SelectAllFeature();
        feature.Attach(context);

        feature.Dispose();

        Assert.False(context.Commands.Contains(SelectAllCommands.SelectAll));
    }

    [Fact]
    public void DefaultFeatures_ContainSelectAll()
    {
        var context = new EditorContext();

        using var host = new EditorFeatureHost(context, EditorFeatures.Default);

        Assert.Contains(host.Features, feature => feature is SelectAllFeature);
        Assert.True(context.Commands.Contains(SelectAllCommands.SelectAll));
    }
}

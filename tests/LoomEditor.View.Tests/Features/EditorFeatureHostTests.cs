using LoomEditor.View.Features;
using LoomEditor.View.Features.SelectAll;

namespace LoomEditor.View.Tests.Features;

public class EditorFeatureHostTests
{
    [Fact]
    public void SameFeatureTwice_Throws_AndDisposesAttachedFeatures()
    {
        var context = new EditorContext();

        Assert.Throws<InvalidOperationException>(() =>
            new EditorFeatureHost(context, [() => new SelectAllFeature(), () => new SelectAllFeature()]));

        Assert.Empty(context.Commands.Ids);
    }

    [Fact]
    public void Dispose_DisposesAllFeatures()
    {
        var context = new EditorContext();
        var host = new EditorFeatureHost(context, EditorFeatures.Default);

        host.Dispose();

        Assert.Empty(host.Features);
        Assert.Empty(context.Commands.Ids);
    }

    [Fact]
    public void SetText_SplitsLines_AndResetsSelection()
    {
        var context = new EditorContext();
        int selectionChanges = 0;
        context.Selection = new Core.Text.Selection(1, 1, 1, 2);
        context.SelectionChanged += (_, _) => selectionChanges++;

        context.SetText("a\r\nb\nc\rd");

        Assert.Equal(4, context.Model.LineCount);
        Assert.Equal("c", context.Model.GetLineContent(3));
        Assert.Equal(1, selectionChanges);
    }
}

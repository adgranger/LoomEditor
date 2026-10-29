using LoomEditor.View.Features;
using LoomEditor.View.Features.FontZoom;

namespace LoomEditor.View.Tests.Features.FontZoom;

public class FontZoomFeatureTests
{
    private static (EditorContext Context, FontZoomFeature Feature) Create()
    {
        var context = new EditorContext();
        var feature = new FontZoomFeature();
        feature.Attach(context);
        return (context, feature);
    }

    [Fact]
    public void FontZoomIn_IncrementsLevel()
    {
        (EditorContext context, FontZoomFeature feature) = Create();
        using (feature)
        {
            context.Commands.Execute(FontZoomCommands.FontZoomIn);
            context.Commands.Execute(FontZoomCommands.FontZoomIn);
            Assert.Equal(2, context.Zoom.Level);
        }
    }

    [Fact]
    public void FontZoomOut_DecrementsLevel()
    {
        (EditorContext context, FontZoomFeature feature) = Create();
        using (feature)
        {
            context.Commands.Execute(FontZoomCommands.FontZoomOut);
            Assert.Equal(-1, context.Zoom.Level);
        }
    }

    [Fact]
    public void FontZoomReset_SetsZero()
    {
        (EditorContext context, FontZoomFeature feature) = Create();
        using (feature)
        {
            context.Zoom.SetLevel(7);
            context.Commands.Execute(FontZoomCommands.FontZoomReset);
            Assert.Equal(0, context.Zoom.Level);
        }
    }

    [Fact]
    public void FontZoomIn_AtMax_StaysAtMax()
    {
        (EditorContext context, FontZoomFeature feature) = Create();
        using (feature)
        {
            context.Zoom.SetLevel(20);
            context.Commands.Execute(FontZoomCommands.FontZoomIn);
            Assert.Equal(20, context.Zoom.Level);
        }
    }

    [Fact]
    public void Feature_DoesNotRegisterKeybindings()
    {
        // The feature only registers commands: the registry has no key concept and the three ids are the only additions.
        var context = new EditorContext();
        var before = new HashSet<string>(context.Commands.Ids);
        using var feature = new FontZoomFeature();

        feature.Attach(context);

        Assert.Equal(
            [FontZoomCommands.FontZoomIn, FontZoomCommands.FontZoomOut, FontZoomCommands.FontZoomReset],
            context.Commands.Ids.Where(id => !before.Contains(id)).Order().ToArray());
    }
}

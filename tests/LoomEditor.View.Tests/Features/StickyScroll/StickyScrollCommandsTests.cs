using LoomEditor.View.Features;
using LoomEditor.View.Features.StickyScroll;
using LoomEditor.View.Tests.Features.TestHelpers;

namespace LoomEditor.View.Tests.Features.StickyScroll;

// New tests: ToggleStickyScroll has no upstream test.
public class StickyScrollCommandsTests
{
    [Fact]
    public void ToggleStickyScroll_DisablesThenEnables()
    {
        var context = new EditorContext(new ManualEditorScheduler());
        context.SetText("a\n  b");
        var feature = new StickyScrollFeature();
        feature.Attach(context);

        Assert.True(context.Commands.Execute(StickyScrollCommands.ToggleStickyScroll));
        Assert.False(context.ViewModel.LayoutOptions.StickyScroll.Enabled);

        Assert.True(context.Commands.Execute(StickyScrollCommands.ToggleStickyScroll));
        Assert.True(context.ViewModel.LayoutOptions.StickyScroll.Enabled);
        feature.Dispose();
    }
}

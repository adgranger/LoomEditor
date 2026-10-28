using LoomEditor.View.Commands;
using LoomEditor.View.DiffEditor;
using LoomEditor.View.Features;
using LoomEditor.View.Features.DiffNavigation;

namespace LoomEditor.View.Tests.Features.DiffNavigation;

// New tests: the keys have no Monaco counterpart (VS Code's workbench binds them).
public class DiffNavigationFeatureTests
{
    private sealed class FakeTarget : IDiffNavigationTarget
    {
        public List<NavigationDirection> Calls { get; } = [];

        public void GoToChange(NavigationDirection direction) => Calls.Add(direction);

        public int ViewerNext { get; private set; }

        public int ViewerPrevious { get; private set; }

        public void AccessibleDiffViewerNext() => ViewerNext++;

        public void AccessibleDiffViewerPrevious() => ViewerPrevious++;
    }

    private static string? Resolve(KeyModifiers modifiers, KeybindingContext context) =>
        KeybindingResolver.Default.Resolve(new Keybinding(modifiers, KeyCode.F5), context);

    [Fact]
    public void Command_NextAndPrevious_InvokeTheTarget()
    {
        var context = new EditorContext();
        var target = new FakeTarget();
        using var feature = new DiffNavigationFeature { Target = target };
        feature.Attach(context);

        Assert.True(context.Commands.Execute(DiffNavigationCommands.NextChange));
        Assert.True(context.Commands.Execute(DiffNavigationCommands.PreviousChange));

        Assert.Equal([NavigationDirection.Next, NavigationDirection.Previous], target.Calls);
    }

    [Fact]
    public void Keybinding_AltF5_ResolvesWhenInDiffEditor()
    {
        var context = new KeybindingContext(EditorTextFocus: true, IsInDiffEditor: true);

        Assert.Equal(DiffNavigationCommands.NextChange, Resolve(KeyModifiers.Alt, context));
        Assert.Equal(DiffNavigationCommands.PreviousChange, Resolve(KeyModifiers.Shift | KeyModifiers.Alt, context));
    }

    [Fact]
    public void Keybinding_AltF5_NotResolvedOutsideDiffEditor()
    {
        var context = new KeybindingContext(EditorTextFocus: true);

        Assert.Null(Resolve(KeyModifiers.Alt, context));
        Assert.Null(Resolve(KeyModifiers.Shift | KeyModifiers.Alt, context));
    }
}

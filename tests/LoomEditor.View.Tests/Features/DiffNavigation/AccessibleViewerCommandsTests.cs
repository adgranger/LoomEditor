using LoomEditor.View.Commands;
using LoomEditor.View.Features;
using LoomEditor.View.Features.DiffNavigation;

namespace LoomEditor.View.Tests.Features.DiffNavigation;

// Monaco: commands.ts (AccessibleDiffViewerNext, AccessibleDiffViewerPrev) has no test; these are new.
public class AccessibleViewerCommandsTests
{
    private sealed class FakeTarget : IDiffNavigationTarget
    {
        public int Next { get; private set; }

        public int Previous { get; private set; }

        public void GoToChange(LoomEditor.View.DiffEditor.NavigationDirection direction)
        {
        }

        public void AccessibleDiffViewerNext() => Next++;

        public void AccessibleDiffViewerPrevious() => Previous++;
    }

    private static string? Resolve(KeyModifiers modifiers, KeybindingContext context) =>
        KeybindingResolver.Default.Resolve(new Keybinding(modifiers, KeyCode.F7), context);

    [Fact]
    public void Command_F7_ResolvesInDiffEditor() =>
        Assert.Equal(DiffNavigationCommands.AccessibleViewerNext, Resolve(KeyModifiers.None, new KeybindingContext(EditorTextFocus: true, IsInDiffEditor: true)));

    [Fact]
    public void Command_ShiftF7_ResolvesInDiffEditor() =>
        Assert.Equal(DiffNavigationCommands.AccessibleViewerPrevious, Resolve(KeyModifiers.Shift, new KeybindingContext(EditorTextFocus: true, IsInDiffEditor: true)));

    [Fact]
    public void Command_F7_NotResolvedOutsideDiffEditor()
    {
        // Outside a diff editor F7 still goes through the word highlights.
        var context = new KeybindingContext(EditorTextFocus: true);

        Assert.NotEqual(DiffNavigationCommands.AccessibleViewerNext, Resolve(KeyModifiers.None, context));
        Assert.NotEqual(DiffNavigationCommands.AccessibleViewerPrevious, Resolve(KeyModifiers.Shift, context));
    }

    [Fact]
    public void Commands_InvokeTheTarget()
    {
        var context = new EditorContext();
        var target = new FakeTarget();
        using var feature = new DiffNavigationFeature { Target = target };
        feature.Attach(context);

        context.Commands.Execute(DiffNavigationCommands.AccessibleViewerNext);
        context.Commands.Execute(DiffNavigationCommands.AccessibleViewerPrevious);

        Assert.Equal((1, 1), (target.Next, target.Previous));
    }
}

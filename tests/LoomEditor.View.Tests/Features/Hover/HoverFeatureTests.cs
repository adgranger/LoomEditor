using LoomEditor.Core.Markers;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Hover;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Tests.Features.TestHelpers;
using LoomEditor.View.Tests.Hover.TestHelpers;

namespace LoomEditor.View.Tests.Features.Hover;

// New tests: Monaco has no test of ShowOrFocusHoverAction.
public class HoverFeatureTests
{
    private readonly ManualEditorScheduler _scheduler = new();
    private readonly FakeHoverPresenter _presenter = new();

    private (EditorContext Context, HoverFeature Feature) Create()
    {
        var context = new EditorContext(_scheduler);
        context.SetText("let value = 1;\nsecond");
        context.HoverPresenter = _presenter;
        context.Markers.Change("o", [new MarkerData(MarkerSeverity.Error, "bad", 1, 5, 1, 10)]);
        var feature = new HoverFeature();
        feature.Attach(context);
        return (context, feature);
    }

    [Fact]
    public void Attach_RegistersTheCommands()
    {
        (EditorContext context, _) = Create();

        Assert.True(context.Commands.Contains(HoverCommands.ShowHover));
        Assert.True(context.Commands.Contains(HoverCommands.HideHover));
    }

    [Fact]
    public void ShowHover_ShowsAtTheCursor()
    {
        (EditorContext context, _) = Create();
        context.SetSelections([Selection.Collapsed(new Position(1, 6))]);

        Assert.True(context.Commands.Execute(HoverCommands.ShowHover));

        Assert.Equal(1, _presenter.ShowCount);
        Assert.IsType<LoomEditor.View.Hover.MarkerHoverPart>(Assert.Single(_presenter.Last!.Parts));
    }

    [Fact]
    public void HideHover_Hides()
    {
        (EditorContext context, _) = Create();
        context.SetSelections([Selection.Collapsed(new Position(1, 6))]);
        context.Commands.Execute(HoverCommands.ShowHover);

        context.Commands.Execute(HoverCommands.HideHover);

        Assert.False(_presenter.IsVisible);
    }

    [Fact]
    public void Dispose_UnregistersTheCommands()
    {
        (EditorContext context, HoverFeature feature) = Create();

        feature.Dispose();

        Assert.False(context.Commands.Contains(HoverCommands.ShowHover));
        Assert.False(context.Commands.Contains(HoverCommands.HideHover));
    }

    [Fact]
    public void NonEditorContext_AttachDoesNothing()
    {
        var commands = new CommandRegistry();

        new HoverFeature().Attach(new FakeContext(commands));

        Assert.False(commands.Contains(HoverCommands.ShowHover));
    }

    [Fact]
    public void DefaultKeybindings_HaveTheHoverRules()
    {
        KeybindingResolver resolver = KeybindingResolver.Default;
        var ctrlK = new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyK);
        var ctrlI = new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyI);
        var escape = new Keybinding(KeyModifiers.None, KeyCode.Escape);
        var context = new KeybindingContext(EditorTextFocus: true, EditorFocus: true);

        Assert.Equal(KeybindingResolutionKind.MoreChordsNeeded, resolver.Resolve(context, null, ctrlK).Kind);
        Assert.Equal(HoverCommands.ShowHover, resolver.Resolve(context, ctrlK, ctrlI).CommandId);
        Assert.Equal(HoverCommands.HideHover, resolver.Resolve(context with { HoverVisible = true }, null, escape).CommandId);
        Assert.NotEqual(HoverCommands.HideHover, resolver.Resolve(context, null, escape).CommandId);
    }

    private sealed class FakeContext(CommandRegistry commands) : IEditorContext
    {
        public IEditorScheduler Scheduler => throw new NotSupportedException();

        public CommandRegistry Commands { get; } = commands;

        public TextModel Model => throw new NotSupportedException();

        public Selection Selection
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public IReadOnlyList<Selection> Selections => throw new NotSupportedException();

        public event EventHandler? SelectionChanged
        {
            add { }
            remove { }
        }

        public void SetSelections(IReadOnlyList<Selection> selections) => throw new NotSupportedException();
    }
}

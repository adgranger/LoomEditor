using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Accessibility;
using LoomEditor.View.Commands;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Features.TabFocus;
using LoomEditor.View.Tests.Accessibility.TestHelpers;

namespace LoomEditor.View.Tests.Features.TabFocus;

// New tests: toggleTabFocusMode.ts has no upstream test.
public class TabFocusFeatureTests
{
    private static (EditorContext Context, TabFocusFeature Feature, RecordingAnnouncer Recorder) Create()
    {
        var recorder = new RecordingAnnouncer();
        var context = new EditorContext { Announcer = recorder };
        var feature = new TabFocusFeature();
        feature.Attach(context);
        return (context, feature, recorder);
    }

    [Fact]
    public void Toggle_FlipsTheContextFlag()
    {
        (EditorContext context, _, _) = Create();

        context.Commands.Execute(TabFocusCommands.ToggleTabFocusMode);
        Assert.True(context.TabMovesFocus);
        context.Commands.Execute(TabFocusCommands.ToggleTabFocusMode);
        Assert.False(context.TabMovesFocus);
    }

    [Fact]
    public void Toggle_On_AnnouncesTheMessage()
    {
        (EditorContext context, _, RecordingAnnouncer recorder) = Create();

        context.Commands.Execute(TabFocusCommands.ToggleTabFocusMode);

        Assert.Equal([AccessibilityMessages.TabMovesFocusOn], recorder.Messages);
    }

    [Fact]
    public void Toggle_Off_AnnouncesTheMessage()
    {
        (EditorContext context, _, RecordingAnnouncer recorder) = Create();
        context.TabMovesFocus = true;

        context.Commands.Execute(TabFocusCommands.ToggleTabFocusMode);

        Assert.Equal([AccessibilityMessages.TabMovesFocusOff], recorder.Messages);
    }

    [Fact]
    public void TabMovesFocusChanged_RaisedOncePerChange()
    {
        (EditorContext context, _, _) = Create();
        int raised = 0;
        context.TabMovesFocusChanged += (_, _) => raised++;

        context.TabMovesFocus = true;
        context.TabMovesFocus = true;
        context.TabMovesFocus = false;

        Assert.Equal(2, raised);
    }

    [Fact]
    public void Command_IsRegisteredAndEnabledWithoutAnyContext()
    {
        (EditorContext context, _, _) = Create();

        Assert.True(context.Commands.Contains(TabFocusCommands.ToggleTabFocusMode));
        Assert.True(context.Commands.Execute(TabFocusCommands.ToggleTabFocusMode));
    }

    [Fact]
    public void Attach_WithANonEditorContext_DoesNothing()
    {
        var commands = new CommandRegistry();
        var fake = new FakeContext(commands);

        new TabFocusFeature().Attach(fake);

        Assert.False(commands.Contains(TabFocusCommands.ToggleTabFocusMode));
    }

    [Fact]
    public void Dispose_UnregistersTheCommand()
    {
        (EditorContext context, TabFocusFeature feature, _) = Create();

        feature.Dispose();

        Assert.False(context.Commands.Contains(TabFocusCommands.ToggleTabFocusMode));
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

        public bool IsReadOnly { get => false; set => throw new NotSupportedException(); }

        public event EventHandler? ReadOnlyChanged { add { } remove { } }

        public event EventHandler? SelectionChanged
        {
            add { }
            remove { }
        }

        public void SetSelections(IReadOnlyList<Selection> selections) => throw new NotSupportedException();
    }
}

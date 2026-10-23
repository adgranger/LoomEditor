using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Accessibility;
using LoomEditor.View.Commands;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Tests.Accessibility.TestHelpers;

namespace LoomEditor.View.Tests.Accessibility;

public class EditorAnnouncerTests
{
    private sealed class FakeContext : IEditorContext
    {
        public IEditorScheduler Scheduler => throw new NotSupportedException();

        public CommandRegistry Commands => throw new NotSupportedException();

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

    [Fact]
    public void GetAnnouncer_DefaultContext_IsTheNullAnnouncer()
    {
        using var context = new EditorContext();
        Assert.Same(NullEditorAnnouncer.Instance, context.GetAnnouncer());
    }

    [Fact]
    public void GetAnnouncer_ReturnsTheAssignedAnnouncer()
    {
        using var context = new EditorContext();
        var recorder = new RecordingAnnouncer();
        context.Announcer = recorder;
        Assert.Same(recorder, context.GetAnnouncer());
    }

    [Fact]
    public void NullAnnouncer_DoesNothing() => NullEditorAnnouncer.Instance.Announce("x");

    [Fact]
    public void GetAnnouncer_NonEditorContext_IsTheNullAnnouncer() =>
        Assert.Same(NullEditorAnnouncer.Instance, new FakeContext().GetAnnouncer());

    [Fact]
    public void ContextAnnouncer_ForwardsToTheCurrentAnnouncer()
    {
        using var context = new EditorContext();
        var forwarder = new ContextAnnouncer(context);
        var recorder = new RecordingAnnouncer();
        context.Announcer = recorder;
        forwarder.Announce("hello");
        Assert.Equal(["hello"], recorder.Messages);
    }

    [Fact]
    public void ContextAnnouncer_FollowsAReplacedAnnouncer()
    {
        using var context = new EditorContext();
        var forwarder = new ContextAnnouncer(context);
        var first = new RecordingAnnouncer();
        var second = new RecordingAnnouncer();
        context.Announcer = first;
        forwarder.Announce("a");
        context.Announcer = second;
        forwarder.Announce("b");
        Assert.Equal(["a"], first.Messages);
        Assert.Equal(["b"], second.Messages);
    }
}

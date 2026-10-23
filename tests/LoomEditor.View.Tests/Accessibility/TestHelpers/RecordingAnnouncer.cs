using LoomEditor.View.Accessibility;

namespace LoomEditor.View.Tests.Accessibility.TestHelpers;

internal sealed class RecordingAnnouncer : IEditorAnnouncer
{
    public List<string> Messages { get; } = [];

    public void Announce(string message) => Messages.Add(message);
}

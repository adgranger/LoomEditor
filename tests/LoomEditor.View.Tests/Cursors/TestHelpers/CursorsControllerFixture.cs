using LoomEditor.Core.Model;
using LoomEditor.View.Cursors;
using LoomEditor.View.Events;

namespace LoomEditor.View.Tests.Cursors.TestHelpers;

/// <summary>A model, a <see cref="CursorsController"/> wired to its content changes, and the events it collected.</summary>
internal sealed class CursorsControllerFixture
{
    public CursorsControllerFixture(string text, EditorCursorOptions? options = null)
    {
        Model = new TextModel(text);
        CursorContext context = CursorTestContext.Create(Model, options);
        Controller = new CursorsController(Model, context.ViewModel, context.CoordinatesConverter, context.CursorConfig);
        Model.RawContentChanged += (_, e) => Controller.OnModelContentChanged(Collector, e);
    }

    public TextModel Model { get; }

    public CursorsController Controller { get; }

    public ViewModelEventsCollector Collector { get; private set; } = new();

    public void ResetEvents() => Collector = new ViewModelEventsCollector();

    public IEnumerable<CursorStateChangedEvent> Outgoing => Collector.OutgoingEvents.OfType<CursorStateChangedEvent>();

    public IEnumerable<ViewCursorStateChangedEvent> ViewState => Collector.ViewEvents.OfType<ViewCursorStateChangedEvent>();
}

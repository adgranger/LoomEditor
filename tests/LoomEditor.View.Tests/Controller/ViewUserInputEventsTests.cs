using LoomEditor.Core.Text;
using LoomEditor.View.Controller;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Controller;

// New tests: Monaco has no test for viewUserInputEvents.ts.
public class ViewUserInputEventsTests
{
    [Fact]
    public void EmitMouseDown_ConvertsTheTargetToModelCoordinates()
    {
        TestCodeEditor.WithTestCodeEditor("l1\nl2\nl3\nl4\nl5\nl6", editor =>
        {
            // View line 3 is the model line 5.
            editor.ViewModel.SetHiddenAreas(new[] { new Range(3, 1, 4, 1) });
            var events = new ViewUserInputEvents(editor.ViewModel.CoordinatesConverter);
            MouseTarget? received = null;
            events.MouseDown += (_, e) => received = e.Target;
            var mouseEvent = new EditorMouseEvent(0, 0, true, false, false, false, false, false, 1, 0);
            var target = new MouseTarget(MouseTargetType.ContentText, new Position(3, 2), 2, new Range(3, 2, 3, 2), false, null);

            events.EmitMouseDown(new EditorMouseEventArgs(mouseEvent, target));

            Assert.Equal(new Position(5, 2), received!.Position);
            Assert.Equal(new Range(5, 2, 5, 2), received.Range);
            Assert.Equal(MouseTargetType.ContentText, received.Type);
        });
    }

    // Monaco: viewUserInputEvents.emitMouseMove
    [Fact]
    public void EmitMouseMove_ConvertsTheTargetToModelCoordinates()
    {
        TestCodeEditor.WithTestCodeEditor("l1\nl2\nl3\nl4\nl5\nl6", editor =>
        {
            editor.ViewModel.SetHiddenAreas(new[] { new Range(3, 1, 4, 1) });
            var events = new ViewUserInputEvents(editor.ViewModel.CoordinatesConverter);
            MouseTarget? received = null;
            events.MouseMove += (_, e) => received = e.Target;
            var target = new MouseTarget(MouseTargetType.ContentText, new Position(3, 2), 2, new Range(3, 2, 3, 2), false, null);

            events.EmitMouseMove(new EditorMouseEventArgs(Mouse(), target));

            Assert.Equal(new Position(5, 2), received!.Position);
            Assert.Equal(new Range(5, 2, 5, 2), received.Range);
        });
    }

    // Monaco: viewUserInputEvents.emitMouseMove
    [Fact]
    public void EmitMouseMove_NoListener_NoException()
    {
        TestCodeEditor.WithTestCodeEditor("l1", editor =>
        {
            var events = new ViewUserInputEvents(editor.ViewModel.CoordinatesConverter);

            events.EmitMouseMove(new EditorMouseEventArgs(Mouse(), Target()));
            events.EmitMouseLeave();
        });
    }

    // Monaco: viewUserInputEvents.emitMouseLeave
    [Fact]
    public void EmitMouseLeave_RaisesOnce()
    {
        TestCodeEditor.WithTestCodeEditor("l1", editor =>
        {
            var events = new ViewUserInputEvents(editor.ViewModel.CoordinatesConverter);
            int count = 0;
            events.MouseLeave += (_, _) => count++;

            events.EmitMouseLeave();

            Assert.Equal(1, count);
        });
    }

    // Monaco: viewUserInputEvents.emitMouseMove
    [Fact]
    public void EmitMouseMove_PassesTheEvent()
    {
        TestCodeEditor.WithTestCodeEditor("l1", editor =>
        {
            var events = new ViewUserInputEvents(editor.ViewModel.CoordinatesConverter);
            EditorMouseEvent? received = null;
            events.MouseMove += (_, e) => received = e.Event;
            var mouseEvent = new EditorMouseEvent(12.5, 7, true, false, true, false, false, false, 1, 0);

            events.EmitMouseMove(new EditorMouseEventArgs(mouseEvent, Target()));

            Assert.Equal(mouseEvent, received);
        });
    }

    [Fact]
    public void Unsubscribe_StopsDelivery()
    {
        TestCodeEditor.WithTestCodeEditor("l1", editor =>
        {
            var events = new ViewUserInputEvents(editor.ViewModel.CoordinatesConverter);
            int moves = 0;
            int leaves = 0;
            EventHandler<EditorMouseEventArgs> onMove = (_, _) => moves++;
            EventHandler onLeave = (_, _) => leaves++;
            events.MouseMove += onMove;
            events.MouseLeave += onLeave;
            events.MouseMove -= onMove;
            events.MouseLeave -= onLeave;

            events.EmitMouseMove(new EditorMouseEventArgs(Mouse(), Target()));
            events.EmitMouseLeave();

            Assert.Equal(0, moves);
            Assert.Equal(0, leaves);
        });
    }

    private static EditorMouseEvent Mouse() => new(0, 0, true, false, false, false, false, false, 1, 0);

    private static MouseTarget Target() => new(MouseTargetType.ContentText, new Position(1, 1), 1, new Range(1, 1, 1, 1), false, null);
}

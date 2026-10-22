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
}

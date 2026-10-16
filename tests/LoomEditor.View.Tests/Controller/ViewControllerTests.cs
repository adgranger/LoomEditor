using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Controller;
using LoomEditor.View.Cursors;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Controller;

// Monaco has no test for dispatchMouse (it is covered by the editor's integration tests): all tests are new.
public class ViewControllerTests
{
    private const string Text = "just some text\nand another line\nand another one";

    private static MouseDispatchData Data(
        int lineNumber,
        int column,
        int mouseDownCount = 1,
        bool inSelectionMode = false,
        bool startedOnLineNumbers = false,
        bool alt = false,
        bool shift = false,
        bool ctrl = false,
        bool middle = false) =>
        new(
            new Position(lineNumber, column), column, NavigationCommandRevealType.Regular, startedOnLineNumbers, inSelectionMode,
            mouseDownCount, alt, ctrl, false, shift, !middle, middle, false);

    private static void WithController(Action<TestCodeEditor, ViewController> callback, TestCodeEditorOptions? options = null) =>
        TestCodeEditor.WithTestCodeEditor(Text, editor => callback(editor, new ViewController(editor.ViewModel)), options);

    [Fact]
    public void DispatchMouse_PlainClick_MovesTheCursor()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(2, 5));

            Assert.Equal(new Selection(2, 5, 2, 5), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_ShiftClick_ExtendsTheSelection()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 3));
            controller.DispatchMouse(Data(2, 5, inSelectionMode: true, shift: true));

            Assert.Equal(new Selection(1, 3, 2, 5), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_DoubleClick_SelectsTheWord()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 8, mouseDownCount: 2));

            Assert.Equal(new Selection(1, 6, 1, 10), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_TripleClick_SelectsTheLine()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(2, 4, mouseDownCount: 3));

            Assert.Equal(new Selection(2, 1, 3, 1), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_FourClicks_SelectsEverything()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(2, 4, mouseDownCount: 4));

            Assert.Equal(new Selection(1, 1, 3, 16), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_AltClick_AddsASecondCursor()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 3));
            controller.DispatchMouse(Data(3, 5, alt: true));

            Assert.Equal([new Selection(1, 3, 1, 3), new Selection(3, 5, 3, 5)], editor.ViewModel.GetSelections());
        });
    }

    [Fact]
    public void DispatchMouse_AltClickOnASecondaryCursor_RemovesIt()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 3));
            controller.DispatchMouse(Data(3, 5, alt: true));

            controller.DispatchMouse(Data(3, 5, alt: true));

            Assert.Single(editor.ViewModel.GetSelections());
        });
    }

    [Fact]
    public void DispatchMouse_AltAndCtrlClick_DoesNothing()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 3));

            controller.DispatchMouse(Data(3, 5, alt: true, ctrl: true));

            Assert.Equal(new Selection(1, 3, 1, 3), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_CtrlIsTheModifier_CtrlClickAddsACursor()
    {
        WithController(
            (editor, controller) =>
            {
                controller.DispatchMouse(Data(1, 3));
                controller.DispatchMouse(Data(3, 5, ctrl: true));

                Assert.Equal(2, editor.ViewModel.GetSelections().Count);
            },
            new TestCodeEditorOptions(CursorOptions: new EditorCursorOptions { MultiCursorModifier = MultiCursorModifier.CtrlCmd }));
    }

    [Fact]
    public void DispatchMouse_AltShiftDrag_MakesAColumnSelection()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 3));

            controller.DispatchMouse(Data(3, 5, inSelectionMode: true, alt: true, shift: true));

            Assert.Equal(3, editor.ViewModel.GetSelections().Count);
        });
    }

    [Fact]
    public void DispatchMouse_MiddleButton_MakesAColumnSelection()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 3, middle: true));

            controller.DispatchMouse(Data(3, 5, inSelectionMode: true, middle: true));

            Assert.Equal(3, editor.ViewModel.GetSelections().Count);
        });
    }

    [Fact]
    public void DispatchMouse_StartedOnLineNumbers_SelectsTheLine()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(2, 1, startedOnLineNumbers: true));

            Assert.Equal(new Selection(2, 1, 3, 1), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_DragStartedOnLineNumbers_ExtendsTheLineSelection()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 1, startedOnLineNumbers: true));

            controller.DispatchMouse(Data(2, 1, startedOnLineNumbers: true, inSelectionMode: true));

            Assert.Equal(new Selection(1, 1, 3, 1), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_AltClickOnLineNumbers_AddsAWholeLineCursor()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 3));

            controller.DispatchMouse(Data(3, 1, startedOnLineNumbers: true, alt: true));

            Assert.Equal(2, editor.ViewModel.GetSelections().Count);
            Assert.Equal(new Selection(3, 1, 3, 16), editor.ViewModel.GetSelections()[1]);
        });
    }

    [Fact]
    public void DispatchMouse_ColumnSelectionOption_ShiftDragMakesAColumnSelection()
    {
        WithController(
            (editor, controller) =>
            {
                controller.DispatchMouse(Data(1, 3));

                controller.DispatchMouse(Data(3, 5, inSelectionMode: true, shift: true));

                Assert.Equal(3, editor.ViewModel.GetSelections().Count);
            },
            new TestCodeEditorOptions(CursorOptions: new EditorCursorOptions { ColumnSelection = true }));
    }
}

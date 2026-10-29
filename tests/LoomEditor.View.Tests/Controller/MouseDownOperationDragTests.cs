using LoomEditor.Core.Text;
using LoomEditor.View.Controller;
using LoomEditor.View.Cursors;
using LoomEditor.View.Tests.Layout.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Controller;

// Monaco has no test for MouseDownOperation (it needs a DOM): all tests are new.
public class MouseDownOperationDragTests
{
    private const string Text = "just some text\nand another line";

    private static readonly Selection Original = new(1, 3, 1, 8);

    private sealed class Fixture : IDisposable
    {
        public Fixture(TestCodeEditor editor)
        {
            Editor = editor;
            Clock = new FakeTimeProvider();
            Events = new ViewUserInputEvents(editor.ViewModel.CoordinatesConverter);
            Operation = new MouseDownOperation(
                editor.ViewModel, new ViewController(editor.ViewModel, Events), CreateMouseTarget, _ => new Handle(), Clock);
            Events.MouseDrag += (_, e) => Log.Add("drag " + e.Target.Position);
            Events.MouseDrop += (_, e) => Log.Add("drop " + (e.Target?.Position?.ToString() ?? "null"));
            Events.MouseDropCanceled += (_, _) => Log.Add("canceled");
            editor.ViewModel.SetSelections("test", [Original]);
        }

        public TestCodeEditor Editor { get; }

        public FakeTimeProvider Clock { get; }

        public ViewUserInputEvents Events { get; }

        public MouseDownOperation Operation { get; }

        public List<string> Log { get; } = [];

        public Selection Selection => Editor.ViewModel.GetSelection();

        // Column 5 is inside the selection (columns 3 to 8), column 12 is outside.
        public EditorMouseEvent Inside(bool alt = false, int detail = 1) => Event(40, alt, detail);

        public EditorMouseEvent Outside() => Event(110, false, 1);

        public EditorMouseEvent Event(double dips, bool alt, int detail) =>
            new(Editor.ViewModel.LayoutInfo.ContentLeft + dips, 3, true, false, alt, false, false, false, detail, 0);

        public void Dispose() => Operation.Dispose();

        private MouseTarget CreateMouseTarget(double x, double y) =>
            MouseTargetFactory.CreateMouseTarget(Editor.ViewModel, new FakeHitTester(Editor), x, y);

        private sealed class Handle : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private sealed class FakeHitTester(TestCodeEditor editor) : ILineHitTester
    {
        public double GetLineWidth(int lineNumber) => editor.Model.GetLineLength(lineNumber) * 10;

        public Position? GetPositionAtCoordinate(int lineNumber, double mouseContentHorizontalOffset)
        {
            int index = (int)Math.Floor((mouseContentHorizontalOffset / 10) + 0.5);
            return new Position(lineNumber, Math.Clamp(index, 0, editor.Model.GetLineLength(lineNumber)) + 1);
        }
    }

    private static void With(Action<Fixture> callback, EditorCursorOptions? options = null) =>
        TestCodeEditor.WithTestCodeEditor(
            Text, editor =>
            {
                using var fixture = new Fixture(editor);
                callback(fixture);
            },
            new TestCodeEditorOptions(CursorOptions: options, OuterWidth: 500, OuterHeight: 380));

    [Fact]
    public void Press_InsideNonEmptySelection_StartsDrag_AndKeepsSelection()
    {
        With(f =>
        {
            f.Operation.Start(MouseTargetType.ContentText, f.Inside());

            Assert.True(f.Operation.IsDragAndDrop);
            Assert.True(f.Operation.IsActive);
            Assert.Equal(Original, f.Selection);
        });
    }

    [Fact]
    public void Press_OutsideSelection_SelectsAsBefore()
    {
        With(f =>
        {
            f.Operation.Start(MouseTargetType.ContentText, f.Outside());

            Assert.False(f.Operation.IsDragAndDrop);
            Assert.Equal(new Selection(1, 12, 1, 12), f.Selection);
        });
    }

    [Fact]
    public void Press_InsideEmptySelection_SelectsAsBefore()
    {
        With(f =>
        {
            f.Editor.ViewModel.SetSelections("test", [new Selection(1, 5, 1, 5)]);

            f.Operation.Start(MouseTargetType.ContentText, f.Inside());

            Assert.False(f.Operation.IsDragAndDrop);
        });
    }

    [Fact]
    public void Press_DragAndDropOff_SelectsAsBefore() =>
        With(
            f =>
            {
                f.Operation.Start(MouseTargetType.ContentText, f.Inside());

                Assert.False(f.Operation.IsDragAndDrop);
                Assert.Equal(new Selection(1, 5, 1, 5), f.Selection);
            },
            new EditorCursorOptions { DragAndDrop = false });

    [Fact]
    public void Press_ReadOnly_SelectsAsBefore() =>
        With(
            f =>
            {
                f.Operation.Start(MouseTargetType.ContentText, f.Inside());

                Assert.False(f.Operation.IsDragAndDrop);
            },
            new EditorCursorOptions { ReadOnly = true });

    [Fact]
    public void Press_ColumnSelection_SelectsAsBefore() =>
        With(
            f =>
            {
                f.Operation.Start(MouseTargetType.ContentText, f.Inside());

                Assert.False(f.Operation.IsDragAndDrop);
            },
            new EditorCursorOptions { ColumnSelection = true });

    [Fact]
    public void Press_DoubleClickInSelection_SelectsWord()
    {
        With(f =>
        {
            f.Operation.Start(MouseTargetType.ContentText, f.Inside());
            f.Operation.OnPointerUp();
            f.Clock.Advance(100);

            f.Operation.Start(MouseTargetType.ContentText, f.Inside(detail: 2));

            Assert.False(f.Operation.IsDragAndDrop);
            Assert.Equal(new Selection(1, 1, 1, 5), f.Selection);
        });
    }

    [Fact]
    public void Press_AltInSelection_DoesNotStartDrag()
    {
        With(f =>
        {
            f.Operation.Start(MouseTargetType.ContentText, f.Inside(alt: true));

            Assert.False(f.Operation.IsDragAndDrop);
        });
    }

    [Fact]
    public void Move_WhileDragging_EmitsMouseDragNotSelection()
    {
        With(f =>
        {
            f.Operation.Start(MouseTargetType.ContentText, f.Inside());

            f.Operation.OnMouseDownThenMove(f.Outside());

            Assert.Equal(["drag (1,12)"], f.Log);
            Assert.Equal(Original, f.Selection);
        });
    }

    [Fact]
    public void Release_WhileDragging_EmitsMouseDropWithLastTarget()
    {
        With(f =>
        {
            f.Operation.Start(MouseTargetType.ContentText, f.Inside());
            f.Operation.OnMouseDownThenMove(f.Outside());
            f.Log.Clear();

            f.Operation.OnPointerUp();

            Assert.Equal(["drop (1,12)"], f.Log);
            Assert.False(f.Operation.IsActive);
        });
    }

    [Fact]
    public void Release_WithoutMove_EmitsMouseDropOnSamePosition()
    {
        With(f =>
        {
            f.Operation.Start(MouseTargetType.ContentText, f.Inside());

            f.Operation.OnPointerUp();

            Assert.Equal(["drop (1,5)"], f.Log);
            Assert.Equal(Original, f.Selection);
        });
    }

    [Fact]
    public void Escape_WhileDragging_EmitsDropCanceledAndStops()
    {
        With(f =>
        {
            f.Operation.Start(MouseTargetType.ContentText, f.Inside());

            f.Operation.OnKeyDownWhileDragging();
            f.Operation.OnPointerUp();

            Assert.Equal(["canceled"], f.Log);
            Assert.False(f.Operation.IsActive);
        });
    }

    [Fact]
    public void Stop_ResetsIsDragAndDrop()
    {
        With(f =>
        {
            f.Operation.Start(MouseTargetType.ContentText, f.Inside());

            f.Operation.OnPointerUp();

            Assert.False(f.Operation.IsDragAndDrop);
        });
    }
}

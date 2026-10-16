using LoomEditor.Core.Text;
using LoomEditor.View.Controller;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Layout.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Controller;

// Monaco has no test for MouseDownOperation and DragScrolling (they need a DOM): all tests are new.
public class MouseDownOperationTests
{
    private const int LineHeight = 19;

    // Fixed 10-DIP characters, like in MouseTargetFactoryTests.
    private sealed class Fixture : IDisposable
    {
        private readonly List<(Action Callback, bool[] Disposed)> _frames = [];

        public Fixture(TestCodeEditor editor)
        {
            Editor = editor;
            Clock = new FakeTimeProvider();
            Controller = new ViewController(editor.ViewModel);
            Operation = new MouseDownOperation(
                editor.ViewModel, Controller, CreateMouseTarget, ScheduleAtNextAnimationFrame, Clock);
        }

        public TestCodeEditor Editor { get; }

        public FakeTimeProvider Clock { get; }

        public ViewController Controller { get; }

        public MouseDownOperation Operation { get; }

        public int PendingFrames => _frames.Count(frame => !frame.Disposed[0]);

        public double X(double dips) => Editor.ViewModel.LayoutInfo.ContentLeft + dips;

        public static EditorMouseEvent Event(double x, double y, bool shift = false, int detail = 1) =>
            new(x, y, true, false, false, false, shift, false, detail, 0);

        public void RunFrame()
        {
            (Action callback, bool[] disposed) = _frames.First(frame => !frame.Disposed[0]);
            disposed[0] = true;
            callback();
        }

        public void Dispose() => Operation.Dispose();

        private MouseTarget CreateMouseTarget(double x, double y) =>
            MouseTargetFactory.CreateMouseTarget(Editor.ViewModel, new FakeHitTester(Editor), x, y);

        private Handle ScheduleAtNextAnimationFrame(Action callback)
        {
            bool[] disposed = [false];
            _frames.Add((callback, disposed));
            return new Handle(disposed);
        }

        private sealed class Handle(bool[] disposed) : IDisposable
        {
            public void Dispose() => disposed[0] = true;
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

    private static void With(string text, Action<Fixture> callback) =>
        TestCodeEditor.WithTestCodeEditor(
            text, editor =>
            {
                using var fixture = new Fixture(editor);
                callback(fixture);
            },
            new TestCodeEditorOptions(OuterWidth: 500, OuterHeight: 380));

    [Fact]
    public void Start_ThenMoveWithTheButtonHeld_SelectsFromTheClickToTheMouse()
    {
        With("just some text\nand another line", f =>
        {
            f.Operation.Start(MouseTargetType.ContentText, Fixture.Event(f.X(20), 3));
            Assert.True(f.Operation.IsActive);
            Assert.Equal(new Selection(1, 3, 1, 3), f.Editor.ViewModel.GetSelection());

            f.Operation.OnMouseDownThenMove(Fixture.Event(f.X(50), 3));

            Assert.Equal(new Selection(1, 3, 1, 6), f.Editor.ViewModel.GetSelection());

            f.Operation.OnPointerUp();
            Assert.False(f.Operation.IsActive);
        });
    }

    [Fact]
    public void Start_ShiftClick_ExtendsTheSelection()
    {
        With("just some text\nand another line", f =>
        {
            f.Operation.Start(MouseTargetType.ContentText, Fixture.Event(f.X(20), 3));
            f.Operation.OnPointerUp();

            f.Operation.Start(MouseTargetType.ContentText, Fixture.Event(f.X(50), LineHeight + 3, shift: true));

            Assert.Equal(new Selection(1, 3, 2, 6), f.Editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void Start_DoubleClickThenDrag_GrowsTheSelectionByWords()
    {
        With("just some text", f =>
        {
            f.Operation.Start(MouseTargetType.ContentText, Fixture.Event(f.X(50), 3));
            f.Operation.OnPointerUp();
            f.Clock.Advance(100);
            f.Operation.Start(MouseTargetType.ContentText, Fixture.Event(f.X(50), 3, detail: 4));
            Assert.Equal(new Selection(1, 6, 1, 10), f.Editor.ViewModel.GetSelection());

            f.Operation.OnMouseDownThenMove(Fixture.Event(f.X(120), 3));

            Assert.Equal(new Selection(1, 6, 1, 15), f.Editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void Start_ClickOnTheLineNumbers_SelectsTheLine()
    {
        With("just some text\nand another line", f =>
        {
            f.Operation.Start(MouseTargetType.GutterLineNumbers, Fixture.Event(f.Editor.ViewModel.LayoutInfo.LineNumbersLeft + 1, LineHeight + 3));

            Assert.Equal(new Selection(2, 1, 2, 17), f.Editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void OnMouseDownThenMove_AboveTheEditor_ScrollsUpAtTheSpeedOfTheDistance()
    {
        With(string.Join('\n', Enumerable.Repeat("some text", 100)), f =>
        {
            f.Editor.ViewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(null, 500), ScrollType.Immediate);
            f.Operation.Start(MouseTargetType.ContentText, Fixture.Event(f.X(20), 100));

            // One line height above the editor: v = 380 / 19 = 20 lines, d = 1 => max(30, 20 * 2) = 40 lines per second.
            f.Operation.OnMouseDownThenMove(Fixture.Event(f.X(20), -LineHeight));
            Assert.Equal(1, f.PendingFrames);
            f.Clock.Advance(100);
            f.RunFrame();

            Assert.Equal(500 - 76, f.Editor.ViewModel.ViewLayout.GetCurrentScrollTop());
            Assert.Equal(1, f.PendingFrames);
        });
    }

    [Fact]
    public void OnMouseDownThenMove_BelowTheEditor_ScrollsDownAndSelectsToTheLastVisibleLine()
    {
        With(string.Join('\n', Enumerable.Repeat("some text", 100)), f =>
        {
            f.Operation.Start(MouseTargetType.ContentText, Fixture.Event(f.X(20), 3));

            f.Operation.OnMouseDownThenMove(Fixture.Event(f.X(20), 380 + LineHeight));
            f.Clock.Advance(100);
            f.RunFrame();

            Assert.Equal(76, f.Editor.ViewModel.ViewLayout.GetCurrentScrollTop());
            Assert.True(f.Editor.ViewModel.GetSelection().PositionLineNumber > 20);
        });
    }

    [Fact]
    public void OnMouseDownThenMove_BackInsideTheEditor_StopsTheDragScrolling()
    {
        With(string.Join('\n', Enumerable.Repeat("some text", 100)), f =>
        {
            f.Editor.ViewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(null, 500), ScrollType.Immediate);
            f.Operation.Start(MouseTargetType.ContentText, Fixture.Event(f.X(20), 100));
            f.Operation.OnMouseDownThenMove(Fixture.Event(f.X(20), -LineHeight));
            Assert.Equal(1, f.PendingFrames);

            f.Operation.OnMouseDownThenMove(Fixture.Event(f.X(20), 100));

            Assert.Equal(0, f.PendingFrames);
        });
    }

    [Fact]
    public void OnPointerUp_WhileDragScrolling_StopsTheDragScrolling()
    {
        With(string.Join('\n', Enumerable.Repeat("some text", 100)), f =>
        {
            f.Operation.Start(MouseTargetType.ContentText, Fixture.Event(f.X(20), 100));
            f.Operation.OnMouseDownThenMove(Fixture.Event(f.X(20), -LineHeight));

            f.Operation.OnPointerUp();

            Assert.Equal(0, f.PendingFrames);
            Assert.False(f.Operation.IsActive);
        });
    }

    [Fact]
    public void OnMouseDownThenMove_RightOfTheContent_ScrollsRightAndSelectsToTheEndOfTheLine()
    {
        With("a fairly long line of text that is longer than the editor", f =>
        {
            f.Editor.ViewModel.ViewLayout.SetMaxLineWidth(2000);
            f.Operation.Start(MouseTargetType.ContentText, Fixture.Event(f.X(20), 3));

            f.Operation.OnMouseDownThenMove(Fixture.Event(f.Editor.ViewModel.LayoutInfo.Width + 20, 3));
            f.Clock.Advance(100);
            f.RunFrame();

            Assert.True(f.Editor.ViewModel.ViewLayout.GetCurrentScrollLeft() > 0);
            Assert.Equal(new Position(1, 58), f.Editor.ViewModel.GetSelection().Position);
        });
    }
}

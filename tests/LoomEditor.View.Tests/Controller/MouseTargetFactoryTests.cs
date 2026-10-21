using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Controller;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Controller;

// Monaco has no test file for mouseTarget.ts (it needs a DOM): all tests are new.
public class MouseTargetFactoryTests
{
    private const int LineHeight = 19;

    // Fixed 10-DIP characters; the nearest boundary of 12 is 10, of 16 is 20 (a .5 goes to the right).
    private sealed class FakeLineHitTester(TestCodeEditor editor) : ILineHitTester
    {
        public double GetLineWidth(int lineNumber) => editor.ViewModel.GetLineLength(lineNumber) * 10;

        public Position? GetPositionAtCoordinate(int lineNumber, double mouseContentHorizontalOffset)
        {
            int index = (int)Math.Floor((mouseContentHorizontalOffset / 10) + 0.5);
            return new Position(lineNumber, Math.Clamp(index, 0, editor.ViewModel.GetLineLength(lineNumber)) + 1);
        }
    }

    private static void WithEditor(string text, Action<TestCodeEditor, ILineHitTester> callback, TestCodeEditorOptions? options = null) =>
        TestCodeEditor.WithTestCodeEditor(text, editor => callback(editor, new FakeLineHitTester(editor)), options);

    private static double ContentX(TestCodeEditor editor, double dips) => editor.ViewModel.LayoutInfo.ContentLeft + dips;

    [Fact]
    public void CreateMouseTarget_XInTheLineNumbers_IsGutterLineNumbersAtColumn1()
    {
        WithEditor("abc\ndef\nghi", (editor, lines) =>
        {
            double x = editor.ViewModel.LayoutInfo.LineNumbersLeft + 1;

            MouseTarget target = MouseTargetFactory.CreateMouseTarget(editor.ViewModel, lines, x, (2 * LineHeight) + 3);

            Assert.Equal(MouseTargetType.GutterLineNumbers, target.Type);
            Assert.Equal(new Position(3, 1), target.Position);
            Assert.Equal(new Range(3, 1, 3, 4), target.Range);
            Assert.False(target.IsAfterLines);
        });
    }

    [Fact]
    public void CreateMouseTarget_GutterBelowTheLastLine_IsAfterLinesAtTheEndOfTheLastLine()
    {
        WithEditor("abc\ndef", (editor, lines) =>
        {
            MouseTarget target = MouseTargetFactory.CreateMouseTarget(
                editor.ViewModel, lines, editor.ViewModel.LayoutInfo.LineNumbersLeft + 1, 90);

            Assert.Equal(MouseTargetType.GutterLineNumbers, target.Type);
            Assert.Equal(new Position(2, 4), target.Position);
            Assert.True(target.IsAfterLines);
        });
    }

    [Fact]
    public void CreateMouseTarget_YInTheTopPadding_IsContentEmptyAtTheStart()
    {
        WithEditor("abc\ndef", (editor, lines) =>
        {
            editor.ViewModel.SetOptions(new EditorLayoutOptions(), new ViewLayoutOptions(LineHeight, PaddingTop: 10));

            MouseTarget target = MouseTargetFactory.CreateMouseTarget(editor.ViewModel, lines, ContentX(editor, 5), 4);

            Assert.Equal(MouseTargetType.ContentEmpty, target.Type);
            Assert.Equal(new Position(1, 1), target.Position);
            Assert.True(target.IsAfterLines);
        });
    }

    [Fact]
    public void CreateMouseTarget_BelowTheLastLine_IsContentEmptyAtTheMaxColumnAfterLines()
    {
        WithEditor("abc\ndefgh", (editor, lines) =>
        {
            MouseTarget target = MouseTargetFactory.CreateMouseTarget(editor.ViewModel, lines, ContentX(editor, 5), 90);

            Assert.Equal(MouseTargetType.ContentEmpty, target.Type);
            Assert.Equal(new Position(2, 6), target.Position);
            Assert.True(target.IsAfterLines);
        });
    }

    [Fact]
    public void CreateMouseTarget_EmptyLine_IsContentEmptyAtColumn1()
    {
        WithEditor("abc\n\nghi", (editor, lines) =>
        {
            MouseTarget target = MouseTargetFactory.CreateMouseTarget(editor.ViewModel, lines, ContentX(editor, 30), LineHeight + 3);

            Assert.Equal(MouseTargetType.ContentEmpty, target.Type);
            Assert.Equal(new Position(2, 1), target.Position);
            Assert.False(target.IsAfterLines);
            Assert.Equal(30, target.HorizontalDistanceToText);
        });
    }

    [Fact]
    public void CreateMouseTarget_BeyondTheEndOfALine_IsContentEmptyAtTheMaxColumn()
    {
        WithEditor("abc\ndef", (editor, lines) =>
        {
            MouseTarget target = MouseTargetFactory.CreateMouseTarget(editor.ViewModel, lines, ContentX(editor, 45), 3);

            Assert.Equal(MouseTargetType.ContentEmpty, target.Type);
            Assert.Equal(new Position(1, 4), target.Position);
            Assert.Equal(15, target.HorizontalDistanceToText);
        });
    }

    [Theory]
    [InlineData(12, 2)]
    [InlineData(16, 3)]
    [InlineData(0, 1)]
    public void CreateMouseTarget_OnText_IsContentTextAtTheNearestBoundary(double dips, int expectedColumn)
    {
        WithEditor("abc\ndef", (editor, lines) =>
        {
            MouseTarget target = MouseTargetFactory.CreateMouseTarget(editor.ViewModel, lines, ContentX(editor, dips), 3);

            Assert.Equal(MouseTargetType.ContentText, target.Type);
            Assert.Equal(new Position(1, expectedColumn), target.Position);
            Assert.Equal(Range.Collapsed(new Position(1, expectedColumn)), target.Range);
        });
    }

    [Fact]
    public void CreateMouseTarget_Scrolled_MapsTheScreenPointToTheScrolledPosition()
    {
        WithEditor(string.Join('\n', Enumerable.Repeat("abcdefghijklmnopqrstuvwxyz", 50)), (editor, lines) =>
        {
            editor.ViewModel.ViewLayout.SetMaxLineWidth(260);
            editor.ViewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(40, 10 * LineHeight), ScrollType.Immediate);

            MouseTarget target = MouseTargetFactory.CreateMouseTarget(editor.ViewModel, lines, ContentX(editor, 12), 3);

            Assert.Equal(MouseTargetType.ContentText, target.Type);
            // Line 11 (10 lines scrolled), 40 + 12 = 52 DIPs: the boundary at 50 is column 6.
            Assert.Equal(new Position(11, 6), target.Position);
        });
    }

    [Fact]
    public void CreateMouseTarget_StickyTabStops_SnapsToTheSoftTabBoundary()
    {
        WithEditor(
            "        abc",
            (editor, lines) =>
            {
                // 30 DIPs: between boundaries 3 and 4 of the indentation; the nearest tab stop (4 spaces) is column 5.
                MouseTarget target = MouseTargetFactory.CreateMouseTarget(editor.ViewModel, lines, ContentX(editor, 33), 3);

                Assert.Equal(new Position(1, 5), target.Position);
            },
            new TestCodeEditorOptions(CursorOptions: new EditorCursorOptions { StickyTabStops = true }));
    }

    [Fact]
    public void GetMouseColumn_UsesJavaScriptRounding()
    {
        Assert.Equal(4, MouseTargetFactory.GetMouseColumn(25, 10));
        Assert.Equal(1, MouseTargetFactory.GetMouseColumn(-5, 10));
        Assert.Equal(1, MouseTargetFactory.GetMouseColumn(4, 10));
    }

    [Fact]
    public void CreateMouseTarget_BelowAHiddenArea_ReturnsTheViewPosition()
    {
        WithEditor("l1\nl2\nl3\nl4\nl5\nl6", (editor, lines) =>
        {
            editor.ViewModel.SetHiddenAreas(new[] { new Range(3, 1, 4, 1) });

            // View line 3 is the model line 5.
            MouseTarget target = MouseTargetFactory.CreateMouseTarget(editor.ViewModel, lines, ContentX(editor, 2), (2 * LineHeight) + 3);

            Assert.Equal(MouseTargetType.ContentText, target.Type);
            Assert.Equal(new Position(3, 1), target.Position);

            var controller = new ViewController(editor.ViewModel);
            controller.DispatchMouse(new MouseDispatchData(
                target.Position!.Value, 1, NavigationCommandRevealType.Regular, false, false, 1, false, false, false, false, true, false, false));
            Assert.Equal(new Selection(5, 1, 5, 1), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void CreateMouseTarget_AfterLastViewLine_UsesTheLastViewLine()
    {
        WithEditor("l1\nl2\nl3\nl4", (editor, lines) =>
        {
            editor.ViewModel.SetHiddenAreas(new[] { new Range(3, 1, 4, 1) });

            MouseTarget target = MouseTargetFactory.CreateMouseTarget(editor.ViewModel, lines, ContentX(editor, 5), 200);

            Assert.True(target.IsAfterLines);
            Assert.Equal(new Position(2, 3), target.Position);
        });
    }
}
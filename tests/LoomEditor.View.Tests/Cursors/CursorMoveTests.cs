using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Cursors;

// Monaco's cursorMoveCommand.test.ts runs through the cursorMove core command (P3-26): these are direct tests.
public class CursorMoveTests
{
    private static void Run(string text, Action<ViewModel> test) =>
        TestCodeEditor.WithTestCodeEditor(text, editor => test(editor.ViewModel));

    private static void Place(ViewModel viewModel, int lineNumber, int column) =>
        viewModel.SetCursorStates(
            "test",
            CursorChangeReason.NotSet,
            [CursorState.FromModelSelection(new Selection(lineNumber, column, lineNumber, column))]);

    [Fact]
    public void Parse_Left_UsesDefaults()
    {
        CursorMoveParsedArguments? parsed = CursorMove.Parse(new CursorMoveArguments("left"));

        Assert.Equal(new CursorMoveParsedArguments(CursorMoveDirection.Left, CursorMoveUnit.None, false, 1, false), parsed);
    }

    [Fact]
    public void Parse_DownByWrappedLine_ReadsAllArguments()
    {
        CursorMoveParsedArguments? parsed = CursorMove.Parse(new CursorMoveArguments("down", Select: true, By: "wrappedLine", Value: 3));

        Assert.Equal(new CursorMoveParsedArguments(CursorMoveDirection.Down, CursorMoveUnit.WrappedLine, true, 3, false), parsed);
    }

    [Fact]
    public void Parse_IllegalDirection_ReturnsNull()
    {
        Assert.Null(CursorMove.Parse(new CursorMoveArguments("nope")));
        Assert.Null(CursorMove.Parse(new CursorMoveArguments(string.Empty)));
    }

    [Fact]
    public void Parse_ZeroValue_BecomesOne()
    {
        // Monaco: 'value: (args.value || 1)'
        CursorMoveParsedArguments? parsed = CursorMove.Parse(new CursorMoveArguments("left", Value: 0));

        Assert.Equal(1, parsed!.Value);
    }

    [Fact]
    public void Parse_UnknownUnit_IsNone()
    {
        Assert.Equal(CursorMoveUnit.None, CursorMove.Parse(new CursorMoveArguments("up", By: "page"))!.Unit);
    }

    [Fact]
    public void SimpleMove_LeftByHalfLine_RoundsHalfUpLikeJavaScript()
    {
        Run("abcdefghi", viewModel =>
        {
            Place(viewModel, 1, 10);

            List<PartialCursorState>? result = CursorMoveCommands.SimpleMove(
                viewModel, viewModel.GetCursorStates(), CursorMoveDirection.Left, false, 1, CursorMoveUnit.HalfLine);

            // Math.round(9 / 2) is 5 in JavaScript (C# Math.Round would give 4).
            Assert.Equal(new Position(1, 5), result![0].ViewState!.Position);
        });
    }

    [Fact]
    public void SimpleMove_DownByFoldedLine_BehavesLikeLinesWithoutFolding()
    {
        Run("a\nb\nc\nd", viewModel =>
        {
            Place(viewModel, 1, 1);

            List<PartialCursorState>? result = CursorMoveCommands.SimpleMove(
                viewModel, viewModel.GetCursorStates(), CursorMoveDirection.Down, false, 2, CursorMoveUnit.FoldedLine);

            Assert.Equal(new Position(3, 1), result![0].ModelState!.Position);
        });
    }

    [Fact]
    public void SimpleMove_WrappedLineColumnCenter_RoundsHalfUp()
    {
        Run("abcd", viewModel =>
        {
            Place(viewModel, 1, 1);

            List<PartialCursorState>? result = CursorMoveCommands.SimpleMove(
                viewModel, viewModel.GetCursorStates(), CursorMoveDirection.WrappedLineColumnCenter, false, 1, CursorMoveUnit.None);

            // (5 + 1) / 2 = 3
            Assert.Equal(new Position(1, 3), result![0].ViewState!.Position);
        });
    }

    [Fact]
    public void SimpleMove_ViewportDirection_ReturnsNull()
    {
        Run("a", viewModel =>
        {
            Assert.Null(CursorMoveCommands.SimpleMove(
                viewModel, viewModel.GetCursorStates(), CursorMoveDirection.ViewPortTop, false, 1, CursorMoveUnit.None));
        });
    }

    [Fact]
    public void ViewportMove_ViewPortTop_GoesToTheFirstVisibleLine()
    {
        Run(string.Join('\n', Enumerable.Repeat("x", 30)), viewModel =>
        {
            viewModel.CompletelyVisibleViewRangeOverride = () => new Range(10, 1, 20, 1);
            Place(viewModel, 1, 1);

            List<PartialCursorState>? result = CursorMoveCommands.ViewportMove(
                viewModel, viewModel.GetCursorStates(), CursorMoveDirection.ViewPortTop, false, 1);

            Assert.Equal(10, result![0].ModelState!.Position.LineNumber);
        });
    }

    [Fact]
    public void ViewportMove_ViewPortBottomAndCenter_UseTheVisibleRange()
    {
        Run(string.Join('\n', Enumerable.Repeat("x", 30)), viewModel =>
        {
            viewModel.CompletelyVisibleViewRangeOverride = () => new Range(10, 1, 20, 1);
            Place(viewModel, 1, 1);

            List<PartialCursorState>? bottom = CursorMoveCommands.ViewportMove(
                viewModel, viewModel.GetCursorStates(), CursorMoveDirection.ViewPortBottom, false, 1);
            List<PartialCursorState>? center = CursorMoveCommands.ViewportMove(
                viewModel, viewModel.GetCursorStates(), CursorMoveDirection.ViewPortCenter, false, 1);

            Assert.Equal(20, bottom![0].ModelState!.Position.LineNumber);
            Assert.Equal(15, center![0].ModelState!.Position.LineNumber);
        });
    }

    [Fact]
    public void FindPositionInViewportIfOutside_KeepsInsideCursorAndMovesOutsideOne()
    {
        Run(string.Join('\n', Enumerable.Repeat("x", 30)), viewModel =>
        {
            var visible = new Range(10, 1, 20, 1);

            Place(viewModel, 12, 1);
            PartialCursorState inside = CursorMoveCommands.FindPositionInViewportIfOutside(viewModel, viewModel.GetCursorStates()[0], visible, false);
            Place(viewModel, 3, 1);
            PartialCursorState above = CursorMoveCommands.FindPositionInViewportIfOutside(viewModel, viewModel.GetCursorStates()[0], visible, false);
            Place(viewModel, 25, 1);
            PartialCursorState below = CursorMoveCommands.FindPositionInViewportIfOutside(viewModel, viewModel.GetCursorStates()[0], visible, false);

            Assert.Equal(12, inside.ViewState!.Position.LineNumber);
            Assert.Equal(10, above.ViewState!.Position.LineNumber);
            Assert.Equal(19, below.ViewState!.Position.LineNumber);
        });
    }
}

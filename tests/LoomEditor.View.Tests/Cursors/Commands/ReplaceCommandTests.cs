using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Tests.Cursors.TestHelpers;

namespace LoomEditor.View.Tests.Cursors.Commands;

// No Monaco test file targets these classes (they are tested through the cursor tests): all tests are new.
public class ReplaceCommandTests
{
    private static Selection Caret(int line, int column) => Selection.Collapsed(new Position(line, column));

    [Fact]
    public void ReplaceCommand_ReplacesRangeAndPutsCursorAtTheEndOfTheText()
    {
        CommandTester.TestCommand(
            ["abcde"],
            Caret(1, 1),
            _ => new ReplaceCommand(new Range(1, 2, 1, 4), "XY"),
            ["aXYde"],
            Caret(1, 4));
    }

    [Fact]
    public void ReplaceCommand_MultiLineText_PutsCursorAtTheEndOfTheLastInsertedLine()
    {
        CommandTester.TestCommand(
            ["abcde"],
            Caret(1, 1),
            _ => new ReplaceCommand(new Range(1, 2, 1, 4), "X\nYZ"),
            ["aX", "YZde"],
            Caret(2, 3));
    }

    [Fact]
    public void ReplaceCommandThatSelectsText_SelectsTheInsertedText()
    {
        CommandTester.TestCommand(
            ["abcde"],
            Caret(1, 1),
            _ => new ReplaceCommandThatSelectsText(new Range(1, 2, 1, 4), "XY"),
            ["aXYde"],
            new Selection(new Position(1, 2), new Position(1, 4)));
    }

    [Fact]
    public void ReplaceCommandWithoutChangingPosition_PutsCursorAtTheStartOfTheText()
    {
        CommandTester.TestCommand(
            ["abcde"],
            Caret(1, 2),
            _ => new ReplaceCommandWithoutChangingPosition(new Range(1, 2, 1, 2), "XY"),
            ["aXYbcde"],
            Caret(1, 2));
    }

    [Fact]
    public void ReplaceCommandWithOffsetCursorState_MovesTheCursorByTheOffsets()
    {
        CommandTester.TestCommand(
            ["abcde"],
            Caret(1, 2),
            _ => new ReplaceCommandWithOffsetCursorState(new Range(1, 2, 1, 2), "()", 0, -1),
            ["a()bcde"],
            Caret(1, 3));
    }

    [Fact]
    public void ReplaceCommandThatPreservesSelection_KeepsTheSelectionOnTheSameText()
    {
        CommandTester.TestCommand(
            ["abcde"],
            new Selection(new Position(1, 2), new Position(1, 4)),
            selection => new ReplaceCommandThatPreservesSelection(new Range(1, 1, 1, 1), "\n", selection),
            ["", "abcde"],
            new Selection(new Position(2, 2), new Position(2, 4)));
    }

    [Fact]
    public void ReplaceOvertypeCommand_EmptyRange_OverwritesAsManyCharactersAsTheText()
    {
        CommandTester.TestCommand(
            ["abcde"],
            Caret(1, 2),
            _ => new ReplaceOvertypeCommand(new Range(1, 2, 1, 2), "XY"),
            ["aXYde"],
            Caret(1, 4));
    }

    [Fact]
    public void ReplaceOvertypeCommand_AtTheEndOfTheLine_Appends()
    {
        CommandTester.TestCommand(
            ["abcde"],
            Caret(1, 6),
            _ => new ReplaceOvertypeCommand(new Range(1, 6, 1, 6), "XY"),
            ["abcdeXY"],
            Caret(1, 8));
    }

    [Fact]
    public void ReplaceOvertypeCommand_NegativeOffset_Throws()
    {
        var model = new LoomEditor.Core.Model.TextModel("abc");

        Assert.Throws<ArgumentOutOfRangeException>(
            () => ReplaceCommandHelpers.AddPositiveOffsetToModelPosition(model, new Position(1, 1), -1));
    }

    [Fact]
    public void ReplaceOvertypeCommandOnCompositionEnd_DeletesTheCharactersAfterTheRange()
    {
        CommandTester.TestCommand(
            ["abcde"],
            Caret(1, 1),
            _ => new ReplaceOvertypeCommandOnCompositionEnd(new Range(1, 2, 1, 4)),
            ["abc"],
            Caret(1, 4));
    }
}

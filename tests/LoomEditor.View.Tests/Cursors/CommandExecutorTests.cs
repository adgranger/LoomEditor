using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Tests.Cursors.TestHelpers;

namespace LoomEditor.View.Tests.Cursors;

// No Monaco test targets CommandExecutor directly (it is tested through every command): all tests are new.
public class CommandExecutorTests
{
    private static Selection Caret(int line, int column) => Selection.Collapsed(new Position(line, column));

    [Fact]
    public void ExecuteCommands_TwoCursors_AppliesBothEditsAsOneUndoStep()
    {
        var model = new TextModel("abcd");
        Selection[] before = [Caret(1, 1), Caret(1, 3)];

        IReadOnlyList<Selection>? after = CommandExecutor.ExecuteCommands(
            model,
            before,
            [new InsertTextCommand(new Range(1, 1, 1, 1), "x"), new InsertTextCommand(new Range(1, 3, 1, 3), "x")]);

        Assert.Equal("xabxcd", model.GetValue());
        Assert.NotNull(after);
        Assert.Equal([Caret(1, 2), Caret(1, 5)], after);

        model.Undo();

        Assert.Equal("abcd", model.GetValue());
        Assert.False(model.CanUndo);
    }

    [Fact]
    public void ExecuteCommands_OverlappingEdits_TheLaterCursorLoses()
    {
        // Operations are sorted with the last one first; when the start of the previous one is before the end of the
        // current one they overlap, and the cursor with the greater index (major identifier) loses: its operations are
        // dropped and its selection is removed from the result.
        var model = new TextModel("abcd");
        Selection[] before = [Caret(1, 1), Caret(1, 2)];

        IReadOnlyList<Selection>? after = CommandExecutor.ExecuteCommands(
            model,
            before,
            [new InsertTextCommand(new Range(1, 1, 1, 3), "X"), new InsertTextCommand(new Range(1, 2, 1, 4), "Y")]);

        Assert.Equal("Xcd", model.GetValue());
        Assert.NotNull(after);
        Assert.Equal([Caret(1, 2)], after);
    }

    [Fact]
    public void ExecuteCommands_FirstCursorLoses_AllCommandsAreIgnored()
    {
        // Cursor 0 can only lose when its own operations overlap each other.
        var model = new TextModel("abcd");

        IReadOnlyList<Selection>? after = CommandExecutor.ExecuteCommands(
            model,
            [Caret(1, 1)],
            [new MultiOperationCommand([new Range(1, 1, 1, 3), new Range(1, 2, 1, 4)])]);

        Assert.Null(after);
        Assert.Equal("abcd", model.GetValue());
    }

    [Fact]
    public void ExecuteCommands_TrackSelection_ReturnsTheSelectionMovedByTheOtherEdits()
    {
        var model = new TextModel("abcdef");
        var tracked = new Selection(new Position(1, 3), new Position(1, 4));

        IReadOnlyList<Selection>? after = CommandExecutor.ExecuteCommands(
            model,
            [Caret(1, 1), Caret(1, 6)],
            [new InsertTextCommand(new Range(1, 1, 1, 1), "xx"), new InsertTextCommand(new Range(1, 6, 1, 6), "y", tracked)]);

        Assert.Equal("xxabcdeyf", model.GetValue());
        Assert.NotNull(after);
        Assert.Equal(new Selection(new Position(1, 5), new Position(1, 6)), after[1]);
    }

    [Fact]
    public void ExecuteCommands_TrackSelection_KeepsTheDirectionOfTheSelection()
    {
        var model = new TextModel("abcdef");
        var backwards = new Selection(new Position(1, 4), new Position(1, 3));

        IReadOnlyList<Selection>? after = CommandExecutor.ExecuteCommands(
            model,
            [Caret(1, 6)],
            [new InsertTextCommand(new Range(1, 6, 1, 6), "y", backwards)]);

        Assert.NotNull(after);
        Assert.Equal(backwards, Assert.Single(after));
    }

    [Fact]
    public void ExecuteCommands_Always_ReleasesTheTrackedRanges()
    {
        var model = new TextModel("abcdef");

        CommandExecutor.ExecuteCommands(
            model,
            [Caret(1, 6)],
            [new InsertTextCommand(new Range(1, 6, 1, 6), "y", new Selection(new Position(1, 1), new Position(1, 2)))]);

        Assert.Equal(0, model.TrackedRangeCount);
    }

    [Fact]
    public void ExecuteCommands_NullCommand_KeepsItsSelection()
    {
        var model = new TextModel("abcd");

        IReadOnlyList<Selection>? after = CommandExecutor.ExecuteCommands(
            model,
            [Caret(1, 1), Caret(1, 4)],
            [null, new InsertTextCommand(new Range(1, 4, 1, 4), "z")]);

        Assert.Equal("abczd", model.GetValue());
        Assert.NotNull(after);
        Assert.Equal([Caret(1, 1), Caret(1, 5)], after);
    }

    [Fact]
    public void ExecuteCommands_AllNullCommands_ReturnsNullAndLeavesTheModelAlone()
    {
        var model = new TextModel("abcd");

        IReadOnlyList<Selection>? after = CommandExecutor.ExecuteCommands(model, [Caret(1, 1)], [null]);

        Assert.Null(after);
        Assert.Equal("abcd", model.GetValue());
        Assert.False(model.CanUndo);
    }

    [Fact]
    public void ExecuteCommands_NoOpOperation_IsDropped()
    {
        var model = new TextModel("abcd");

        IReadOnlyList<Selection>? after = CommandExecutor.ExecuteCommands(
            model, [Caret(1, 1)], [new InsertTextCommand(new Range(1, 1, 1, 1), string.Empty)]);

        Assert.Null(after);
        Assert.Equal("abcd", model.GetValue());
    }

    [Fact]
    public void ExecuteCommands_ThrowingCommand_IsIgnored()
    {
        var model = new TextModel("abcd");

        IReadOnlyList<Selection>? after = CommandExecutor.ExecuteCommands(
            model,
            [Caret(1, 1), Caret(1, 4)],
            [new ThrowingCommand(), new InsertTextCommand(new Range(1, 4, 1, 4), "z")]);

        Assert.Equal("abczd", model.GetValue());
        Assert.NotNull(after);
        Assert.Equal([Caret(1, 1), Caret(1, 5)], after);
    }

    [Fact]
    public void ExecuteCommands_AutoWhitespaceCommand_MarksItsOperations()
    {
        var model = new TextModel("  x");

        CommandExecutor.ExecuteCommands(
            model,
            [Caret(1, 4)],
            [new InsertTextCommand(new Range(1, 4, 1, 4), "\n  ", insertsAutoWhitespace: true)]);
        model.PushStackElement();

        // Line 2 is auto whitespace: the next edit near the cursor trims it.
        CommandExecutor.ExecuteCommands(model, [Caret(2, 3)], [new InsertTextCommand(new Range(2, 3, 2, 3), "\n")]);

        Assert.Equal(["  x", "", ""], model.GetLinesContent());
    }

    [Fact]
    public void GetEditOperation_ReturnsTheOperationsInTheOrderTheyWereAdded()
    {
        var model = new TextModel("abcd");
        var command = new MultiOperationCommand([new Range(1, 3, 1, 4), new Range(1, 1, 1, 2)]);

        var operations = CommandTester.GetEditOperation(model, command);

        Assert.Equal([new Range(1, 3, 1, 4), new Range(1, 1, 1, 2)], operations.Select(o => o.Range));
        Assert.All(operations, o => Assert.Equal("-", o.Text));
    }

    [Fact]
    public void TestCommand_InsertCommand_ChecksLinesAndSelection()
    {
        CommandTester.TestCommand(
            ["abcd"],
            Caret(1, 2),
            _ => new InsertTextCommand(new Range(1, 2, 1, 2), "xy"),
            ["axybcd"],
            Caret(1, 4));
    }
}
